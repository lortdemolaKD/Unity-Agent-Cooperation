using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class ReproductionManager : MonoBehaviour
{
    [Header("Agent Reproduction Settings")]
    public List<MLAgentController> agents;  
    public GameObject agentPrefab;          
    public float spawnRadius = 2.0f;       

    
    public void HandleReproduction()
    {
        List<MLAgentController> newAgents = new List<MLAgentController>();

        foreach (var agent in agents)
        {
            
            if (!agent.paired || agent.partnerAgent == null) continue;

            
            if (!agent.hasReturnedToSpawn || !agent.partnerAgent.hasReturnedToSpawn) continue;

            
            if (agent.collectedFood < agent.survivalFoodThreshold + 2 ||
                agent.partnerAgent.collectedFood < agent.partnerAgent.survivalFoodThreshold + 2)
                continue;

            
            if (agent.hasReproduced || agent.partnerAgent.hasReproduced) continue;

            
            Vector3 spawnPosition = FindValidSpawnPosition(agent.transform.position);

            
            GameObject offspring = Instantiate(agentPrefab, spawnPosition, Quaternion.identity);
            MLAgentController offspringAgent = offspring.GetComponent<MLAgentController>();

            if (offspringAgent != null)
            {
                
                offspringAgent.collectedFood = 0;
                offspringAgent.readyToReproduce = false;
                offspringAgent.hasReproduced = false;
                offspringAgent.paired = false; 

                
                newAgents.Add(offspringAgent);
            }

            
            agent.collectedFood = 0;
            agent.partnerAgent.collectedFood = 0;
            agent.readyToReproduce = false;
            agent.partnerAgent.readyToReproduce = false;
            agent.hasReproduced = true;
            agent.partnerAgent.hasReproduced = true;

            Debug.Log("Reproduction Successful!");
        }

        
        agents.AddRange(newAgents);
    }

 
    private Vector3 FindValidSpawnPosition(Vector3 parentPosition)
    {
        for (int i = 0; i < 10; i++)  
        {
            Vector3 randomOffset = new Vector3(
                Random.Range(-spawnRadius, spawnRadius),
                0,
                Random.Range(-spawnRadius, spawnRadius)
            );

            Vector3 spawnPosition = parentPosition + randomOffset;

            
            if (IsValidSpawnPosition(spawnPosition))
            {
                return spawnPosition;
            }
        }
        
        return parentPosition;
    }


    private bool IsValidSpawnPosition(Vector3 position)
    {
        
        return NavMesh.SamplePosition(position, out NavMeshHit hit, 1.0f, NavMesh.AllAreas);
    }
}
