using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Generation
{
    /// <summary>
    /// Holds the generation algorithm, tying all gen data together
    /// </summary>
    [CreateAssetMenu(fileName = "WorldGen", menuName = "Generation/WorldGenData")]
    public class WorldGen : ScriptableObject
    {
        // data
        private MonoBehaviour owner = null!;
        [SerializeField] private List<Area> areaData = new();
        [SerializeField] private int roomRepetitionAllowance = 2;
        [SerializeField] private float minDistToBossRoom;
        [SerializeField] private int maxTries = 64;
        [SerializeField] private int maxOverlapAttempts = 8;
        [SerializeField] private int maxOverlapAttemptsBruteForce = 16;
        
        // anim
        private Action<WorldGenSnapshot> onUpdateSnapshot = null!;
        private float animWaitTime;
        
        // Simple getters and setters
        public int RoomRepetitionAllowance => this.roomRepetitionAllowance;
        public int MaxTries => this.maxTries;
        public int MaxOverlapAttempts => this.maxOverlapAttempts;
        public int MaxOverlapAttemptsBruteForce => this.maxOverlapAttemptsBruteForce;

        public void SetOwner(MonoBehaviour o) => this.owner = o;
        public void SetAnimWaitTime(float w) => this.animWaitTime = w;
        public float GetAnimWaitTime() => this.animWaitTime;
        public YieldInstruction GetAnimYieldInstruction() => (this.animWaitTime <= 0f ? null : new WaitForSeconds(this.animWaitTime))!;
        public Action<WorldGenSnapshot> GetOnUpdateSnapshot(WorldGenRuntime runtime) => runtime == null ? null! : this.onUpdateSnapshot;

        /// <summary>
        /// The generation algorithm. This algorithm is a random walk through all data.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="seed">A string of characters that represents the used seed for this generation</param>
        /// <param name="minDistToBossRoom">The minimum distance that the starting room needs to be to the boss room</param>
        /// <param name="onFinish">An action called when the generation has finished</param>
        /// <param name="setOnSnapShotUpdate">An action called whenever the snapshot is updated</param>
        /// <returns>IEnumerator, because this function is meant to be called through a coroutine</returns>
        public IEnumerator InitiateGen(string seed, Action onFinish, Action<WorldGenSnapshot> setOnSnapShotUpdate)
        {
            this.onUpdateSnapshot = setOnSnapShotUpdate;
            WorldGenRuntime genRuntime = new(this.owner, this, seed, this.areaData, this.minDistToBossRoom);

            NotificationManager.Log("Starting generator", GetAnimWaitTime());
            yield return genRuntime.StartGen(seed, this.areaData);
            
            onFinish.Invoke();
        }

        public int GetWalkDirectionRepetitionAllowance(AreaType areaType)
        {
            Area? a = this.areaData.FirstOrDefault(a => a.areaType == areaType);
            return a ? a.WalkDirectionRepetitionAllowance : 0;
        }
    }
}