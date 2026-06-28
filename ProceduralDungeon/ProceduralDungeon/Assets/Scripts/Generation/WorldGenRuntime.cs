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
        
        public readonly GridRuntime gridRuntime = new();
        
        private List<Area> backlog;
        private readonly List<AreaRuntime> hadAreas = new();
        
        public RoomType? lastHadRoomType;
        private readonly List<Room> hadRooms = new();
        
        public Vector2Int CurrentPosition => this.currentPosition;
        private Vector2Int currentPosition = Vector2Int.zero;
        private Vector2Int currentWalkDirection = Vector2Int.zero;
        private int walkDirRepeated;
        
        private readonly float minDistToBossRoom;

        public WorldGenRuntime(MonoBehaviour unityConnection, WorldGen worldGen, string seed, List<Area> areaData,
            float minDistToBossRoom)
        {
            this.unityConnection = unityConnection;
            this.owner = worldGen;
            this.currentSeed = seed;
            this.backlog = new List<Area>(areaData);
            this.minDistToBossRoom = minDistToBossRoom;
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
            this.currentPosition = Vector2Int.zero;
            this.walkDirRepeated = 0;

            this.lastHadRoomType = null;

            this.hadAreas.Clear();
            this.hadRooms.Clear();
            
            this.gridRuntime.Clear();
        }

        private void AddToHadRooms(Room foundRoom, int roomRepetitionAllowance)
        {
            this.hadRooms.Add(foundRoom);
            if (this.hadRooms.Count >= roomRepetitionAllowance) this.hadRooms.RemoveRange(this.hadRooms.Count - roomRepetitionAllowance, this.hadRooms.Count);
        }
        
        /// <summary>
        /// Triggers the start of a world generation
        /// </summary>
        /// <param name="seed">used rng seed</param>
        /// <param name="areaData">List of all desired areas</param>
        public IEnumerator StartGen(string seed, List<Area> areaData)
        {
            while (true)
            {
                yield return this.unityConnection.StartCoroutine(WalkthroughAreas());
                
                AreaRuntime? lastArea = this.hadAreas.Last();
                RoomRuntimeRef bossRoomRef = new();

                while (true)
                {
                    yield return this.unityConnection.StartCoroutine(AddRoom(lastArea, bossRoomRef));
                    if (bossRoomRef.instance != null) break;
                }
                
                NotificationManager.Log($"ANALYSE PHASE STARTING");
                if (Vector2.Distance(this.currentPosition, Vector2.zero) >= this.minDistToBossRoom) break;

                NotificationManager.Log($"GENERATION FAILED, RESTARTING");
                this.currentSeed = Rng.MutateNext(seed);
                Reset(areaData, this.currentSeed);
                bossRoomRef.instance = null;
            }

            this.gridRuntime.RemoveUnusedDoorways();
        }

        private IEnumerator WalkthroughAreas()
        {
            while (this.backlog.Count > 0)
            {
                NotificationManager.Log($"Walking through area backlog, current count: {this.backlog.Count}");
                int foundIndex = Rng.RandomRange(0, this.backlog.Count, this.currentSeed);
                this.currentSeed = Rng.MutateNext(this.currentSeed);
                
                AreaRuntime pickedArea = this.backlog[foundIndex].GetAreaGenData(this.currentSeed);
                this.currentSeed = Rng.MutateNext(this.currentSeed);

                this.backlog.RemoveAt(foundIndex);
                yield return this.unityConnection.StartCoroutine(pickedArea.WalkthroughArea(this, this.unityConnection, this.owner.GetAnimWaitTime()));
                this.hadAreas.Add(pickedArea);
                
                yield return this.owner.GetAnimWaitTime();
            }

            NotificationManager.Log("Done with walking through areas");
        }

        public void Walk(RoomRuntime currentRoom)
        {
            UpdateWalkDirection();

            // Move to doorway of room
            List<Room.DoorPointGroup> doorways = currentRoom.roomRef.DoorPoints.First(dir => dir.key == this.currentWalkDirection).value;
            Room.DoorPointGroup doorway = doorways[Rng.RandomRange(0, doorways.Count, this.currentSeed)];
            Vector2Int newPos = currentRoom.position + doorway.roomPoint + this.currentWalkDirection;
            currentRoom.RemoveDoorFromLayout(doorway);
            this.currentSeed = Rng.MutateNext(this.currentSeed);
                
            NotificationManager.Log($"Walking from {this.currentPosition} to {newPos}");
            this.currentPosition = newPos;

            Action<WorldGenSnapshot> onUpdateSnapshot = this.owner.GetOnUpdateSnapshot(this);
            onUpdateSnapshot?.Invoke(new WorldGenSnapshot(this.gridRuntime, this.currentPosition));
        }

        private void UpdateWalkDirection()
        {
            while (true)
            {
                int index = Rng.RandomRange(0, Extensions.CardinalDirections.Length, this.currentSeed);
                this.currentSeed = Rng.MutateNext(this.currentSeed);

                if (Extensions.CardinalDirections[index] == this.currentWalkDirection)
                    this.walkDirRepeated++;
                else
                    this.walkDirRepeated = 0;

                if (this.walkDirRepeated > this.owner.WalkDirectionRepetitionAllowance) continue;

                this.currentWalkDirection = Extensions.CardinalDirections[index];
                break;
            }
        }

        public IEnumerator AddRoom(AreaRuntime pickedArea, RoomRuntimeRef runtimeRef)
        {
            TypedRoomList pickedTypeList = pickedArea.PickTypeList(this);
            pickedTypeList.OnPicked(pickedArea);
            this.currentSeed = Rng.MutateNext(this.currentSeed);

            GetRoomResult getRoomResult = new();
            yield return this.unityConnection.StartCoroutine(TryGetTypedRoom(getRoomResult, pickedArea, pickedTypeList));
            
            if (getRoomResult.foundRoom == null) yield break;

            if (getRoomResult.center != null)
            {
                RoomRuntime placedRoom = this.gridRuntime.PlaceRoom(getRoomResult.foundRoom, getRoomResult.center.Value);
                if (getRoomResult.doorGroup != null)
                    placedRoom.RemoveDoorFromLayout(getRoomResult.doorGroup);
                placedRoom.areaType = pickedArea.AreaType;
                placedRoom.roomType = pickedTypeList.roomType;

                NotificationManager.Log($"Adding room of size: {getRoomResult.foundRoom.Size}");
                AddToHadRooms(getRoomResult.foundRoom, this.owner.RoomRepetitionAllowance);

                pickedArea.Size -= getRoomResult.foundRoom.Size;
                this.lastHadRoomType = pickedTypeList.roomType; // TODO: make allowance unique per type per area?

                runtimeRef.instance = placedRoom;
            }

            Action<WorldGenSnapshot> onUpdateSnapshot = this.owner.GetOnUpdateSnapshot(this);
            onUpdateSnapshot?.Invoke(new WorldGenSnapshot(this.gridRuntime, this.currentPosition));
        }

        /// <summary>
        /// Uses duplicated code to pull from the endRooms list, and spawn a boss room somewhere adjacent
        /// </summary>
        private IEnumerator TryGetBossRoom(GetRoomResult result, AreaRuntime pickedArea)
        {
            int tries = 0;
            const int maxTries = 16;
            const int lastTry = 64;
            List<Room> pool = pickedArea.EndRooms;
            if (pool.Count <= 0) yield break;

            List<GetRoomResult> fallbacks = new();
            while (true)
            {
                result = new GetRoomResult { foundRoom = pool[Rng.RandomRange(0, pool.Count, this.currentSeed)] };
                this.currentSeed = Rng.MutateNext(this.currentSeed);
                
                if (!TryWalkThroughDoor(result)) continue;
                
                Action<WorldGenSnapshot> onUpdateSnapshot = this.owner.GetOnUpdateSnapshot(this);
                onUpdateSnapshot?.Invoke(new WorldGenSnapshot(this.gridRuntime, result.center.Value));

                if (this.gridRuntime.CheckRoomPossible(result.foundRoom, result.center.Value, out RoomRuntime? hit))
                {
                    if (Vector2.Distance(result.center.Value, Vector2.zero) >= this.minDistToBossRoom)
                        break;
                    fallbacks.Add(result);
                    break;
                }

                if (tries >= maxTries && hit != null) Walk(hit);

                if (tries > lastTry)
                {
                    result = fallbacks.Last();
                    if (result.center != null) this.currentPosition = result.center.Value;
                    break;
                }

                this.currentSeed = Rng.MutateNext(this.currentSeed);
                tries++;
                yield return this.owner.GetAnimWaitTime();
            }
        }

        private IEnumerator TryGetTypedRoom(GetRoomResult getRoomResult, AreaRuntime pickedArea, TypedRoomList pickedTypeList)
        {
            int tries = 0;
            int maxTries = pickedArea.RoomCount * 2;
            while (getRoomResult.foundRoom == null)
            {
                yield return this.unityConnection.StartCoroutine(TryToGetPlaceAbleRoom(pickedTypeList, pickedArea, getRoomResult));

                if (getRoomResult.foundRoom != null) break;

                if (tries >= maxTries) yield break;

                pickedTypeList.UndoPicked(pickedArea);
                pickedTypeList = pickedArea.PickTypeList(this);
                pickedTypeList.OnPicked(pickedArea);
                this.currentSeed = Rng.MutateNext(this.currentSeed);

                tries++;
            }
        }
        
        private IEnumerator TryToGetPlaceAbleRoom(TypedRoomList pickedTypeList, AreaRuntime pickedArea, GetRoomResult result)
        {
            int tries = 0;
            const int maxTries = 64;
            
            int overlapAttempts = 0;
            const int maxOverlapAttempts = 8;
            const int maxOverlapAttemptsBruteForce = 16;

            List<Room> hadRoomsTemp = new();
            List<Room> maxPool = pickedTypeList.Rooms.RoomData.Where(room => room.Size <= pickedArea.Size).ToList();
            
            while (true)
            {
                if (pickedTypeList.smallestRoomSize > pickedArea.Size)
                {
                    NotificationManager.Log("No rooms exist that can fill area quota");
                    pickedArea.Size = 0;
                    break;
                }

                List<Room> sizedPool = maxPool.Where(room => room.Size <= math.max(pickedTypeList.smallestRoomSize, pickedArea.Size)).ToList();
                result.foundRoom = RoomList.TryGetRoom(this, sizedPool, this.hadRooms.ToArray());
                if (result.foundRoom == null) // TryGetRoom failed
                {
                    if (tries > maxTries)
                    {
                        this.hadRooms.Clear();
                        this.lastHadRoomType = null;
                        NotificationManager.Log($"Need room:\nsize between: {pickedTypeList.smallestRoomSize}, {pickedArea.Size}\nType:{pickedTypeList.roomType} Area:{pickedArea.AreaType}");

                        #if UNITY_EDITOR
                        break;
                        #else
                        continue;
                        #endif
                    }

                    this.currentSeed = Rng.MutateNext(this.currentSeed);
                    tries++;
                    continue;
                }

                if (hadRoomsTemp.Contains(result.foundRoom)) hadRoomsTemp.Add(result.foundRoom);

                if (this.currentWalkDirection == Vector2.zero)
                {
                    int halfWidth = result.foundRoom.Width / 2;
                    int halfHeight = result.foundRoom.Height / 2;
                    Vector2Int offset = new(this.currentWalkDirection.x * halfWidth, this.currentWalkDirection.y * halfHeight);
                    result.center = this.currentPosition;
                    result.center += offset;
                }
                else if (!TryWalkThroughDoor(result)) break;

                Action<WorldGenSnapshot> onUpdateSnapshot = this.owner.GetOnUpdateSnapshot(this);
                onUpdateSnapshot?.Invoke(new WorldGenSnapshot(this.gridRuntime, result.center!.Value));
                
                if (this.gridRuntime.CheckRoomPossible(result.foundRoom, result.center!.Value, out RoomRuntime? hit)) break;
                NotificationManager.Log($"Cant place room of size {result.foundRoom.Size}, at {result.center.Value}");
                
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
                    result.foundRoom = null;
                    yield break;
                }
                
                yield return this.owner.GetAnimWaitTime();
            }
        }

        /// <summary>
        /// Moves the current position to the bounds of the current room based on walk direction
        /// </summary>
        /// <returns>false if walk failed, true if walk succeeded</returns>
        private bool TryWalkThroughDoor(GetRoomResult result)
        {
            if (result.foundRoom == null) return false;
            List<Room.DoorPointGroup> doorways = result.foundRoom.DoorPoints
                .First(dir => dir.key == -this.currentWalkDirection).value;
            result.doorGroup = doorways[Rng.RandomRange(0, doorways.Count, this.currentSeed)];

            if (result.doorGroup == null) return false;
            result.center = this.currentPosition - result.doorGroup.roomPoint;
            this.currentSeed = Rng.MutateNext(this.currentSeed);
            
            return true;
        }
    }
}