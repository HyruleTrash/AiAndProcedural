using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

[RequireComponent(typeof(MazeGeneration))]
public class FoodManager : MonoBehaviour
{
    [SerializeField]
    private MazeGeneration maze;
    [SerializeField]
    private int foodCount;
    [SerializeField]
    private GameObject foodPrefab;
    [NonSerialized]
    private List<Food> instances = new();

    private class Food
    {
        public Vector2Int pos;
        public GameObject instance;
        public Timer respawnTimer;

        public Food(Vector2Int pos, GameObject instance, FoodManager managerRef)
        {
            this.pos = pos;
            this.instance = instance;
            respawnTimer = new Timer(1, () =>
            {
                var newPos = managerRef.GetRandomPosition();
                instance.SetActive(true);
                instance.transform.position = new Vector3(newPos.x, 0.2f, newPos.y);
                pos = newPos;
                respawnTimer!.running = false;
            })
            {
                running = false
            };
        }
    }

    private void OnValidate()
    {
        maze = GetComponent<MazeGeneration>();
        enabled = maze;
    }

    private void Start()
    {
        for (var i = 0; i < foodCount; i++)
        {
            var pos = GetRandomPosition();
            instances.Add(
                new Food(pos, Instantiate(foodPrefab, new Vector3(pos.x, 0.2f, pos.y), Quaternion.identity), this));
        }
    }

    /// <summary>
    /// Gets a random position within maze bounds
    /// </summary>
    private Vector2Int GetRandomPosition()
    {
        var x = Random.Range(0, maze.width);
        var y = Random.Range(0, maze.height);
        return new Vector2Int(x, y);
    }

    public void OnDeath(Vector2Int pos)
    {
        for (var i = 0; i < instances.Count; i++)
        {
            if (instances[i].pos != pos)
                continue;
            if (!instances[i].instance.activeSelf)
                continue;
            instances[i].instance.SetActive(false);
            instances[i].respawnTimer.Reset();
            break;
        }
    }

    private void Update()
    {
        foreach (var food in instances) 
            food.respawnTimer.Update(Time.deltaTime);
    }

    public Vector2Int GetRandomFoodPosition(Vector2Int defaultVal)
    {
        var activeFood = instances.Where(f => f.instance.activeSelf).ToArray();
        if (activeFood.Length == 0)
            return defaultVal;
        return activeFood[Random.Range(0, activeFood.Length)].pos;
    }

    public bool IsFoodActiveAt(Vector2Int position) => instances.Any(f => f.pos == position && f.instance.activeSelf);
}
