using UnityEngine;
using System.Collections;
using System;
using System.Collections.Generic;

public class GoalManager : MonoBehaviour
{
    public static GoalManager Instance;

    public Goal[] goals;
    private bool goalsLoaded = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            StartCoroutine(LoadGoals()); // Initialize goal loading
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private IEnumerator LoadGoals()
    {
        // Load JSON file from Resources
        TextAsset jsonFile = Resources.Load<TextAsset>("Cards/GoalCards/goal-cards");
        if (jsonFile == null)
        {
            Debug.LogError("[GoalManager] JSON file not found");
            yield break;
        }

        try
        {
            // Deserialize JSON data
            goals = JsonHelper.FromJson<Goal>(jsonFile.text);
            Debug.Log($"[GoalManager] Loaded {goals?.Length} goals");
            Debug.Log($"[GoalManager] Deserialized JSON: {JsonUtility.ToJson(goals, true)}");

            if (goals == null || goals.Length == 0)
            {
                Debug.LogError("[GoalManager] No goals loaded. Check:");
                Debug.LogError($"[GoalManager] 1. JSON validity: {jsonFile.text}");
                Debug.LogError("[GoalManager] 2. Goal class structure matches JSON");
            }
            else
            {
                // Log all loaded goals for debugging
                foreach (var goal in goals)
                {
                    Debug.Log($"[GoalManager] Loaded: {goal.title} (ID: {goal.id})");
                    Debug.Log($"[GoalManager] Resources: Money={goal.resources?.money}, Influence={goal.resources?.influence}, People={goal.resources?.people}");
                }
                goalsLoaded = true;
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[GoalManager] Error loading goals: {e.Message}");
        }

        yield return null;
    }

    public Goal GetGoal(int index)
    {
        // Safe array access with bounds checking
        return (index >= 0 && index < goals.Length) ? goals[index] : null;
    }

    public int GetRandomGoalIndex()
    {
        if (!goalsLoaded) return -1;

        // Find unused goal
        int goalAssigned = UnityEngine.Random.Range(0, goals.Length);
        while (goals[goalAssigned].used)
        {
            goalAssigned = UnityEngine.Random.Range(0, goals.Length);
        }
        
        goals[goalAssigned].used = true;
        return goalAssigned;
    }

    public bool AreGoalsLoaded()
    {
        return goalsLoaded;
    }

    // Player-specific goal tracking
    private Dictionary<ulong, Goal> assignedGoals = new Dictionary<ulong, Goal>();

    public void AssignGoalToPlayer(ulong clientId, int goalIndex)
    {
        assignedGoals[clientId] = goals[goalIndex];
    }

    public Goal GetPlayerGoal(ulong clientId)
    {
        return assignedGoals.TryGetValue(clientId, out Goal goal) ? goal : null;
    }
}