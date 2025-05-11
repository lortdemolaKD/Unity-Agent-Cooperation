using System.Collections.Generic;
using UnityEngine;

public class DefaultGenerationSpawner : MonoBehaviour
{
    [Header("Default Generation Settings")]
    [Tooltip("Prefab of the ML agent that contains the MLAgentController component.")]
    public GameObject agentPrefab;
    [Tooltip("Prefab of the ML agent that contains the MLAgentController component.")]
    public GameObject agentPrefabSolo = null;

    [Tooltip("Number of pairs to spawn for the default generation.")]
    public int numberOfPairs = 5;

    [Tooltip("Center point of the spawn area.")]
    public Vector3 spawnAreaCenter = Vector3.zero;

    [Tooltip("Size of the spawn area (x and z dimensions will be used).")]
    public Vector3 spawnAreaSize = new Vector3(10, 0, 10);

    [Tooltip("List to keep track of all spawned agents.")]
    public List<MLAgentController> spawnedAgents = new List<MLAgentController>();
    [Tooltip("List to keep track of all spawned agent Solo.")]
    public List<SoloMLAgentController> spawnedAgentsSolo = new List<SoloMLAgentController>();

    [Tooltip("Spawn solo agents")]
    public bool solo = false;

    public void SpawnDefaultGeneration()
    {
        if (!solo)
        {
                        spawnedAgents.Clear();

            for (int i = 0; i < numberOfPairs; i++)
            {
                                Vector3 spawnPos1 = spawnAreaCenter + new Vector3(
                    Random.Range(-spawnAreaSize.x / 2f, spawnAreaSize.x / 2f),
                    0,
                    Random.Range(-spawnAreaSize.z / 2f, spawnAreaSize.z / 2f)
                );
                Vector3 spawnPos2 = spawnAreaCenter + new Vector3(
                    Random.Range(-spawnAreaSize.x / 2f, spawnAreaSize.x / 2f),
                    0,
                    Random.Range(-spawnAreaSize.z / 2f, spawnAreaSize.z / 2f)
                );

                                GameObject agentObj1 = Instantiate(agentPrefab, spawnPos1, Quaternion.identity);
                GameObject agentObj2 = Instantiate(agentPrefab, spawnPos2, Quaternion.identity);
                agentObj1.transform.parent = transform;
                agentObj2.transform.parent = transform;
                                MLAgentController agent1 = agentObj1.GetComponent<MLAgentController>();
                MLAgentController agent2 = agentObj2.GetComponent<MLAgentController>();
                agent1.prime = true;
                agent2.prime = true;
                agent1.sett();
                agent2.sett();

                                if (agent1 == null || agent2 == null)
                {
                    Debug.LogError("One or both spawned agents do not have the MLAgentController component attached.");
                    continue;
                }

                                agent1.paired = true;
                agent1.partnerAgent = agent2;
                agent2.paired = true;
                agent2.partnerAgent = agent1;
                agent1.prime = true;
                agent2.prime = true;
                                spawnedAgents.Add(agent1);
                spawnedAgents.Add(agent2);
                agentObj1.transform.parent = transform;
                agentObj2.transform.parent = transform;
            }
        }
        else
        {
            spawnedAgentsSolo.Clear();

            for (int i = 0; i < numberOfPairs; i++)
            {
                                Vector3 spawnPos1 = spawnAreaCenter + new Vector3(
                    Random.Range(-spawnAreaSize.x / 2f, spawnAreaSize.x / 2f),
                    0,
                    Random.Range(-spawnAreaSize.z / 2f, spawnAreaSize.z / 2f)
                );
                Vector3 spawnPos2 = spawnAreaCenter + new Vector3(
                    Random.Range(-spawnAreaSize.x / 2f, spawnAreaSize.x / 2f),
                    0,
                    Random.Range(-spawnAreaSize.z / 2f, spawnAreaSize.z / 2f)
                );

                                GameObject agentObj1 = Instantiate(agentPrefabSolo, spawnPos1, Quaternion.identity);
               
                agentObj1.transform.parent = transform;
              
                                SoloMLAgentController agent1 = agentObj1.GetComponent<SoloMLAgentController>();
                
                agent1.prime = true;
                agent1.sett();


                                if (agent1 == null )
                {
                    Debug.LogError("One or  spawned agents do not have the SoloMLAgentController component attached.");
                    continue;
                }

                

                agent1.prime = true;
                                spawnedAgentsSolo.Add(agent1);
                agentObj1.transform.parent = transform;
            }
        }
    }

    private void Start()
    {
        SpawnDefaultGeneration();
    }
}
