using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;
using Util;

namespace Generation
{
    /// <summary>
    /// Holds the live data of world gen, as well as its function to manipulate the live values
    /// </summary>
    public class WorldGenRuntime
    {
        private readonly MonoBehaviour unityConnection;
        public readonly WorldGen owner;
        public string currentSeed;
        
        public readonly GridRuntime gridRuntime = new();
        
        private List<Area> backlog;
        private readonly List<AreaRuntime> hadAreas = new();
        
        public RoomType? lastHadRoomType;
        private readonly List<Room> hadRooms = new();
        
        public Vector2Int CurrentPosition { get; private set; } = Vector2Int.zero;
        private Vector2Int currentWalkDirection = Vector2Int.zero;
        private int walkDirRepeated;
        
        private readonly float minDistToBossRoom;
        private RepeatSetting walkDirRepeatSetting;

        public WorldGenRuntime(MonoBehaviour unityConnection, WorldGen worldGen, string seed, List<Area> areaData,
            float minDistToBossRoom)
        {
            this.unityConnection = unityConnection;
            this.owner = worldGen;
            this.currentSeed = seed;
            this.backlog = new List<Area>(areaData);
            this.minDistToBossRoom = minDistToBossRoom;
            this.walkDirRepeatSetting = RepeatSetting.Area;
        }
        
        /// <summary>
        /// used when trying out a whole different seed
        /// </summary>
        /// <param name="areaData"></param>
        /// <param name="seed"></param>
        private void Reset(List<Area> areaData, string seed)
        {
            this.currentSeed = seed;
            this.backlog = new List<Area>(areaData);

            this.currentWalkDirection = Vector2Int.zero;
            this.CurrentPosition = Vector2Int.zero;
            this.walkDirRepeated = 0;

            this.lastHadRoomType = null;

            this.hadAreas.Clear();
            this.hadRooms.Clear();
            
            this.gridRuntime.Clear();
            this.walkDirRepeatSetting = RepeatSetting.Area;
        }

        private void RegisterHadRoom(Room foundRoom, int roomRepetitionAllowance)
        {
            this.hadRooms.Add(foundRoom);
            if (this.hadRooms.Count >= roomRepetitionAllowance) this.hadRooms.RemoveRange(this.hadRooms.Count - roomRepetitionAllowance, this.hadRooms.Count);
        }

        public void MutateRng() => this.currentSeed = Rng.MutateNext(this.currentSeed);
        
        private AreaRuntime GetRandomArea()
        {
            int foundIndex = Rng.RandomRange(0, this.backlog.Count, this.currentSeed);
            MutateRng();
                
            AreaRuntime pickedArea = this.backlog[foundIndex].GetAreaGenData(this.currentSeed);
            this.backlog.RemoveAt(foundIndex);
            
            return pickedArea;
        }
        
        private AreaRuntime GetHadAreaByType(AreaType areaType) => this.hadAreas.First(a => a.AreaType == areaType);

        /// <summary>
        /// Triggers the start of a world generation
        /// </summary>
        /// <param name="seed">used rng seed</param>
        /// <param name="areaData">List of all desired areas</param>
        public IEnumerator StartGen(string seed, List<Area> areaData)
        {
            while (true)
            {
                yield return this.unityConnection.StartCoroutine(WalkAllAreas());
                
                NotificationManager.Log($"Spawn BossRoom", this.owner.GetAnimWaitTime());
                RoomRuntimeRef bossRoomRef = new();
                yield return this.unityConnection.StartCoroutine(TryAddBossRoom(bossRoomRef));
                
                NotificationManager.Log($"ANALYSE PHASE STARTING", this.owner.GetAnimWaitTime());
                if (Vector2.Distance(this.CurrentPosition, Vector2.zero) >= this.minDistToBossRoom) break;

                NotificationManager.Log($"GENERATION FAILED, RESTARTING", this.owner.GetAnimWaitTime());
                this.currentSeed = Rng.MutateNext(seed);
                Reset(areaData, this.currentSeed);
                bossRoomRef.instance = null;
            }

            this.gridRuntime.RemoveUnusedDoorways();
        }

        private IEnumerator WalkAllAreas()
        {
            while (this.backlog.Count > 0)
            {
                NotificationManager.Log($"Walking through area backlog, current count: {this.backlog.Count}", this.owner.GetAnimWaitTime());
                AreaRuntime pickedArea = GetRandomArea();
                
                yield return this.unityConnection.StartCoroutine(pickedArea.WalkthroughArea(this, this.unityConnection, this.owner.GetAnimYieldInstruction()));
                this.hadAreas.Add(pickedArea);
                
                yield return this.owner.GetAnimYieldInstruction();
            }

            NotificationManager.Log("Done with walking through areas", this.owner.GetAnimWaitTime());
        }

        public void Walk(RoomRuntime currentRoom)
        {
            // go to door
            UpdateWalkDirection(currentRoom.areaType);
            Room.DoorPointGroup doorway = GetRandomDoor(currentRoom.roomRef, this.currentWalkDirection);
            
            // register that door was used, and space self outside of it
            Vector2Int newPos = currentRoom.position + doorway.roomPoint + this.currentWalkDirection; // position just about outside of room
            this.CurrentPosition = newPos;
            NotificationManager.Log($"Walking from {this.CurrentPosition} to {newPos}", this.owner.GetAnimWaitTime());
            
            currentRoom.RemoveDoorFromLayout(doorway);

            UpdateSnapShot();
            MutateRng();
        }

        private void UpdateSnapShot()
        {
            Action<WorldGenSnapshot> onUpdateSnapshot = this.owner.GetOnUpdateSnapshot(this);
            onUpdateSnapshot?.Invoke(new WorldGenSnapshot(this.gridRuntime, this.CurrentPosition));
        }

        private void UpdateWalkDirection(AreaType areaType)
        {
            while (true)
            {
                float maxAllowance = 0;
                switch (this.walkDirRepeatSetting)
                {
                    case RepeatSetting.Area:
                        maxAllowance = this.owner.GetWalkDirectionRepetitionAllowance(areaType);
                        break;
                    case RepeatSetting.World:
                        maxAllowance = int.MaxValue;
                        break;
                    case RepeatSetting.Room:
                    default:
                        throw new ArgumentOutOfRangeException();
                }
                
                int index = Rng.RandomRange(0, Extensions.CardinalDirections.Length, this.currentSeed);
                MutateRng();

                if (Extensions.CardinalDirections[index] == this.currentWalkDirection)
                    this.walkDirRepeated++;
                else
                    this.walkDirRepeated = 0;

                if (this.walkDirRepeated > maxAllowance) continue;

                this.currentWalkDirection = Extensions.CardinalDirections[index];
                break;
            }
        }

        public IEnumerator AddRoom(AreaRuntime pickedArea, PendingRoomPlacement pendingRoom, RoomRuntimeRef runtimeRef)
        {
            if (pendingRoom.possibleRoom == null) yield break;

            if (pendingRoom.center != null)
            {
                RoomRuntime placedRoom = this.gridRuntime.PlaceRoom(pendingRoom.possibleRoom, pendingRoom.center.Value);
                if (pendingRoom.doorGroup != null)
                    placedRoom.RemoveDoorFromLayout(pendingRoom.doorGroup);
                placedRoom.areaType = pickedArea.AreaType;
                placedRoom.roomType = pendingRoom.possibleRoomType;

                NotificationManager.Log($"Adding room of size: {pendingRoom.possibleRoom.Size}", this.owner.GetAnimWaitTime());
                RegisterHadRoom(pendingRoom.possibleRoom, this.owner.RoomRepetitionAllowance);

                pickedArea.Size -= pendingRoom.possibleRoom.Size;
                this.lastHadRoomType = placedRoom.roomType; // TODO: make allowance unique per type per area?

                runtimeRef.instance = placedRoom;
            }

            UpdateSnapShot();
        }
        
        /// <summary>
        /// Tries to find a spot where it can place a room, updates result accordingly
        /// </summary>
        public IEnumerator TryToGetPossibleRoom(AreaRuntime pickedArea, PendingRoomPlacement placement, List<Room> maxPool, float smallestRoomSize, RoomType roomType)
        {
            int tries = 0;
            const int maxTries = 64;
            
            int overlapAttempts = 0;
            const int maxOverlapAttempts = 8;
            const int maxOverlapAttemptsBruteForce = 16;

            List<Room> hadRoomsTemp = new();
            
            while (true)
            {
                if (smallestRoomSize > pickedArea.Size)
                {
                    Debug.Log("No rooms exist that can fill area quota");
                    pickedArea.Size = 0;
                    break;
                }

                List<Room> sizedPool = maxPool.Where(room => room.Size <= math.max(smallestRoomSize, pickedArea.Size)).ToList();
                placement.possibleRoom = RoomList.TryGetRoom(this, sizedPool, this.hadRooms.ToArray());
                
                // TryGetRoom failed
                if (placement.possibleRoom == null) 
                {
                    if (tries > maxTries)
                    {
                        this.hadRooms.Clear();
                        this.lastHadRoomType = null;
                        NotificationManager.Log($"Need room:\nsize between: {smallestRoomSize}, {pickedArea.Size}\nType:{roomType} Area:{pickedArea.AreaType}", this.owner.GetAnimWaitTime());

                        #if UNITY_EDITOR
                        break;
                        #else
                        continue;
                        #endif
                    }

                    MutateRng();
                    tries++;
                    continue;
                }

                // register
                if (hadRoomsTemp.Contains(placement.possibleRoom)) hadRoomsTemp.Add(placement.possibleRoom);

                // move to new possible center position
                if (this.currentWalkDirection == Vector2.zero)
                {
                    int halfWidth = placement.possibleRoom.Width / 2;
                    int halfHeight = placement.possibleRoom.Height / 2;
                    Vector2Int offset = new(this.currentWalkDirection.x * halfWidth, this.currentWalkDirection.y * halfHeight);
                    placement.center = this.CurrentPosition + offset;
                }
                else if (!TryWalkThroughDoor(placement, -this.currentWalkDirection)) break;

                // movement failed, so show on visual
                Action<WorldGenSnapshot> onUpdateSnapshot = this.owner.GetOnUpdateSnapshot(this);
                onUpdateSnapshot?.Invoke(new WorldGenSnapshot(this.gridRuntime, placement.center!.Value));
                
                if (this.gridRuntime.CheckRoomPossible(placement.possibleRoom, placement.center!.Value, out RoomRuntime? hit)) break;
                NotificationManager.Log($"Cant place room of size {placement.possibleRoom.Size}, at {placement.center.Value}", this.owner.GetAnimWaitTime());

                #region Tracking attempts

                overlapAttempts++;
                if (overlapAttempts > maxOverlapAttempts)
                {
                    if (hit != null) Walk(hit);
                    
                    if (overlapAttempts >= maxOverlapAttemptsBruteForce)
                    {
                        this.hadRooms.Clear();
                        this.lastHadRoomType = null;
                    }
                }

                if (hadRoomsTemp.Count >= maxPool.Count)
                {
                    placement.possibleRoom = null;
                    yield break;
                }
                
                yield return this.owner.GetAnimYieldInstruction();

                #endregion
                
            }
        }

        /// <summary>
        /// Moves the current position to the bounds of the current room based on walk direction
        /// </summary>
        /// <returns>false if walk failed, true if walk succeeded</returns>
        private bool TryWalkThroughDoor(PendingRoomPlacement placement, Vector2Int walkDirection)
        {
            if (placement.possibleRoom == null) return false;
            placement.doorGroup = GetRandomDoor(placement.possibleRoom, walkDirection);

            if (placement.doorGroup == null) return false;
            placement.center = this.CurrentPosition - placement.doorGroup.roomPoint;
            MutateRng();
            
            return true;
        }

        private Room.DoorPointGroup GetRandomDoor(Room room, Vector2Int walkDirection)
        {
            List<Room.DoorPointGroup> doorways = room.DoorPoints
                .First(dir => dir.key == walkDirection).value;
            return doorways[Rng.RandomRange(0, doorways.Count, this.currentSeed)];
        }

        /// <summary>
        /// Gets all possible doorways, then checks overlap, and places a boss room fitting the adjecent area
        /// </summary>
        private IEnumerator TryAddBossRoom(RoomRuntimeRef bossRoomRef)
        {
            this.walkDirRepeatSetting = RepeatSetting.World;
            
            List<(RoomRuntime room, Vector2Int position, Vector2Int direction)> doorPositions = this.gridRuntime.GetAllPossibleDoorPositions()
                .Where(d => Vector2.Distance(d.position, Vector2.zero) >= this.minDistToBossRoom)
                .OrderByDescending(d => Vector2.Distance(d.position, Vector2.zero))
                .ToList();

            for (int i = 0; i < doorPositions.Count; i++)
            {
                (RoomRuntime room, Vector2Int position, Vector2Int direction) door = doorPositions[i];
                this.CurrentPosition = door.room.position;
                this.currentWalkDirection = door.direction;
                
                if (Vector2.Distance(door.position + door.room.position, Vector2.zero) < this.minDistToBossRoom) continue;

                AreaRuntime area = GetHadAreaByType(door.room.areaType);

                PendingRoomPlacement pendingRoom = new();
                yield return this.unityConnection.StartCoroutine(area.GetPossibleBossRoom(this, pendingRoom,
                    this.unityConnection));

                float dist = pendingRoom.possibleRoom != null
                    ? Vector2.Distance(pendingRoom.center!.Value, Vector2.zero)
                    : 0;
                if (dist < this.minDistToBossRoom)
                {
                    Debug.Log($"Boss room not possible or too close, at door {i}");
                    continue;
                }

                RoomRuntimeRef runtimeRef = new();
                yield return this.unityConnection.StartCoroutine(AddRoom(area, pendingRoom, runtimeRef));

                if (runtimeRef.instance == null) continue;

                NotificationManager.Log($"Boss room added, {dist} from start", this.owner.GetAnimWaitTime());
                bossRoomRef.instance = runtimeRef.instance;
                this.CurrentPosition = bossRoomRef.instance.position;
                yield break;
            }
        }
    }
}