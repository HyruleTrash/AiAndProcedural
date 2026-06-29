using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Util;

namespace Generation
{
    /// <summary>
    /// Class used for holding live area gen data, has mutable data without breaking default editor data
    /// </summary>
    public class AreaRuntime
    {
        private readonly List<TypedRoomList> roomTypes;
        private readonly RoomList endRooms;
        private readonly int smallestEndRoomSize;
        public AreaType AreaType { get; }
        public int Size { get; set; }

        private int RoomCount => this.roomTypes.Count;
        public int EndRoomCount => this.endRooms.RoomData.Count;
        public List<Room> EndRooms => this.endRooms.RoomData;
        private RoomType? lastHadRoomType;

        public AreaRuntime(AreaType areaType, int size, List<TypedRoomList> roomTypes, RoomList endRooms, int smallestEndRoomSize)
        {
            this.AreaType = areaType;
            this.Size = size;
            this.roomTypes = new List<TypedRoomList>();
            this.endRooms = new RoomList(endRooms);
            this.smallestEndRoomSize = smallestEndRoomSize;
            foreach (TypedRoomList list in roomTypes) this.roomTypes.Add(list.Duplicate());
        }

        private TypedRoomList? PickTypeList(WorldGenRuntime genRuntime)
        {
            TypedRoomList foundTypeList;
            List<int> indexPool = new();
            const int resolution = 100;

            bool guaranteedFound = false;

            for (int i = 0; i < this.roomTypes.Count; i++)
            {
                if (!Mathf.Approximately(this.roomTypes[i].Weight, 1f)) continue;
                indexPool.Add(i);
                guaranteedFound = true;
            }

            if (!guaranteedFound)
            {
                for (int i = 0; i < this.roomTypes.Count; i++)
                {
                    int count = Mathf.RoundToInt(this.roomTypes[i].Weight * resolution);

                    for (int j = 0; j < count; j++)
                        indexPool.Add(i);
                }
            }

            if (indexPool.Count == 0)
            {
                Debug.LogWarning("indexPool is empty");
                return null;
            }
            
            if (this.lastHadRoomType != null)
            {
                bool hasValid = indexPool.Any(i => this.roomTypes[i].roomType != this.lastHadRoomType.Value);
                if (!hasValid)
                {
                    Debug.LogWarning("only non valid room types are left");
                    return null;
                }
            }
            while (true)
            {
                int foundIndex = indexPool[Rng.RandomRange(0, indexPool.Count, genRuntime.currentSeed)];
                genRuntime.MutateRng();
                foundTypeList = this.roomTypes[foundIndex];
                
                if (foundTypeList && (this.lastHadRoomType == null || foundTypeList.roomType != this.lastHadRoomType.Value)) break;
            }
            return foundTypeList;
        }

        public IEnumerator WalkthroughArea(WorldGenRuntime.AnimDataWrapper animData)
        {
            WorldGenRuntime genRuntime = animData.genRuntime;
            MonoBehaviour unityConnection = animData.unityConnection;
            float waitTime = animData.waitTime;
            YieldInstruction? yieldInstruction = animData.yieldInstruction;
            
            while (this.Size > 0)
            {
                NotificationManager.Log($"Walking through area: {this.AreaType}, current size left: {this.Size}\nCurrent position is: {genRuntime.CurrentPosition}", waitTime);
                RoomRuntime? foundRoom = genRuntime.GetRoomAtPosition(genRuntime.CurrentPosition);

                if (foundRoom == null)
                {
                    NotificationManager.Log("Current walk position empty, trying to add room", waitTime);
                    
                    PendingRoomPlacement pendingRoom = new();
                    yield return unityConnection.StartCoroutine(GetTypedPossibleRoom(animData, pendingRoom));
                    
                    RoomRuntimeRef runtimeRef = new();
                    yield return unityConnection.StartCoroutine(genRuntime.AddRoom(this, pendingRoom, runtimeRef));

                    // When the addition of a room fails, check if area is still possible
                    if (runtimeRef.instance == null)
                    {
                        if (this.Size <= 0) break;
                        continue;
                    }
                    
                    NotificationManager.Log("Room added", waitTime);
                    foundRoom = runtimeRef.instance;
                    this.lastHadRoomType = foundRoom.roomType; // TODO: make allowance unique per type per area?
                    genRuntime.MutateRng();
                }

                if (this.Size <= 0) break;
                genRuntime.Walk(foundRoom);
                
                if (yieldInstruction != null)
                    yield return yieldInstruction;
            }
        }
        
        private IEnumerator GetTypedPossibleRoom(WorldGenRuntime.AnimDataWrapper animData, PendingRoomPlacement pendingRoomPlacement)
        {
            WorldGenRuntime genRuntime = animData.genRuntime;
            MonoBehaviour unityConnection = animData.unityConnection;
            
            TypedRoomList? pickedTypeList = null;
            int tries = 0;
            int maxTries = this.RoomCount * 2;
            while (true)
            {
                genRuntime.MutateRng();
                pickedTypeList?.UndoPicked(this);
                pickedTypeList = PickTypeList(genRuntime);
                if (!pickedTypeList) continue;
                pickedTypeList.OnPicked(this);
                
                List<Room> maxPool = pickedTypeList.Rooms.RoomData.Where(room => room.Size <= this.Size).ToList();
                
                yield return unityConnection.StartCoroutine(genRuntime.TryToGetPossibleRoom(this, pendingRoomPlacement, maxPool, pickedTypeList.smallestRoomSize, pickedTypeList.roomType));

                if (pendingRoomPlacement.possibleRoom != null)
                {
                    pendingRoomPlacement.possibleRoomType = pickedTypeList.roomType;
                    break;
                }
                if (tries >= maxTries) yield break;

                tries++;
            }
        }

        /// <summary>
        /// Checks if there's a possible placement for a boss room, and mutates pendingRoom accordingly
        /// </summary>
        /// <returns></returns>
        public IEnumerator GetPossibleBossRoom(WorldGenRuntime.AnimDataWrapper animData, PendingRoomPlacement pendingRoom)
        {
            WorldGenRuntime genRuntime = animData.genRuntime;
            MonoBehaviour unityConnection = animData.unityConnection;
            
            int largestSize = this.endRooms.RoomData.Max(room => room.Size);
            bool isSpaceNeeded = this.Size < largestSize;
            List<Room> maxPool = this.endRooms.RoomData.Where(room => room.Size <= this.Size).ToList();
            
            if (isSpaceNeeded) this.Size += largestSize; // make area accommodate boss room
            
            yield return unityConnection.StartCoroutine(genRuntime.TryToGetPossibleRoom(this, pendingRoom, maxPool, this.smallestEndRoomSize, RoomType.EndRoom));

            if (pendingRoom.possibleRoom == null) yield break;
            pendingRoom.possibleRoomType = RoomType.EndRoom;

            if (!isSpaceNeeded) yield break; // remove accommodation of boss room, to keep sizing accurate
            this.Size -= largestSize;
            this.Size += pendingRoom.possibleRoom.Size;
        }
    }
}