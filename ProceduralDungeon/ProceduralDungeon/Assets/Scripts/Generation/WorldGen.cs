using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NaughtyAttributes;
using TMPro;
using UnityEngine;
using Util;

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
        [SerializeField, Expandable] private List<Area> areaData = new();
        [SerializeField, Range(0, 24)] private int roomRepetitionAllowance = 2;
        [SerializeField, Range(5, 200)] private float minDistToBossRoom;
        [SerializeField, Range(0, 64)] private int maxTries = 64;
        [SerializeField, Range(0, 64)] private int maxOverlapAttempts = 8;
        [SerializeField, Range(0, 64)] private int maxOverlapAttemptsBruteForce = 16;
        
        // anim
        private Action<WorldGenSnapshot> onUpdateSnapshot = null!;
        private float animWaitTime;
        
        // runtime
        private WorldGenRuntime? genRuntime;

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
        
        // Create a duplicate so that ui sliders may safely edit
        public void InstantiateAreaData() => this.areaData = this.areaData.Select(Instantiate).ToList();

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
            this.genRuntime = new WorldGenRuntime(this.owner, this, seed, this.areaData, this.minDistToBossRoom);

            NotificationManager.Log("Starting generator", GetAnimWaitTime());
            yield return this.genRuntime.StartGen(seed, this.areaData);
            
            onFinish.Invoke();
        }

        public int GetWalkDirectionRepetitionAllowance(AreaType areaType)
        {
            Area? a = this.areaData.FirstOrDefault(a => a.areaType == areaType);
            return a ? a.WalkDirectionRepetitionAllowance : 0;
        }

        public void StopGen()
        {
            if (this.genRuntime == null) return;
            this.genRuntime.StopGen();
            this.genRuntime = null;
        }

        public void CreateAreaUI(Transform mainParent, GameObject areaUIPrefab, GameObject sliderAndTextPrefab)
        {
            foreach (Area? area in this.areaData)
            {
                GameObject? instance = Instantiate(areaUIPrefab, mainParent.transform);
                instance.GetComponentInChildren<TextMeshProUGUI>()?.SetText(area.areaType.ToString().ToReadableString());
                TagComponent[]? possibleParents = instance.GetComponentsInChildren<TagComponent>();
                Transform? parent = possibleParents?.FirstOrDefault(tag => tag.name == "Content")?.transform;
                Debug.Log(parent ? parent.name : "Empty area");
                area.CreatUI(parent, sliderAndTextPrefab);
            }
        }
    }
}