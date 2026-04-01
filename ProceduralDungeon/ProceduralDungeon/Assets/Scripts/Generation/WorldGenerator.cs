using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
        
        public static readonly Vector2Int[] CardinalDirections = new Vector2Int[]
        {
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

            public WorldGenData(List<AreaData> areaData, string seed)
            {
                currentSeed = seed;
                backlog = new List<AreaData>(areaData);
            }

            public void AddToHadRooms(RoomData foundRoom, int roomRepetitionAllowance)
            {
                hadRooms.Add(foundRoom);
                if (hadRooms.Count >= roomRepetitionAllowance)
                    hadRooms.RemoveRange(hadRooms.Count - roomRepetitionAllowance, hadRooms.Count);
            }
        }

        public static YieldInstruction GetWaitTime() => new WaitForSeconds(0.1f);  //null;//
        
        public void SetOwner(MonoBehaviour owner) => this.owner = owner;

        public class GenerationResult
        {
            public Grid grid;
        }
        public IEnumerator Generate(string seed, GenerationResult result, Action onFinish)
        {
            currentSeed = seed;
            var genData = new WorldGenData(areaData, seed);

            Debug.Log("Starting generator");
            yield return owner.StartCoroutine(WalkthroughAreas(genData));

            // TODO spawn bossroom
            
            result.grid = genData.grid;
            
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
                Debug.Log($"Walking through area, current size left: {pickedArea.Size}\nCurrent position is: {genData.currentPosition}");
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
                genData.currentPosition = foundRoom.position;
                
                // TODO add doorway to last been to existing room

                if (pickedArea.Size <= 0)
                    break;
                
                Walk(genData, foundRoom);
                
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

            // Move to edge of room
            var newPos = genData.currentPosition + new Vector2Int(
                genData.currentWalkDirection.x * (currentRoom.dataRef.Width / 2),
                genData.currentWalkDirection.y * (currentRoom.dataRef.Height / 2)
            );
            
            Debug.Log($"Walking from {genData.currentPosition} to {newPos}");
            genData.currentPosition = newPos;
        }

        private class AddRoomResult
        {
            public RoomInstance instance = null;
        }
        private IEnumerator AddRoom(WorldGenData genData, AreaGenData pickedArea, AddRoomResult result)
        {
            var pickedTypeList = pickedArea.PickTypeList(genData);
            pickedTypeList.OnPicked(pickedArea);
            genData.currentSeed = RNG.MutateNext(genData.currentSeed);

            GetRoomResult getRoomResult = new();
            yield return owner.StartCoroutine(TryGetRoom(genData, pickedTypeList, pickedArea, getRoomResult));

            if (getRoomResult.foundRoom == null)
            {
                result.instance = null;
                yield break;
            }
            
            var placedRoom = genData.grid.PlaceRoom(getRoomResult.foundRoom, getRoomResult.center.Value);

            Debug.Log($"Adding room of size: {getRoomResult.foundRoom.Size}");
            genData.AddToHadRooms(getRoomResult.foundRoom, roomRepetitionAllowance);

            pickedArea.Size -= getRoomResult.foundRoom.Size;
            genData.lastHadRoomType = pickedTypeList.roomType; // TODO: make allowance unique per type per area?

            result.instance = placedRoom;
        }

        private class GetRoomResult
        {
            public RoomData foundRoom = null;
            public Vector2Int? center = null;
        }
        private IEnumerator TryGetRoom(WorldGenData genData, RoomTypeDataList pickedTypeList, AreaGenData pickedArea,
            GetRoomResult result)
        {
            #if UNITY_EDITOR
            var tries = 0;
            const int maxTries = 64;
            #endif
            while (true)
            {
                if (pickedTypeList.smallestRoomSize > pickedArea.Size)
                {
                    Debug.Log("No rooms exist that can fill area quota");
                    pickedArea.Size = 0;
                    break;
                }
                
                result.foundRoom = pickedTypeList.TryGetRoom(genData,
                    RNG.RandomRange(pickedTypeList.smallestRoomSize, pickedArea.Size, genData.currentSeed), 
                    genData.hadRooms.ToArray());

                if (result.foundRoom == null) // TryGetRoom failed
                {
                    #if UNITY_EDITOR
                    if (tries > maxTries)
                    {
                        Debug.Log($"Need room:\nsize between: {pickedTypeList.smallestRoomSize}, {pickedArea.Size}\nType:{pickedTypeList.roomType} Area:{pickedArea.AreaType}");
                        break;
                    }
                    genData.currentSeed = RNG.MutateNext(genData.currentSeed);
                    tries++;
                    #endif
                    continue;
                }
                
                result.center = genData.currentPosition + new Vector2Int(
                    genData.currentWalkDirection.x * (result.foundRoom.Width / 2),
                    genData.currentWalkDirection.y * (result.foundRoom.Height / 2)
                );
                if (genData.grid.CheckRoomPossible(result.foundRoom, result.center.Value))
                    break;
                yield return GetWaitTime();
            }
        }
    }
}