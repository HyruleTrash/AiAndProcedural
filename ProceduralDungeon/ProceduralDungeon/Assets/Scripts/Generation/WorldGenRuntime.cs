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
        
        private Vector2Int currentWalkDirection = Vector2Int.zero;
        private Vector2Int currentPosition = Vector2Int.zero;
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
        
        private void Reset(List<Area> areaData, string seed)
        {
            this.currentSeed = seed;
            this.backlog = new List<Area>(areaData);

            this.currentWalkDirection = Vector2Int.zero;
            this.currentPosition = Vector2Int.zero;
            this.walkDirRepeated = 0;
        }

        private void AddToHadRooms(Room foundRoom, int roomRepetitionAllowance)
        {
            this.hadRooms.Add(foundRoom);
            if (this.hadRooms.Count >= roomRepetitionAllowance) this.hadRooms.RemoveRange(this.hadRooms.Count - roomRepetitionAllowance, this.hadRooms.Count);
        }

        private class RoomRuntimeRef
        {
            public RoomRuntime? runtime;
        }
        public IEnumerator StartGen(string seed, List<Area> areaData)
        {
            RoomRuntimeRef bossRoomRef = new();
            while (true)
            {
                yield return this.unityConnection.StartCoroutine(WalkthroughAreas(this));

                while (true)
                {
                    yield return this.unityConnection.StartCoroutine(AddRoom(this, this.hadAreas.Last(), bossRoomRef!, true));
                    if (bossRoomRef != null) break;
                }
                
                // ANALYSE HERE
                if (Vector2.Distance(this.currentPosition, Vector2.zero) >= this.minDistToBossRoom) break;

                this.currentSeed = Rng.MutateNext(seed);
                Reset(areaData, this.currentSeed);
                bossRoomRef = new RoomRuntimeRef();
            }

            this.gridRuntime.RemoveUnusedDoorways();
        }

        private IEnumerator WalkthroughAreas(WorldGenRuntime genRuntime)
        {
            while (genRuntime.backlog.Count > 0)
            {
                NotificationManager.Log($"Walking through area backlog, current count: {genRuntime.backlog.Count}");
                int foundIndex = Rng.RandomRange(0, genRuntime.backlog.Count, genRuntime.currentSeed);
                genRuntime.currentSeed = Rng.MutateNext(genRuntime.currentSeed);
                
                AreaRuntime pickedArea = genRuntime.backlog[foundIndex].GetAreaGenData(genRuntime.currentSeed);
                genRuntime.currentSeed = Rng.MutateNext(genRuntime.currentSeed);
                
                genRuntime.backlog.RemoveAt(foundIndex);
                yield return this.unityConnection.StartCoroutine(WalkthroughArea(genRuntime, pickedArea));
                genRuntime.hadAreas.Add(pickedArea);
                
                yield return this.owner.GetAnimWaitTime();
            }

            NotificationManager.Log("Done with walking through areas");
        }

        private IEnumerator WalkthroughArea(WorldGenRuntime genRuntime, AreaRuntime pickedArea)
        {
            while (pickedArea.Size > 0)
            {
                NotificationManager.Log($"Walking through area: {pickedArea.AreaType}, current size left: {pickedArea.Size}\nCurrent position is: {genRuntime.currentPosition}");
                RoomRuntime? foundRoom = genRuntime.gridRuntime.GetRoomAtPosition(genRuntime.currentPosition);

                if (foundRoom == null)
                {
                    NotificationManager.Log("Current walk position empty, trying to add room");
                    RoomRuntimeRef runtimeRef = new();
                    yield return this.unityConnection.StartCoroutine(AddRoom(genRuntime, pickedArea, runtimeRef));

                    // When the addition of a room fails, check if area is still possible
                    if (runtimeRef.runtime == null)
                    {
                        if (pickedArea.Size <= 0) break;
                        continue;
                    }
                    
                    NotificationManager.Log("Room added");
                    foundRoom = runtimeRef.runtime;
                    genRuntime.currentSeed = Rng.MutateNext(genRuntime.currentSeed);
                }

                if (pickedArea.Size <= 0) break;
                Walk(genRuntime, foundRoom);
                
                yield return this.owner.GetAnimWaitTime();
            }
        }

        private void Walk(WorldGenRuntime genRuntime, RoomRuntime currentRoom)
        {
            while (true)
            {
                int index = Rng.RandomRange(0, Extensions.CardinalDirections.Length, genRuntime.currentSeed);
                genRuntime.currentSeed = Rng.MutateNext(genRuntime.currentSeed);

                if (Extensions.CardinalDirections[index] == genRuntime.currentWalkDirection)
                    genRuntime.walkDirRepeated++;
                else
                    genRuntime.walkDirRepeated = 0;

                if (genRuntime.walkDirRepeated > this.owner.WalkDirectionRepetitionAllowance) continue;
                    
                genRuntime.currentWalkDirection = Extensions.CardinalDirections[index];
                break;
            }

            // Move to doorway of room
            List<Room.DoorPointGroup> doorways = currentRoom.roomRef.DoorPoints.First(dir => dir.key == genRuntime.currentWalkDirection).value;
            Room.DoorPointGroup doorway = doorways[Rng.RandomRange(0, doorways.Count, genRuntime.currentSeed)];
            Vector2Int newPos = currentRoom.position + doorway.roomPoint + genRuntime.currentWalkDirection;
            currentRoom.RemoveDoorFromLayout(doorway);
            genRuntime.currentSeed = Rng.MutateNext(genRuntime.currentSeed);
                
            NotificationManager.Log($"Walking from {genRuntime.currentPosition} to {newPos}");
            genRuntime.currentPosition = newPos;

            Action<WorldGenSnapshot> onUpdateSnapshot = this.owner.GetOnUpdateSnapshot(this);
            onUpdateSnapshot?.Invoke(new WorldGenSnapshot(genRuntime.gridRuntime, genRuntime.currentPosition));
        }

        private IEnumerator AddRoom(WorldGenRuntime genRuntime, AreaRuntime pickedArea, RoomRuntimeRef runtimeRef, bool shouldUseBossPool = false)
        {
            TypedRoomList pickedTypeList = pickedArea.PickTypeList(genRuntime);
            pickedTypeList.OnPicked(pickedArea);
            genRuntime.currentSeed = Rng.MutateNext(genRuntime.currentSeed);

            GetRoomResult getRoomResult = new();
            if (!shouldUseBossPool)
                yield return this.unityConnection.StartCoroutine(TryGetTypedRoom(getRoomResult, pickedArea, genRuntime, pickedTypeList));
            else        
                yield return this.unityConnection.StartCoroutine(TryGetBossRoom(getRoomResult, pickedArea, genRuntime));
            
            if (getRoomResult.foundRoom == null)
                yield break;

            if (getRoomResult.center != null)
            {
                RoomRuntime placedRoom = genRuntime.gridRuntime.PlaceRoom(getRoomResult.foundRoom, getRoomResult.center.Value);
                if (getRoomResult.doorGroup != null)
                    placedRoom.RemoveDoorFromLayout(getRoomResult.doorGroup);
                placedRoom.areaType = pickedArea.AreaType;
                placedRoom.roomType = pickedTypeList.roomType;

                NotificationManager.Log($"Adding room of size: {getRoomResult.foundRoom.Size}");
                genRuntime.AddToHadRooms(getRoomResult.foundRoom, this.owner.RoomRepetitionAllowance);

                pickedArea.Size -= getRoomResult.foundRoom.Size;
                genRuntime.lastHadRoomType = pickedTypeList.roomType; // TODO: make allowance unique per type per area?

                runtimeRef.runtime = placedRoom;
            }

            Action<WorldGenSnapshot> onUpdateSnapshot = this.owner.GetOnUpdateSnapshot(this);
            onUpdateSnapshot?.Invoke(new WorldGenSnapshot(genRuntime.gridRuntime, genRuntime.currentPosition));
        }

        /// <summary>
        /// Uses duplicated code to pull from the endRooms list, and spawn a boss room somewhere adjacent
        /// </summary>
        private IEnumerator TryGetBossRoom(GetRoomResult result, AreaRuntime pickedArea, WorldGenRuntime genRuntime)
        {
            int tries = 0;
            const int maxTries = 16;
            const int lastTry = 64;
            List<Room> pool = pickedArea.EndRooms;
            if (pool.Count <= 0)
                yield break;

            List<GetRoomResult> fallbacks = new();
            while (true)
            {
                result = new GetRoomResult
                {
                    foundRoom = pool[Rng.RandomRange(0, pool.Count, genRuntime.currentSeed)]
                };
                genRuntime.currentSeed = Rng.MutateNext(genRuntime.currentSeed);
                
                // move away based on doorway
                List<Room.DoorPointGroup> doorways = result.foundRoom.DoorPoints.First(dir => dir.key == -genRuntime.currentWalkDirection).value;
                result.doorGroup = doorways[Rng.RandomRange(0, doorways.Count, genRuntime.currentSeed)];
                result.center = genRuntime.currentPosition - result.doorGroup.roomPoint;
                genRuntime.currentSeed = Rng.MutateNext(genRuntime.currentSeed);
                
                Action<WorldGenSnapshot> onUpdateSnapshot = this.owner.GetOnUpdateSnapshot(this);
                onUpdateSnapshot?.Invoke(new WorldGenSnapshot(genRuntime.gridRuntime, result.center.Value));

                if (genRuntime.gridRuntime.CheckRoomPossible(result.foundRoom, result.center.Value, out RoomRuntime? hit))
                {
                    if (Vector2.Distance(result.center.Value, Vector2.zero) >= genRuntime.minDistToBossRoom)
                        break;
                    fallbacks.Add(result);
                    break;
                }

                if (tries >= maxTries && hit != null) Walk(genRuntime, hit);

                if (tries > lastTry)
                {
                    result = fallbacks.Last();
                    if (result.center != null) genRuntime.currentPosition = result.center.Value;
                    break;
                }
                
                genRuntime.currentSeed = Rng.MutateNext(genRuntime.currentSeed);
                tries++;
                yield return this.owner.GetAnimWaitTime();
            }
        }

        private IEnumerator TryGetTypedRoom(GetRoomResult getRoomResult, AreaRuntime pickedArea, WorldGenRuntime genRuntime, TypedRoomList pickedTypeList)
        {
            int tries = 0;
            int maxTries = pickedArea.RoomCount * 2;
            while (getRoomResult.foundRoom == null)
            {
                yield return this.unityConnection.StartCoroutine(TryGetRoom(genRuntime, pickedTypeList, pickedArea, getRoomResult));

                if (getRoomResult.foundRoom != null)
                    break;

                if (tries >= maxTries)
                    yield break;

                pickedTypeList.UndoPicked(pickedArea);
                pickedTypeList = pickedArea.PickTypeList(genRuntime);
                pickedTypeList.OnPicked(pickedArea);
                genRuntime.currentSeed = Rng.MutateNext(genRuntime.currentSeed);

                tries++;
            }
        }

        private class GetRoomResult
        {
            public Room? foundRoom;
            public Vector2Int? center;
            public Room.DoorPointGroup? doorGroup;
        }
        private IEnumerator TryGetRoom(WorldGenRuntime genRuntime, TypedRoomList pickedTypeList, AreaRuntime pickedArea,
            GetRoomResult result)
        {
            int tries = 0;
            const int maxTries = 64;
            
            int overlapAttempts = 0;
            const int maxOverlapAttempts = 8;
            const int maxOverlapAttemptsBruteForce = 16;

            List<Room> hadRooms = new();
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
                result.foundRoom = TypedRoomList.TryGetRoom(genRuntime, sizedPool, genRuntime.hadRooms.ToArray());
                if (result.foundRoom == null) // TryGetRoom failed
                {
                    if (tries > maxTries)
                    {
                        genRuntime.hadRooms.Clear();
                        genRuntime.lastHadRoomType = null;
                        NotificationManager.Log($"Need room:\nsize between: {pickedTypeList.smallestRoomSize}, {pickedArea.Size}\nType:{pickedTypeList.roomType} Area:{pickedArea.AreaType}");

#if UNITY_EDITOR
                        break;
#else
                        continue;
#endif
                    }
                    genRuntime.currentSeed = Rng.MutateNext(genRuntime.currentSeed);
                    tries++;
                    continue;
                }

                if (hadRooms.Contains(result.foundRoom))
                    hadRooms.Add(result.foundRoom);

                if (genRuntime.currentWalkDirection == Vector2.zero)
                {
                    result.center = genRuntime.currentPosition + new Vector2Int(
                        genRuntime.currentWalkDirection.x * (result.foundRoom.Width / 2),
                        genRuntime.currentWalkDirection.y * (result.foundRoom.Height / 2)
                    );
                }
                else
                {
                    // move away based on doorway
                    List<Room.DoorPointGroup> doorways = result.foundRoom.DoorPoints.First(dir => dir.key == -genRuntime.currentWalkDirection).value;
                    result.doorGroup = doorways[Rng.RandomRange(0, doorways.Count, genRuntime.currentSeed)];
                    result.center = genRuntime.currentPosition - result.doorGroup.roomPoint;
                    genRuntime.currentSeed = Rng.MutateNext(genRuntime.currentSeed);
                }
                
                Action<WorldGenSnapshot> onUpdateSnapshot = this.owner.GetOnUpdateSnapshot(this);
                onUpdateSnapshot?.Invoke(new WorldGenSnapshot(genRuntime.gridRuntime, result.center.Value));
                
                if (genRuntime.gridRuntime.CheckRoomPossible(result.foundRoom, result.center.Value, out _))
                    break;
                
                // NotificationManager.Log($"Cant place room of size {result.foundRoom.Size}, at {result.center.Value}");
                overlapAttempts++;
                if (overlapAttempts > maxOverlapAttempts)
                {
                    RoomRuntime? neighbour = genRuntime.gridRuntime.GetRoomAtPosition(result.center.Value);
                    if (neighbour != null) Walk(genRuntime, neighbour);
                    
                    if (overlapAttempts >= maxOverlapAttemptsBruteForce)
                    {
                        genRuntime.hadRooms.Clear();
                        genRuntime.lastHadRoomType = null;
                    }
                }

                if (hadRooms.Count >= maxPool.Count)
                {
                    result.foundRoom = null;
                    yield break;
                }
                
                yield return this.owner.GetAnimWaitTime();
            }
        }
    }
}