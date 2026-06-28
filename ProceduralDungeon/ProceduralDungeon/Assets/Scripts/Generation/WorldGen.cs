using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Unity.Mathematics;
using UnityEngine;
using Util;

namespace Generation
{
    [Serializable]
    public class WorldGen
    {
        private string currentSeed;
        [SerializeField]
        private List<Area> areaData;
        [SerializeField]
        private int roomRepetitionAllowance = 2;
        [SerializeField]
        private int walkDirectionRepetitionAllowance = 2;

        private MonoBehaviour owner;
        private Action<GenerationResult> onUpdate;
        
        public static float WaitTime;
        public static YieldInstruction GetWaitTime() => WaitTime <= 0f ? null : new WaitForSeconds(WaitTime);
        public void SetOwner(MonoBehaviour owner) => this.owner = owner;

        public class GenerationResult
        {
            public Grid grid;
            public Vector2Int currentPosition;
        }
        public IEnumerator Generate(string seed, GenerationResult result, Action onFinish,
            Action<GenerationResult> onUpdate, float minDistanceBossRoom)
        {
            this.onUpdate = onUpdate;
            this.currentSeed = seed;
            WorldGenRuntime genRuntime = new(this.areaData, seed, minDistanceBossRoom);

            Debug.Log("Starting generator");

            AddRoomResult addBossResult = new();
            while (true)
            {
                yield return this.owner.StartCoroutine(WalkthroughAreas(genRuntime));

                while (true)
                {
                    yield return this.owner.StartCoroutine(AddRoom(genRuntime, genRuntime.hadAreas.Last(), addBossResult, true));
                    if (addBossResult.runtime != null)
                        break;
                }
                
                // Any end analyze should go here
                if (Vector2.Distance(genRuntime.currentPosition, Vector2.zero) >= minDistanceBossRoom)
                    break;

                this.currentSeed = Rng.MutateNext(seed);
                genRuntime = new WorldGenRuntime(this.areaData, this.currentSeed, minDistanceBossRoom);
                addBossResult = new AddRoomResult();
            }
            
            genRuntime.grid.RemoveUnusedDoorways();
            
            result.grid = genRuntime.grid;
            result.currentPosition = Vector2Int.zero;
            
            onFinish.Invoke();
        }

        private IEnumerator WalkthroughAreas(WorldGenRuntime genRuntime)
        {
            while (genRuntime.backlog.Count > 0)
            {
                Debug.Log($"Walking through area backlog, current count: {genRuntime.backlog.Count}");
                int foundIndex = Rng.RandomRange(0, genRuntime.backlog.Count, genRuntime.currentSeed);
                genRuntime.currentSeed = Rng.MutateNext(genRuntime.currentSeed);
                
                AreaRuntime pickedArea = genRuntime.backlog[foundIndex].GetAreaGenData(genRuntime.currentSeed);
                genRuntime.currentSeed = Rng.MutateNext(genRuntime.currentSeed);
                
                genRuntime.backlog.RemoveAt(foundIndex);
                yield return this.owner.StartCoroutine(WalkthroughArea(genRuntime, pickedArea));
                genRuntime.hadAreas.Add(pickedArea);
                
                yield return GetWaitTime();
            }

            Debug.Log("Done with walking through areas");
        }

        private IEnumerator WalkthroughArea(WorldGenRuntime genRuntime, AreaRuntime pickedArea)
        {
            while (pickedArea.Size > 0)
            {
                Debug.Log($"Walking through area: {pickedArea.AreaType}, current size left: {pickedArea.Size}\nCurrent position is: {genRuntime.currentPosition}");
                RoomRuntime foundRoom = genRuntime.grid.GetRoomAtPosition(genRuntime.currentPosition);

                if (foundRoom == null)
                {
                    Debug.Log("Current walk position empty, trying to add room");
                    AddRoomResult result = new();
                    yield return this.owner.StartCoroutine(AddRoom(genRuntime, pickedArea, result));

                    // On addition room fail, check if area is still possible
                    if (result.runtime == null)
                    {
                        if (pickedArea.Size <= 0)
                            break;
                        continue;
                    }
                    
                    Debug.Log("Room added");
                    foundRoom = result.runtime;
                    genRuntime.currentSeed = Rng.MutateNext(genRuntime.currentSeed);
                }

                if (pickedArea.Size <= 0)
                    break;
                
                Walk(genRuntime, foundRoom);

                Debug.Log("AAAAAAAAAAA");
                
                yield return GetWaitTime();
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

                if (genRuntime.walkDirRepeated > this.walkDirectionRepetitionAllowance) continue;
                    
                genRuntime.currentWalkDirection = Extensions.CardinalDirections[index];
                break;
            }

            // Move to doorway of room
            List<Room.DoorPointGroup> doorways = currentRoom.@ref.DoorPoints.First(dir => dir.key == genRuntime.currentWalkDirection).value;
            Room.DoorPointGroup doorway = doorways[Rng.RandomRange(0, doorways.Count, genRuntime.currentSeed)];
            Vector2Int newPos = currentRoom.position + doorway.roomPoint + genRuntime.currentWalkDirection;
            currentRoom.RemoveDoorFromLayout(doorway);
            genRuntime.currentSeed = Rng.MutateNext(genRuntime.currentSeed);
                
            Debug.Log($"Walking from {genRuntime.currentPosition} to {newPos}");
            genRuntime.currentPosition = newPos;

            this.onUpdate.Invoke(new GenerationResult{grid = genRuntime.grid, currentPosition = genRuntime.currentPosition});
        }

        private class AddRoomResult
        {
            public RoomRuntime runtime;
        }
        private IEnumerator AddRoom(WorldGenRuntime genRuntime, AreaRuntime pickedArea, AddRoomResult result, bool shouldUseBossPool = false)
        {
            TypedRoomList pickedTypeList = pickedArea.PickTypeList(genRuntime);
            pickedTypeList.OnPicked(pickedArea);
            genRuntime.currentSeed = Rng.MutateNext(genRuntime.currentSeed);

            GetRoomResult getRoomResult = new();
            if (!shouldUseBossPool)
                yield return this.owner.StartCoroutine(TryGetTypedRoom(getRoomResult, pickedArea, genRuntime, pickedTypeList));
            else        
                yield return this.owner.StartCoroutine(TryGetBossRoom(getRoomResult, pickedArea, genRuntime));
            
            if (getRoomResult.foundRoom == null)
                yield break;
            
            RoomRuntime placedRoom = genRuntime.grid.PlaceRoom(getRoomResult.foundRoom, getRoomResult.center.Value);
            if (getRoomResult.doorGroup != null)
                placedRoom.RemoveDoorFromLayout(getRoomResult.doorGroup);
            placedRoom.areaType = pickedArea.AreaType;
            placedRoom.roomType = pickedTypeList.roomType;

            Debug.Log($"Adding room of size: {getRoomResult.foundRoom.Size}");
            genRuntime.AddToHadRooms(getRoomResult.foundRoom, this.roomRepetitionAllowance);

            pickedArea.Size -= getRoomResult.foundRoom.Size;
            genRuntime.lastHadRoomType = pickedTypeList.roomType; // TODO: make allowance unique per type per area?

            result.runtime = placedRoom;
            this.onUpdate.Invoke(new GenerationResult{grid = genRuntime.grid, currentPosition = genRuntime.currentPosition});
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

                this.onUpdate.Invoke(new GenerationResult{grid = genRuntime.grid, currentPosition = result.center.Value});

                if (genRuntime.grid.CheckRoomPossible(result.foundRoom, result.center.Value, out RoomRuntime hit))
                {
                    if (Vector2.Distance(result.center.Value, Vector2.zero) >= genRuntime.minDistanceBossRoom)
                        break;
                    fallbacks.Add(result);
                    break;
                }

                if (tries >= maxTries && hit != null) Walk(genRuntime, hit);

                if (tries > lastTry)
                {
                    result = fallbacks.Last();
                    genRuntime.currentPosition = result.center.Value;
                    break;
                }
                
                genRuntime.currentSeed = Rng.MutateNext(genRuntime.currentSeed);
                tries++;
                yield return GetWaitTime();
            }
        }

        private IEnumerator TryGetTypedRoom(GetRoomResult getRoomResult, AreaRuntime pickedArea, WorldGenRuntime genRuntime, TypedRoomList pickedTypeList)
        {
            int tries = 0;
            int maxTries = pickedArea.RoomCount * 2;
            while (getRoomResult.foundRoom == null)
            {
                yield return this.owner.StartCoroutine(TryGetRoom(genRuntime, pickedTypeList, pickedArea, getRoomResult));

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
            public Room foundRoom = null;
            public Vector2Int? center = null;
            public Room.DoorPointGroup doorGroup = null;
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
                    Debug.Log("No rooms exist that can fill area quota");
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
                        Debug.Log($"Need room:\nsize between: {pickedTypeList.smallestRoomSize}, {pickedArea.Size}\nType:{pickedTypeList.roomType} Area:{pickedArea.AreaType}");

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

                this.onUpdate.Invoke(new GenerationResult{grid = genRuntime.grid, currentPosition = result.center.Value});
                
                if (genRuntime.grid.CheckRoomPossible(result.foundRoom, result.center.Value, out _))
                    break;
                
                // Debug.Log($"Cant place room of size {result.foundRoom.Size}, at {result.center.Value}");
                overlapAttempts++;
                if (overlapAttempts > maxOverlapAttempts)
                {
                    RoomRuntime neighbour = genRuntime.grid.GetRoomAtPosition(result.center.Value);
                    if (neighbour != null)
                        Walk(genRuntime, neighbour);
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
                
                yield return GetWaitTime();
            }
        }
    }
}