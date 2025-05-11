using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class SimulationLogger : MonoBehaviour
{
    private string logFilePath;
    private List<GenerationData> logData = new List<GenerationData>();
    public bool solo = false;
    private void Awake()
    {
        Debug.Log(Application.persistentDataPath);
        if (solo)
        {
            logFilePath = Path.Combine(Application.persistentDataPath, "SimulationLog_" + name + "_SOLO.json");
        }
        else
        {
            logFilePath = Path.Combine(Application.persistentDataPath, "SimulationLog_" + name + ".json");
        }
    }

    public void LogGeneration(int generation, int totalAgents, float avgScore, float avgFoodCollected,
                          int totalFood, int reproductionCount, float survivalRate, float explorationRate)
    {
        GenerationData data = new GenerationData()
        {
            generation = generation,
            totalAgents = totalAgents,
            avgScore = float.IsInfinity(avgScore) || float.IsNaN(avgScore) ? 0f : avgScore,
            avgFoodCollected = float.IsInfinity(avgFoodCollected) || float.IsNaN(avgFoodCollected) ? 0f : avgFoodCollected,
            totalFoodCollected = totalFood,
            reproductionCount = reproductionCount,
            survivalRate = float.IsInfinity(survivalRate) || float.IsNaN(survivalRate) ? 0f : survivalRate,
            explorationRate = float.IsInfinity(explorationRate) || float.IsNaN(explorationRate) ? 0f : explorationRate
        };

        logData.Add(data);
        WriteToFile();
    }

    private void WriteToFile()
    {
        string json = JsonUtility.ToJson(new LogWrapper { generations = logData }, true);
        File.WriteAllText(logFilePath, json);
    }
}

[System.Serializable]
public class GenerationData
{
    public int generation;
    public int totalAgents;
    public float avgScore;
    public float avgFoodCollected;
    public int totalFoodCollected;
    public int reproductionCount;
    public float survivalRate;
    public float explorationRate;
}

[System.Serializable]
public class LogWrapper
{
    public List<GenerationData> generations;
}
