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
    /// <summary>
    /// Holds the generation algorithm, tying all gen data together
    /// </summary>
    [Serializable]
    public class WorldGen
    {
        // data
        private MonoBehaviour owner = null!;
        private string mainSeed = null!;
        
        [SerializeField] private List<Area> areaData = new();
        [SerializeField] private int roomRepetitionAllowance = 2;
        [SerializeField] private int walkDirectionRepetitionAllowance = 2;
        
        // anim
        private Action<WorldGenSnapshot> onUpdateSnapshot = null!;
        private float animWaitTime;
        
        // Simple getters and setters
        public int WalkDirectionRepetitionAllowance => this.walkDirectionRepetitionAllowance;
        public int RoomRepetitionAllowance => this.roomRepetitionAllowance;

        public void SetOwner(MonoBehaviour o) => this.owner = o;
        public void SetAnimWaitTime(float w) => this.animWaitTime = w;
        public YieldInstruction GetAnimWaitTime() => (this.animWaitTime <= 0f ? null : new WaitForSeconds(this.animWaitTime))!;
        public Action<WorldGenSnapshot> GetOnUpdateSnapshot(WorldGenRuntime runtime) => runtime == null ? null! : this.onUpdateSnapshot;

        /// <summary>
        /// The generation algorithm. This algorithm is a random walk through all data.
        /// </summary>
        /// <param name="seed">A string of characters that represents the used seed for this generation</param>
        /// <param name="minDistToBossRoom">The minimum distance that the starting room needs to be to the boss room</param>
        /// <param name="onFinish">An action called when the generation has finished</param>
        /// <param name="snapshot">A reference to a snapshot, this snapshot will get updated regularly</param>
        /// <param name="setOnSnapShotUpdate">An action called whenever the snapshot is updated</param>
        /// <returns>IEnumerator, because this function is meant to be called through a coroutine</returns>
        public IEnumerator InitiateGen(string seed, float minDistToBossRoom, Action onFinish, WorldGenSnapshot snapshot, Action<WorldGenSnapshot> setOnSnapShotUpdate)
        {
            this.onUpdateSnapshot = setOnSnapShotUpdate;
            this.mainSeed = seed;
            WorldGenRuntime genRuntime = new(this.owner, this, seed, this.areaData, minDistToBossRoom);

            Debug.Log("Starting generator");
            yield return genRuntime.StartGen(seed, this.areaData);
            
            snapshot.Update(genRuntime.gridRuntime, Vector2Int.zero);
            onFinish.Invoke();
        }
    }
}