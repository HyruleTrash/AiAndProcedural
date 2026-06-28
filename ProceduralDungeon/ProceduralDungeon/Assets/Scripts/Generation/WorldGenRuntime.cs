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

        private void RegisterHadRoom(Room foundRoom, int roomRepetitionAllowance)
        {
            this.hadRooms.Add(foundRoom);
            if (this.hadRooms.Count >= roomRepetitionAllowance) this.hadRooms.RemoveRange(this.hadRooms.Count - roomRepetitionAllowance, this.hadRooms.Count);
        }

        public void MutateSeed() => this.currentSeed = Rng.MutateNext(this.currentSeed);

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
                
                // AreaRuntime? lastArea = this.hadAreas.Last(); TODO
                // RoomRuntimeRef bossRoomRef = new();
                //
                // while (true)
                // {
                //     yield return this.unityConnection.StartCoroutine(AddRoom(lastArea, bossRoomRef));
                //     if (bossRoomRef.instance != null) break;
                // }
                
                NotificationManager.Log($"ANALYSE PHASE STARTING");
                break;
                // if (Vector2.Distance(this.currentPosition, Vector2.zero) >= this.minDistToBossRoom) break;

                NotificationManager.Log($"GENERATION FAILED, RESTARTING");
                this.currentSeed = Rng.MutateNext(seed);
                Reset(areaData, this.currentSeed);
                // bossRoomRef.instance = null;
            }

            this.gridRuntime.RemoveUnusedDoorways();
        }

        private IEnumerator WalkAllAreas()
        {
            while (this.backlog.Count > 0)
            {
                NotificationManager.Log($"Walking through area backlog, current count: {this.backlog.Count}");
                AreaRuntime pickedArea = GetRandomArea();
                
                yield return this.unityConnection.StartCoroutine(pickedArea.WalkthroughArea(this, this.unityConnection, this.owner.GetAnimWaitTime()));
                this.hadAreas.Add(pickedArea);
                
                yield return this.owner.GetAnimWaitTime();
            }

            NotificationManager.Log("Done with walking through areas");
        }
        
        private AreaRuntime GetRandomArea()
        {
            int foundIndex = Rng.RandomRange(0, this.backlog.Count, this.currentSeed);
            MutateSeed();
                
            AreaRuntime pickedArea = this.backlog[foundIndex].GetAreaGenData(this.currentSeed);
            this.backlog.RemoveAt(foundIndex);
            
            return pickedArea;
        }

        public void Walk(RoomRuntime currentRoom)
        {
            // go to door
            UpdateWalkDirection();
            Room.DoorPointGroup doorway = WalkToDoor(currentRoom.roomRef, this.currentWalkDirection);
            
            // register that door was used, and space self outside of it
            Vector2Int newPos = currentRoom.position + doorway.roomPoint + this.currentWalkDirection; // position just about outside of room
            this.currentPosition = newPos;
            NotificationManager.Log($"Walking from {this.currentPosition} to {newPos}");
            
            currentRoom.RemoveDoorFromLayout(doorway);

            UpdateSnapShot();
            MutateSeed();
        }

        private void UpdateSnapShot()
        {
            Action<WorldGenSnapshot> onUpdateSnapshot = this.owner.GetOnUpdateSnapshot(this);
            onUpdateSnapshot?.Invoke(new WorldGenSnapshot(this.gridRuntime, this.currentPosition));
        }

        private void UpdateWalkDirection()
        {
            while (true)
            {
                int index = Rng.RandomRange(0, Extensions.CardinalDirections.Length, this.currentSeed);
                MutateSeed();

                if (Extensions.CardinalDirections[index] == this.currentWalkDirection)
                    this.walkDirRepeated++;
                else
                    this.walkDirRepeated = 0;

                if (this.walkDirRepeated > this.owner.WalkDirectionRepetitionAllowance) continue;

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

                NotificationManager.Log($"Adding room of size: {pendingRoom.possibleRoom.Size}");
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
        public IEnumerator TryToGetPlaceAbleRoom(TypedRoomList pickedTypeList, AreaRuntime pickedArea, PendingRoomPlacement placement)
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
                placement.possibleRoom = RoomList.TryGetRoom(this, sizedPool, this.hadRooms.ToArray());
                
                // TryGetRoom failed
                if (placement.possibleRoom == null) 
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

                    MutateSeed();
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
                    placement.center = this.currentPosition;
                    placement.center += offset;
                }
                else if (!TryWalkThroughDoor(placement, -this.currentWalkDirection)) break;

                // movement failed, so show on visual
                Action<WorldGenSnapshot> onUpdateSnapshot = this.owner.GetOnUpdateSnapshot(this);
                onUpdateSnapshot?.Invoke(new WorldGenSnapshot(this.gridRuntime, placement.center!.Value));
                
                if (this.gridRuntime.CheckRoomPossible(placement.possibleRoom, placement.center!.Value, out RoomRuntime? hit)) break;
                NotificationManager.Log($"Cant place room of size {placement.possibleRoom.Size}, at {placement.center.Value}");

                #region Attempt tracking

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
                
                yield return this.owner.GetAnimWaitTime();

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
            placement.doorGroup = WalkToDoor(placement.possibleRoom, walkDirection);

            if (placement.doorGroup == null) return false;
            placement.center = this.currentPosition - placement.doorGroup.roomPoint;
            MutateSeed();
            
            return true;
        }

        private Room.DoorPointGroup WalkToDoor(Room room, Vector2Int walkDirection)
        {
            List<Room.DoorPointGroup> doorways = room.DoorPoints
                .First(dir => dir.key == walkDirection).value;
            return doorways[Rng.RandomRange(0, doorways.Count, this.currentSeed)];
        }
    }
}