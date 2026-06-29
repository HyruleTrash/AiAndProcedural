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
        private readonly WorldGen owner;
        public string currentSeed;
        
        private readonly GridRuntime gridRuntime = new();
        
        private List<Area> backlog;
        private readonly List<AreaRuntime> hadAreas = new();
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

        public class AnimDataWrapper
        {
            // public readonly WorldGen gen;
            public readonly WorldGenRuntime genRuntime;
            public readonly MonoBehaviour unityConnection;
            public readonly YieldInstruction? yieldInstruction;
            public readonly float waitTime;

            public AnimDataWrapper(WorldGenRuntime worldGenRuntimeRuntime, MonoBehaviour unityConnection, YieldInstruction? yieldInstruction, float waitTime)
            { 
                // this.gen = gen; 
                this.genRuntime = worldGenRuntimeRuntime;
                this.unityConnection = unityConnection;
                this.yieldInstruction = yieldInstruction;
                this.waitTime = waitTime;
            }
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

            this.hadAreas.Clear();
            this.hadRooms.Clear();
            
            this.gridRuntime.Clear();
            this.walkDirRepeatSetting = RepeatSetting.Area;
        }

        public RoomRuntime? GetRoomAtPosition(Vector2Int pos) => this.gridRuntime.GetRoomAtPosition(pos);
        public void MutateRng() => this.currentSeed = Rng.MutateNext(this.currentSeed);
        
        private void RegisterHadRoom(Room foundRoom, int roomRepetitionAllowance)
        {
            this.hadRooms.Add(foundRoom);
            if (this.hadRooms.Count >= roomRepetitionAllowance) this.hadRooms.RemoveRange(this.hadRooms.Count - roomRepetitionAllowance, this.hadRooms.Count);
        }
        
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
            float waitTime = this.owner.GetAnimWaitTime();
            while (true)
            {
                yield return this.unityConnection.StartCoroutine(WalkAllAreas());
                
                NotificationManager.Log($"Spawn BossRoom", waitTime);
                RoomRuntimeRef bossRoomRef = new();
                yield return this.unityConnection.StartCoroutine(TryAddBossRoom(bossRoomRef));
                
                NotificationManager.Log($"ANALYSE PHASE STARTING", waitTime);
                if (Vector2.Distance(this.CurrentPosition, Vector2.zero) >= this.minDistToBossRoom) break;

                NotificationManager.Log($"GENERATION FAILED, RESTARTING", waitTime);
                Reset(areaData, Rng.MutateNext(this.currentSeed));
            }

            NotificationManager.Log($"FINISHED", waitTime);
            this.gridRuntime.RemoveUnusedDoorways();
            this.CurrentPosition = Vector2Int.zero;
            UpdateSnapShot();
        }

        private IEnumerator WalkAllAreas()
        {
            AnimDataWrapper animData = new(this, this.unityConnection, this.owner.GetAnimYieldInstruction(), this.owner.GetAnimWaitTime());
            
            while (this.backlog.Count > 0)
            {
                NotificationManager.Log($"Walking through area backlog, current count: {this.backlog.Count}", animData.waitTime);
                AreaRuntime pickedArea = GetRandomArea();
                
                yield return this.unityConnection.StartCoroutine(pickedArea.WalkthroughArea(animData));
                this.hadAreas.Add(pickedArea);
                
                if (animData.yieldInstruction != null)
                    yield return animData.yieldInstruction;
            }

            NotificationManager.Log("Done with walking through areas", animData.waitTime);
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
                float maxAllowance;
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
                runtimeRef.instance = placedRoom;
            }

            UpdateSnapShot();
        }
        
        /// <summary>
        /// Tries to find a spot where it can place a room, updates result accordingly
        /// </summary>
        public IEnumerator TryFindRoomPlacement(AreaRuntime pickedArea, PendingRoomPlacement placement, List<Room> maxPool, float smallestRoomSize, RoomType roomType)
        {
            int tries = 0;
            int overlapAttempts = 0;
            
            while (true)
            {
                if (!pickedArea.SizeCheck(smallestRoomSize)) break;
                
                RoomSelectionResult selectionResult = TrySelectRoomForAvailableSpace(pickedArea, placement, maxPool, smallestRoomSize, roomType, ref tries);
                if (selectionResult == RoomSelectionResult.Abort) break;
                if (selectionResult == RoomSelectionResult.Retry) continue;
                
                if (TrySetPlacementPosition(placement)) break;
                UpdateSnapShot();
                
                if (this.gridRuntime.CheckRoomPossible(placement.possibleRoom!, placement.center!.Value, out RoomRuntime? hit)) break;
                
                overlapAttempts = ProcessOverlapAttempts(placement, overlapAttempts, hit);

                YieldInstruction? yieldInstruction = this.owner.GetAnimYieldInstruction();
                if (yieldInstruction != null) yield return yieldInstruction;
            }
        }

        private int ProcessOverlapAttempts(PendingRoomPlacement placement, int overlapAttempts, RoomRuntime? hit)
        {
            NotificationManager.Log($"Cant place room of size {placement.possibleRoom!.Size}, at {placement.center!.Value}", this.owner.GetAnimWaitTime());

            overlapAttempts++;
            if (overlapAttempts <= this.owner.MaxOverlapAttempts) return overlapAttempts;
            if (hit != null) Walk(hit);
            if (overlapAttempts >= this.owner.MaxOverlapAttemptsBruteForce) this.hadRooms.Clear();

            return overlapAttempts;
        }

        /// <summary>
        /// If there's no set walk direction, it places the pending room it the center relative to current pos
        /// if there is a direction, we need to go through the door still
        /// </summary>
        /// <returns>true on successful movement</returns>
        private bool TrySetPlacementPosition(PendingRoomPlacement placement)
        {
            if (this.currentWalkDirection != Vector2.zero) return !TryWalkThroughDoor(placement, -this.currentWalkDirection);
            
            int halfWidth = placement.possibleRoom!.Width / 2;
            int halfHeight = placement.possibleRoom.Height / 2;
            Vector2Int offset = new(this.currentWalkDirection.x * halfWidth, this.currentWalkDirection.y * halfHeight);
            placement.center = this.CurrentPosition + offset;
            return true;
        }

        private enum RoomSelectionResult { Success, Retry, Abort }
        private RoomSelectionResult TrySelectRoomForAvailableSpace(AreaRuntime pickedArea, PendingRoomPlacement placement, List<Room> maxPool, float smallestRoomSize, RoomType roomType, ref int tries)
        {
            List<Room> sizedPool = maxPool.Where(room => room.Size <= math.max(smallestRoomSize, pickedArea.Size)).ToList();
            placement.possibleRoom = RoomList.TryGetRoom(this, sizedPool, this.hadRooms.ToArray());
    
            if (placement.possibleRoom != null) return RoomSelectionResult.Success;
    
            if (tries > this.owner.MaxTries)
            {
                this.hadRooms.Clear();
                NotificationManager.Log($"Need room:\nsize between: {smallestRoomSize}, {pickedArea.Size}\nType:{roomType} Area:{pickedArea.AreaType}", this.owner.GetAnimWaitTime());
                return RoomSelectionResult.Abort;
            }

            MutateRng();
            tries++;
            return RoomSelectionResult.Retry;
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
            AnimDataWrapper animData = new(this, this.unityConnection, this.owner.GetAnimYieldInstruction(),
                this.owner.GetAnimWaitTime());
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
                yield return this.unityConnection.StartCoroutine(area.GetPossibleBossRoom(animData, pendingRoom));

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