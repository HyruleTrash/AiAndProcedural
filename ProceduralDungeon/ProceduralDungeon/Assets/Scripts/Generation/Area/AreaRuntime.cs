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
        public AreaType AreaType { get; }
        public int Size { get; set; }

        private int RoomCount => this.roomTypes.Count;
        public int EndRoomCount => this.endRooms.RoomData.Count;
        public List<Room> EndRooms => this.endRooms.RoomData;

        public AreaRuntime(AreaType areaType, int size, List<TypedRoomList> roomTypes, RoomList endRooms)
        {
            this.AreaType = areaType;
            this.Size = size;
            this.roomTypes = new List<TypedRoomList>();
            this.endRooms = new RoomList(endRooms);
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
                throw new Exception("indexPool is empty");
            
            if (genRuntime.lastHadRoomType != null)
            {
                bool hasValid = indexPool.Any(i => this.roomTypes[i].roomType != genRuntime.lastHadRoomType.Value);
                if (!hasValid)
                {
                    // genRuntime.lastHadRoomType = null;
                    Debug.LogWarning("only non valid room types are left");
                    return null;
                }
            }
            while (true)
            {
                int foundIndex = indexPool[Rng.RandomRange(0, indexPool.Count, genRuntime.currentSeed)];
                genRuntime.currentSeed = Rng.MutateNext(genRuntime.currentSeed);
                foundTypeList = this.roomTypes[foundIndex];
                
                if (foundTypeList && (genRuntime.lastHadRoomType == null || foundTypeList.roomType != genRuntime.lastHadRoomType.Value)) break;
            }
            return foundTypeList;
        }

        public IEnumerator WalkthroughArea(WorldGenRuntime genRuntime, MonoBehaviour unityConnection, YieldInstruction waitTime)
        {
            while (this.Size > 0)
            {
                NotificationManager.Log($"Walking through area: {this.AreaType}, current size left: {this.Size}\nCurrent position is: {genRuntime.CurrentPosition}");
                RoomRuntime? foundRoom = genRuntime.gridRuntime.GetRoomAtPosition(genRuntime.CurrentPosition);

                if (foundRoom == null)
                {
                    NotificationManager.Log("Current walk position empty, trying to add room");
                    
                    PendingRoomPlacement pendingRoom = new();
                    yield return unityConnection.StartCoroutine(GetTypedPlaceAbleRoom(genRuntime, pendingRoom, unityConnection));
                    
                    RoomRuntimeRef runtimeRef = new();
                    yield return unityConnection.StartCoroutine(genRuntime.AddRoom(this, pendingRoom, runtimeRef));

                    // When the addition of a room fails, check if area is still possible
                    if (runtimeRef.instance == null)
                    {
                        if (this.Size <= 0) break;
                        continue;
                    }
                    
                    NotificationManager.Log("Room added");
                    foundRoom = runtimeRef.instance;
                    genRuntime.currentSeed = Rng.MutateNext(genRuntime.currentSeed);
                }

                if (this.Size <= 0) break;
                genRuntime.Walk(foundRoom);
                
                yield return waitTime;
            }
        }
        
        private IEnumerator GetTypedPlaceAbleRoom(WorldGenRuntime genRuntime, PendingRoomPlacement pendingRoomPlacement, MonoBehaviour unityConnection)
        {
            TypedRoomList? pickedTypeList = null;
            int tries = 0;
            int maxTries = this.RoomCount * 2;
            while (true)
            {
                genRuntime.MutateSeed();
                pickedTypeList?.UndoPicked(this);
                pickedTypeList = PickTypeList(genRuntime);
                if (!pickedTypeList) continue;
                pickedTypeList.OnPicked(this);
                
                yield return unityConnection.StartCoroutine(genRuntime.TryToGetPlaceAbleRoom(pickedTypeList, this, pendingRoomPlacement));

                if (pendingRoomPlacement.possibleRoom != null)
                {
                    pendingRoomPlacement.possibleRoomType = pickedTypeList.roomType;
                    break;
                }
                if (tries >= maxTries) yield break;

                tries++;
            }
        }
    }
}