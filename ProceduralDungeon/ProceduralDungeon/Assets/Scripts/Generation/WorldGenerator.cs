using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Unity.Mathematics;
using UnityEngine;

namespace Generation
{
    [Serializable]
    public class WorldGenerator
    {
        private string currentSeed;
        [SerializeField]
        private List<AreaData> areaData;
        [SerializeField]
        private int roomRepetitionAllowance = 2;
        [SerializeField]
        private int walkDirectionRepetitionAllowance = 2;

        private MonoBehaviour owner;
        private Action<GenerationResult> onUpdate;

        public static readonly Vector2Int[] CardinalDirections = {
            Vector2Int.up,    // (0, 1)
            Vector2Int.down,  // (0, -1)
            Vector2Int.left,  // (-1, 0)
            Vector2Int.right  // (1, 0)
        };

        public class WorldGenData
        {
            public string currentSeed;
            public RoomType? lastHadRoomType;
            public List<RoomData> hadRooms = new();
            public Vector2Int currentWalkDirection = Vector2Int.zero;
            public Vector2Int currentPosition = Vector2Int.zero;
            public List<AreaData> backlog;
            public List<AreaGenData> hadAreas = new();
            public Grid grid = new();
            public int walkDirRepeated = 0;
            public readonly float minDistanceBossRoom;

            public WorldGenData(List<AreaData> areaData, string seed, float minDistanceBossRoom)
            {
                currentSeed = seed;
                backlog = new List<AreaData>(areaData);
                this.minDistanceBossRoom = minDistanceBossRoom;
            }

            public void AddToHadRooms(RoomData foundRoom, int roomRepetitionAllowance)
            {
                hadRooms.Add(foundRoom);
                if (hadRooms.Count >= roomRepetitionAllowance)
                    hadRooms.RemoveRange(hadRooms.Count - roomRepetitionAllowance, hadRooms.Count);
            }
        }
        
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
            currentSeed = seed;
            var genData = new WorldGenData(areaData, seed, minDistanceBossRoom);

            Debug.Log("Starting generator");

            var addBossResult = new AddRoomResult();
            while (true)
            {
                yield return owner.StartCoroutine(WalkthroughAreas(genData));

                while (true)
                {
                    yield return owner.StartCoroutine(AddRoom(genData, genData.hadAreas.Last(), addBossResult, true));
                    if (addBossResult.instance != null)
                        break;
                }
                
                // Any end analyze should go here
                if (Vector2.Distance(genData.currentPosition, Vector2.zero) >= minDistanceBossRoom)
                    break;

                currentSeed = RNG.MutateNext(seed);
                genData = new WorldGenData(areaData, currentSeed, minDistanceBossRoom);
                addBossResult = new();
            }
            
            genData.grid.RemoveUnusedDoorways();
            
            result.grid = genData.grid;
            result.currentPosition = Vector2Int.zero;
            
            onFinish.Invoke();
        }

        private IEnumerator WalkthroughAreas(WorldGenData genData)
        {
            while (genData.backlog.Count > 0)
            {
                Debug.Log($"Walking through area backlog, current count: {genData.backlog.Count}");
                var foundIndex = RNG.RandomRange(0, genData.backlog.Count, genData.currentSeed);
                genData.currentSeed = RNG.MutateNext(genData.currentSeed);
                
                var pickedArea = genData.backlog[foundIndex].GetAreaGenData(genData.currentSeed);
                genData.currentSeed = RNG.MutateNext(genData.currentSeed);
                
                genData.backlog.RemoveAt(foundIndex);
                yield return owner.StartCoroutine(WalkthroughArea(genData, pickedArea));
                genData.hadAreas.Add(pickedArea);
                
                yield return GetWaitTime();
            }

            Debug.Log("Done with walking through areas");
        }

        private IEnumerator WalkthroughArea(WorldGenData genData, AreaGenData pickedArea)
        {
            while (pickedArea.Size > 0)
            {
                Debug.Log($"Walking through area: {pickedArea.AreaType}, current size left: {pickedArea.Size}\nCurrent position is: {genData.currentPosition}");
                var foundRoom = genData.grid.GetRoomAtPosition(genData.currentPosition);

                if (foundRoom == null)
                {
                    Debug.Log("Current walk position empty, trying to add room");
                    var result = new AddRoomResult();
                    yield return owner.StartCoroutine(AddRoom(genData, pickedArea, result));

                    // On addition room fail, check if area is still possible
                    if (result.instance == null)
                    {
                        if (pickedArea.Size <= 0)
                            break;
                        continue;
                    }
                    
                    Debug.Log("Room added");
                    foundRoom = result.instance;
                    genData.currentSeed = RNG.MutateNext(genData.currentSeed);
                }

                if (pickedArea.Size <= 0)
                    break;
                
                Walk(genData, foundRoom);

                Debug.Log("AAAAAAAAAAA");
                
                yield return GetWaitTime();
            }
        }

        private void Walk(WorldGenData genData, RoomInstance currentRoom)
        {
            while (true)
            {
                var index = RNG.RandomRange(0, CardinalDirections.Length, genData.currentSeed);
                genData.currentSeed = RNG.MutateNext(genData.currentSeed);

                if (CardinalDirections[index] == genData.currentWalkDirection)
                    genData.walkDirRepeated++;
                else
                    genData.walkDirRepeated = 0;

                if (genData.walkDirRepeated > walkDirectionRepetitionAllowance) continue;
                    
                genData.currentWalkDirection = CardinalDirections[index];
                break;
            }

            // Move to doorway of room
            var doorways = currentRoom.dataRef.DoorPoints.First(dir => dir.key == genData.currentWalkDirection).value;
            var doorway = doorways[RNG.RandomRange(0, doorways.Count, genData.currentSeed)];
            var newPos = currentRoom.position + doorway.roomPoint + genData.currentWalkDirection;
            currentRoom.RemoveDoorFromLayout(doorway);
            genData.currentSeed = RNG.MutateNext(genData.currentSeed);
                
            Debug.Log($"Walking from {genData.currentPosition} to {newPos}");
            genData.currentPosition = newPos;
                
            onUpdate.Invoke(new GenerationResult{grid = genData.grid, currentPosition = genData.currentPosition});
        }

        private class AddRoomResult
        {
            public RoomInstance instance;
        }
        private IEnumerator AddRoom(WorldGenData genData, AreaGenData pickedArea, AddRoomResult result, bool shouldUseBossPool = false)
        {
            var pickedTypeList = pickedArea.PickTypeList(genData);
            pickedTypeList.OnPicked(pickedArea);
            genData.currentSeed = RNG.MutateNext(genData.currentSeed);

            GetRoomResult getRoomResult = new();
            if (!shouldUseBossPool)
                yield return owner.StartCoroutine(TryGetTypedRoom(getRoomResult, pickedArea, genData, pickedTypeList));
            else        
                yield return owner.StartCoroutine(TryGetBossRoom(getRoomResult, pickedArea, genData));
            
            if (getRoomResult.foundRoom == null)
                yield break;
            
            var placedRoom = genData.grid.PlaceRoom(getRoomResult.foundRoom, getRoomResult.center.Value);
            if (getRoomResult.doorGroup != null)
                placedRoom.RemoveDoorFromLayout(getRoomResult.doorGroup);
            placedRoom.areaType = pickedArea.AreaType;
            placedRoom.roomType = pickedTypeList.roomType;

            Debug.Log($"Adding room of size: {getRoomResult.foundRoom.Size}");
            genData.AddToHadRooms(getRoomResult.foundRoom, roomRepetitionAllowance);

            pickedArea.Size -= getRoomResult.foundRoom.Size;
            genData.lastHadRoomType = pickedTypeList.roomType; // TODO: make allowance unique per type per area?

            result.instance = placedRoom;
            onUpdate.Invoke(new GenerationResult{grid = genData.grid, currentPosition = genData.currentPosition});
        }

        /// <summary>
        /// Uses duplicated code to pull from the endRooms list, and spawn a boss room somewhere adjacent
        /// </summary>
        private IEnumerator TryGetBossRoom(GetRoomResult result, AreaGenData pickedArea, WorldGenData genData)
        {
            var tries = 0;
            const int maxTries = 16;
            const int lastTry = 64;
            var pool = pickedArea.EndRooms;
            if (pool.Count <= 0)
                yield break;

            List<GetRoomResult> fallbacks = new();
            while (true)
            {
                result = new();
                result.foundRoom = pool[RNG.RandomRange(0, pool.Count, genData.currentSeed)];
                genData.currentSeed = RNG.MutateNext(genData.currentSeed);
                
                // move away based on doorway
                var doorways = result.foundRoom.DoorPoints.First(dir => dir.key == -genData.currentWalkDirection).value;
                result.doorGroup = doorways[RNG.RandomRange(0, doorways.Count, genData.currentSeed)];
                result.center = genData.currentPosition - result.doorGroup.roomPoint;
                genData.currentSeed = RNG.MutateNext(genData.currentSeed);
                
                onUpdate.Invoke(new GenerationResult{grid = genData.grid, currentPosition = result.center.Value});

                if (genData.grid.CheckRoomPossible(result.foundRoom, result.center.Value, out var hit))
                {
                    if (Vector2.Distance(result.center.Value, Vector2.zero) >= genData.minDistanceBossRoom)
                        break;
                    fallbacks.Add(result);
                    break;
                }

                if (tries >= maxTries && hit != null) Walk(genData, hit);

                if (tries > lastTry)
                {
                    result = fallbacks.Last();
                    genData.currentPosition = result.center.Value;
                    break;
                }
                
                genData.currentSeed = RNG.MutateNext(genData.currentSeed);
                tries++;
                yield return GetWaitTime();
            }
        }

        private IEnumerator TryGetTypedRoom(GetRoomResult getRoomResult, AreaGenData pickedArea, WorldGenData genData, RoomTypeDataList pickedTypeList)
        {
            var tries = 0;
            var maxTries = pickedArea.RoomCount * 2;
            while (getRoomResult.foundRoom == null)
            {
                yield return owner.StartCoroutine(TryGetRoom(genData, pickedTypeList, pickedArea, getRoomResult));

                if (getRoomResult.foundRoom != null)
                    break;

                if (tries >= maxTries)
                    yield break;

                pickedTypeList.UndoPicked(pickedArea);
                pickedTypeList = pickedArea.PickTypeList(genData);
                pickedTypeList.OnPicked(pickedArea);
                genData.currentSeed = RNG.MutateNext(genData.currentSeed);

                tries++;
            }
        }

        private class GetRoomResult
        {
            public RoomData foundRoom = null;
            public Vector2Int? center = null;
            public RoomData.DoorPointGroup doorGroup = null;
        }
        private IEnumerator TryGetRoom(WorldGenData genData, RoomTypeDataList pickedTypeList, AreaGenData pickedArea,
            GetRoomResult result)
        {
            var tries = 0;
            const int maxTries = 64;
            
            var overlapAttempts = 0;
            const int maxOverlapAttempts = 8;
            const int maxOverlapAttemptsBruteForce = 16;

            List<RoomData> hadRooms = new();
            var maxPool = pickedTypeList.Rooms.RoomData.Where(room => room.Size <= pickedArea.Size).ToList();
            
            while (true)
            {
                if (pickedTypeList.smallestRoomSize > pickedArea.Size)
                {
                    Debug.Log("No rooms exist that can fill area quota");
                    pickedArea.Size = 0;
                    break;
                }

                var sizedPool = maxPool.Where(room => room.Size <= math.max(pickedTypeList.smallestRoomSize, pickedArea.Size)).ToList();
                result.foundRoom = pickedTypeList.TryGetRoom(genData, sizedPool, genData.hadRooms.ToArray());
                if (result.foundRoom == null) // TryGetRoom failed
                {
                    if (tries > maxTries)
                    {
                        genData.hadRooms.Clear();
                        genData.lastHadRoomType = null;
                        Debug.Log($"Need room:\nsize between: {pickedTypeList.smallestRoomSize}, {pickedArea.Size}\nType:{pickedTypeList.roomType} Area:{pickedArea.AreaType}");

                        #if UNITY_EDITOR
                        break;
                        #else
                        continue;
                        #endif
                    }
                    genData.currentSeed = RNG.MutateNext(genData.currentSeed);
                    tries++;
                    continue;
                }

                if (hadRooms.Contains(result.foundRoom))
                    hadRooms.Add(result.foundRoom);

                if (genData.currentWalkDirection == Vector2.zero)
                {
                    result.center = genData.currentPosition + new Vector2Int(
                        genData.currentWalkDirection.x * (result.foundRoom.Width / 2),
                        genData.currentWalkDirection.y * (result.foundRoom.Height / 2)
                    );
                }
                else
                {
                    // move away based on doorway
                    var doorways = result.foundRoom.DoorPoints.First(dir => dir.key == -genData.currentWalkDirection).value;
                    result.doorGroup = doorways[RNG.RandomRange(0, doorways.Count, genData.currentSeed)];
                    result.center = genData.currentPosition - result.doorGroup.roomPoint;
                    genData.currentSeed = RNG.MutateNext(genData.currentSeed);
                }
                onUpdate.Invoke(new GenerationResult{grid = genData.grid, currentPosition = result.center.Value});
                
                if (genData.grid.CheckRoomPossible(result.foundRoom, result.center.Value, out _))
                    break;
                
                // Debug.Log($"Cant place room of size {result.foundRoom.Size}, at {result.center.Value}");
                overlapAttempts++;
                if (overlapAttempts > maxOverlapAttempts)
                {
                    var neighbour = genData.grid.GetRoomAtPosition(result.center.Value);
                    if (neighbour != null)
                        Walk(genData, neighbour);
                    if (overlapAttempts >= maxOverlapAttemptsBruteForce)
                    {
                        genData.hadRooms.Clear();
                        genData.lastHadRoomType = null;
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