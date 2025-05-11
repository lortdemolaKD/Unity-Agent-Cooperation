using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;

public class SoloMLAgentController : Agent
{
    [Header("Navigation Settings")]
    public NavMeshAgent navAgent;
    public Transform foodSource;
    public Vector3 foodAreaSize = new Vector3(28f, 12f, 48f);
    public List<Transform> treeLocations;
    public bool hasLeftSpawn = false;
    public bool prime = false;

    [Header("Food Collection Settings")]
    public float survivalFoodThreshold = 2f;           
    public float collectedFood = 0f;
    public int collectedFoodLST = 0; 
    public float treeInteractionRadius = 2f;         
    private float previousDistanceToSpawn = 0f;

    [Header("Reproduction & Return Settings")]
    public bool hasReturnedToSpawn = false;  
    public bool hasReturnedToSpawnLST = false;
    public bool readyToReproduce = false;    
    public bool hasReproduced = false;       
    public bool hasReproducedLST = false;

    private static int currentGeneration = 1;
    private static int totalAgentsInGeneration = 0;
    private static float totalScoreInGeneration = 0;
    private static int totalFoodCollected = 0;
    private static int reproductionCount = 0;
    private static int successfulReturns = 0;
    private static int totalExploredTrees = 0;
    private static bool generationLogged = false;
    private static SimulationLogger logger;
    
    public List<Transform> discoveredTrees = new List<Transform>();
    public HashSet<Transform> collectedFromTrees = new HashSet<Transform>();
    
    private float previousDistanceToDestination = 0f;
    private Vector3 lastDestination = Vector3.zero;

    
    
    public Vector3 startingPoint;



    [Header("Enemy Detection Settings")]
    public float enemyDetectionRadius = 8f;  
    public LayerMask enemyLayer;             
    private Transform currentTargetTree;     
    private bool isEvadingEnemy = false;     
    private float evadeCooldown = 2.0f;      
    private float evadeTimer = 0f;           
    public override void Initialize()
    {
        if (prime)
        {
            logger = GameObject.FindAnyObjectByType<SimulationLogger>();
        }
        startingPoint = transform.position;
        discoveredTrees.Clear();
        lastDestination = startingPoint;
        previousDistanceToDestination = 0f;
        foodSource = GameObject.FindGameObjectWithTag("tCent").transform;
        hasReturnedToSpawn = false;
        readyToReproduce = false;
        hasReproduced = false;
    }
    public void sett()
    {
        if (prime)
        {
            logger = GameObject.FindAnyObjectByType<SimulationLogger>(); 
        }
        TreeFood[] temp = transform.parent.GetComponentsInChildren<TreeFood>();
        foreach (var item in temp)
        {
            treeLocations.Add(item.transform);

        }
    }
    public void ENDDATA()
    {
        totalScoreInGeneration += GetCumulativeReward();
        hasReproducedLST = hasReproduced;
        collectedFoodLST = (int)collectedFood;
        hasReturnedToSpawnLST = hasReturnedToSpawn;
        if (!prime && collectedFood < survivalFoodThreshold)
        {
            
            Destroy(gameObject);

        }

        SetReward(2f);
        EndEpisode();

    }
    public override void OnEpisodeBegin()
    {
        if (prime)
        {

            
            SoloMLAgentController[] allAgents = GameObject.FindObjectsOfType<SoloMLAgentController>();
            totalAgentsInGeneration = allAgents.Length;
            
            totalFoodCollected = 0;
            foreach (var agent in allAgents)
            {
                totalFoodCollected += agent.collectedFoodLST;
                if (hasReproducedLST) reproductionCount++;
                if (hasReturnedToSpawnLST) successfulReturns++;
            }

            
            if (!generationLogged && currentGeneration > 1 && totalAgentsInGeneration > 0)
            {
                
                float avgScore = (totalAgentsInGeneration > 0) ? totalScoreInGeneration / totalAgentsInGeneration : 0f;

                float avgFood = (totalAgentsInGeneration > 0) ? (totalFoodCollected / totalAgentsInGeneration) * 1f : 0f;
                float survivalRate = (totalAgentsInGeneration > 0) ? (float)successfulReturns / totalAgentsInGeneration : 0f;
                float explorationRate = (totalAgentsInGeneration > 0) ? (float)totalExploredTrees / totalAgentsInGeneration : 0f;

                logger.LogGeneration(currentGeneration, totalAgentsInGeneration, avgScore, avgFood,
                                     totalFoodCollected, reproductionCount, survivalRate, explorationRate);

                generationLogged = true;  
            }

            
            currentGeneration++;
            totalScoreInGeneration = 0;
            totalFoodCollected = 0;
            reproductionCount = 0;
            successfulReturns = 0;
            totalExploredTrees = 0;
            generationLogged = false;  

            foreach (var tree in treeLocations)
            {
                TreeFood treeFood = tree.GetComponent<TreeFood>();
                if (treeFood != null)
                {
                    treeFood.RegenerateFood();
                }
            }
        }

        totalAgentsInGeneration++; 
        
        hasReturnedToSpawn = false;
        readyToReproduce = false;
        hasLeftSpawn = false;
        hasReproduced = false;  
        navAgent.Warp(startingPoint);
        transform.position = startingPoint;
        
        collectedFood = 0f;
        
        collectedFromTrees.Clear();
        lastDestination = startingPoint;
        previousDistanceToDestination = 0f;

    }

    public override void CollectObservations(VectorSensor sensor)
    {
        Vector3 toFoodCenter = foodSource.position - transform.position;
        sensor.AddObservation(toFoodCenter.normalized);
        HashSet<Transform> sharedDiscovered = new HashSet<Transform>(discoveredTrees);
        foreach (var tree in discoveredTrees)
            sharedDiscovered.Add(tree);
        float discoveryRatio = treeLocations.Count > 0 ? sharedDiscovered.Count / (float)treeLocations.Count : 0f;
        sensor.AddObservation(discoveryRatio);
        sensor.AddObservation(toFoodCenter.magnitude);
        sensor.AddObservation(collectedFood / survivalFoodThreshold);
        sensor.AddObservation((MaxStep - StepCount) / (float)MaxStep);
        Transform nearestUndiscovered = FindNearestTree(true);
        if (nearestUndiscovered != null)
        {
            Vector3 toTree = nearestUndiscovered.position - transform.position;
            sensor.AddObservation(toTree.normalized);
            sensor.AddObservation(toTree.magnitude);
        }
        else
        {
            sensor.AddObservation(Vector3.zero);
            sensor.AddObservation(0f);
        }
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        int decision = actions.DiscreteActions.Array[0];
        if (!hasLeftSpawn && Vector3.Distance(transform.position, startingPoint) > 2.0f)
        {
            hasLeftSpawn = true;
        }
        if (DetectEnemyInRange(out Transform enemyTransform))
        {
            if (!isEvadingEnemy)
            {
                Vector3 directionAway = (transform.position - enemyTransform.position).normalized;
                Vector3 fleeTarget = transform.position + directionAway * 10f;

                navAgent.SetDestination(fleeTarget);
                isEvadingEnemy = true;
                evadeTimer = evadeCooldown;
            }
        }

        if (isEvadingEnemy)
        {
            evadeTimer -= Time.deltaTime;
            if (evadeTimer <= 0f)
            {
                isEvadingEnemy = false;
                
                currentTargetTree = FindNearestTree(true, currentTargetTree);
                if (currentTargetTree != null)
                    navAgent.SetDestination(currentTargetTree.position);
            }
        }
        if (collectedFood >= survivalFoodThreshold)
        {
            decision = 3;
        }
        if (hasLeftSpawn && Vector3.Distance(transform.position, startingPoint) < 2.0f)
        {
            hasReturnedToSpawn = true;
            navAgent.ResetPath();
            successfulReturns++;
            SetReward(1f);
            if (collectedFood >= survivalFoodThreshold + 1)
            {
                
                readyToReproduce = true;
               
                
            }

            
            if ( !hasReproduced && readyToReproduce)
            {
                
                SetReward(5f);
                SpawnOffspring();
                reproductionCount++;
                ENDDATA();

            }
            else if (collectedFood >= survivalFoodThreshold)
            {
                
                SetReward(1.0f);
                ENDDATA();
            }
            
            if (AllAgentsReturnedToSpawn())
            {
                
                ENDDATA();
                return;
            }
            return;
            
        }
        switch (decision)
        {
            case 0:
                navAgent.SetDestination(GetRandomPointInFoodArea());
                break;
            case 1:
                {
                    Transform undiscovered = FindNearestTree(onlyUndiscovered: true);
                    if (undiscovered != null)
                        navAgent.SetDestination(undiscovered.position);
                    else
                    {
                        Transform treeWithFood = FindNearestTree(onlyUndiscovered: false);
                        if (treeWithFood != null)
                            navAgent.SetDestination(treeWithFood.position);
                        else
                            navAgent.SetDestination(GetRandomPointInFoodArea());
                    }
                }
                break;
            case 2:
                {
                    Transform treeWithFood = FindNearestTree(onlyUndiscovered: false);
                    if (treeWithFood != null)
                        navAgent.SetDestination(treeWithFood.position);
                    else
                        navAgent.SetDestination(GetRandomPointInFoodArea());
                }
                break;
            case 3:
                navAgent.SetDestination(startingPoint);
                break;
        }
        if (navAgent.destination != lastDestination)
        {
            lastDestination = navAgent.destination;
            previousDistanceToDestination = Vector3.Distance(transform.position, navAgent.destination);
        }
        float currentDistance = Vector3.Distance(transform.position, navAgent.destination);
        float progress = previousDistanceToDestination - currentDistance;
        if (progress > 0)
            AddReward(0.02f * progress);
        previousDistanceToDestination = currentDistance;

        
        AttemptTreeFoodCollection();
        HashSet<Transform> sharedDiscovered = new HashSet<Transform>(discoveredTrees);
        foreach (var tree in discoveredTrees)
            sharedDiscovered.Add(tree);
        if (treeLocations.Count > 0 && sharedDiscovered.Count >= 0.8f * treeLocations.Count)
            AddReward(0.5f);
        if (collectedFood >= survivalFoodThreshold)
        {
            float currentDistanceToSpawn = Vector3.Distance(transform.position, startingPoint);
            float spawnProgress = previousDistanceToSpawn - currentDistanceToSpawn;
            if (spawnProgress > 0)
                AddReward(0.05f * spawnProgress);  
            previousDistanceToSpawn = currentDistanceToSpawn;
        }
        if (StepCount >= 4990 && Vector3.Distance(transform.position, startingPoint) > 1.0f && !prime)
        {
            
            SetReward(-10.0f);  
            ENDDATA();

        }
        AddReward(-0.001f);
    }
    private Vector3 FindValidSpawnPosition(Vector3 parentSpawnPosition)
    {
        int maxAttempts = 10;  
        float spawnRadius = 3.0f;  

        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            
            Vector3 randomOffset = new Vector3(
                Random.Range(-spawnRadius, spawnRadius),
                0,  
                Random.Range(-spawnRadius, spawnRadius)
            );

            Vector3 candidatePosition = parentSpawnPosition + randomOffset;

            
            NavMeshHit hit;
            if (NavMesh.SamplePosition(candidatePosition, out hit, 1.0f, NavMesh.AllAreas))
            {
                
                bool isPositionValid = true;
                SoloMLAgentController[] allAgents = FindObjectsOfType<SoloMLAgentController>();
                foreach (var agent in allAgents)
                {
                    if (Vector3.Distance(hit.position, agent.transform.position) < 1.5f)
                    {
                        isPositionValid = false;  
                        break;
                    }
                }

                if (isPositionValid)
                {
                    return hit.position;  
                }
            }
        }

        
        
        return parentSpawnPosition;
    }
    private Vector3 GetRandomPointInFoodArea()
    {
        float halfX = foodAreaSize.x / 2f;
        float halfZ = foodAreaSize.z / 2f;
        return foodSource.position + new Vector3(Random.Range(-halfX, halfX), 0, Random.Range(-halfZ, halfZ));
    }
    private bool DetectEnemyInRange(out Transform nearestEnemy)
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, enemyDetectionRadius, enemyLayer);
        nearestEnemy = null;

        float closestDist = float.MaxValue;
        foreach (var hit in hits)
        {
            float dist = Vector3.Distance(transform.position, hit.transform.position);
            if (dist < closestDist)
            {
                closestDist = dist;
                nearestEnemy = hit.transform;
            }
        }

        return nearestEnemy != null;
    }
    private void SpawnOffspring()
    {
        hasReproduced = true;
  

        
        Vector3 spawnPos1 = FindValidSpawnPosition(startingPoint);


        
        GameObject offspring1 = Instantiate(gameObject, spawnPos1, Quaternion.identity);


        
        SoloMLAgentController offspringAgent1 = offspring1.GetComponent<SoloMLAgentController>();
       

        if (offspringAgent1 != null )
        {
            


            offspringAgent1.prime = false;
            
            offspringAgent1.discoveredTrees = new List<Transform>(discoveredTrees);
            offspringAgent1.treeLocations = new List<Transform>(treeLocations);
            offspringAgent1.transform.parent = transform.parent;

            
        }
    }
    private bool AllAgentsReturnedToSpawn()
    {
        MLAgentController[] allAgents = FindObjectsOfType<MLAgentController>();
        foreach (var agent in allAgents)
        {
            if (Vector3.Distance(agent.transform.position, agent.startingPoint) > 1.0f)
                return false; 
        }
        return true; 
    }
    private Transform FindNearestTree(bool onlyUndiscovered = false, Transform exclude = null)
    {
        Transform nearest = null;
        float minDistance = float.MaxValue;
        foreach (var tree in treeLocations)
        {
            if (exclude != null && tree == exclude)
                continue;
            if (onlyUndiscovered && discoveredTrees.Contains(tree))
                continue;
            TreeFood treeFood = tree.GetComponent<TreeFood>();
            if (treeFood != null && treeFood.GetFoodAmount() > 0)
            {
                float distance = Vector3.Distance(transform.position, tree.position);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    nearest = tree;
                }
            }
        }
        return nearest;
    }
    private void AttemptTreeFoodCollection()
    {
        foreach (var tree in treeLocations)
        {
            float distance = Vector3.Distance(transform.position, tree.position);
            if (distance < treeInteractionRadius)
            {
                
                if (!discoveredTrees.Contains(tree))
                {
                    discoveredTrees.Add(tree);
                    AddReward(0.2f); 
                    if (prime)
                    {
                        totalExploredTrees++;
                    }
                }
                
                if (!collectedFromTrees.Contains(tree))
                {
                    TreeFood treeFood = tree.GetComponent<TreeFood>();
                    if (treeFood != null && treeFood.GetFoodAmount() > 0)
                    {
                        float foodCollected = 1f; 
                        collectedFood += foodCollected;
                        treeFood.ConsumeFood(foodCollected);
                        collectedFromTrees.Add(tree);
                        AddReward(0.5f);
                        totalFoodCollected += 2;
                    }
                }
            }
        }
    }
    private void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Water")
        {
            navAgent.speed = 4f;
        }
    }
    private void OnTriggerExit(Collider other)
    {
        if (other.tag == "Water")
        {
            navAgent.speed = 5f;
        }
    }
}
