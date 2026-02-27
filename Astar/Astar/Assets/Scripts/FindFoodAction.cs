using System;
using UnityEngine;

[RequireComponent(typeof(Agent))]
public class FindFoodAction : MonoBehaviour
{
    [SerializeField]
    private Agent agent;
    [SerializeField]
    private FoodManager foodManager;
    private Vector2Int? chosenFoodPosition = null;
    private Vector2Int agentPos;
    private Vector2Int lastAgentPos;
    
    private void OnValidate()
    {
        agent = GetComponent<Agent>();
        enabled = agent && foodManager;
    }

    private void Update()
    {
        chosenFoodPosition ??= foodManager.GetRandomFoodPosition(agentPos);
        agentPos = agent.Vector3ToVector2Int(transform.position);
        if (agentPos != lastAgentPos)
        {
            if (foodManager.IsFoodActiveAt(agentPos))
            {
                foodManager.OnDeath(agentPos);
            }
            lastAgentPos = agentPos;
        }
        
        if (chosenFoodPosition.HasValue &&
            !foodManager.IsFoodActiveAt(chosenFoodPosition.Value))
        {
            chosenFoodPosition = null;
        }
        
        if (agent.IsMoving())
            return;
        
        if (agentPos != chosenFoodPosition)
            agent.CallAStar(agent.Vector3ToVector2Int(transform.position), chosenFoodPosition.Value, agent.maze.grid);
    }
}
