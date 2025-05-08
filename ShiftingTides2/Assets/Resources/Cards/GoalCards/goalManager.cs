using UnityEngine;
using System.Collections;
using System;
using System.Collections.Generic;
using Random = UnityEngine.Random;
using Unity.Netcode;

public class GoalManager : MonoBehaviour
{
    public static GoalManager Instance;

    [System.Serializable]
    public class GoalArrayWrapper
    {
        public Goal[] goals;
    }

    public Goal[] goals;
    private bool goalsLoaded = false;

    private HashSet<int> assignedGoalIndices = new HashSet<int>();

    public GameObject goalCard;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            if (!goalsLoaded) StartCoroutine(LoadGoals()); // Initialize goal loading
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
            GoalArrayWrapper wrapper = JsonUtility.FromJson<GoalArrayWrapper>(jsonFile.text);
            //goals = JsonHelper.FromJson<Goal>(jsonFile.text);
            goals = wrapper?.goals;
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
        if (!goalsLoaded || goals == null) return -1;

        // Create a list of available indices
        List<int> availableIndices = new List<int>();
        for (int i = 0; i < goals.Length; i++)
        {
            if (!assignedGoalIndices.Contains(i))
                availableIndices.Add(i);
        }

        if (availableIndices.Count == 0)
        {
            Debug.LogWarning("[GoalManager] No more unique goals available!");
            return -1;
        }

        // Assign a random goal
        int randomIndex = availableIndices[Random.Range(0, availableIndices.Count)];
        assignedGoalIndices.Add(randomIndex);
        return randomIndex;
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
        Debug.Log($"[GoalManager] Assigned goal {goalIndex} to player {clientId}");
    }

    public Goal GetPlayerGoal(ulong clientId)
    {
        return assignedGoals.TryGetValue(clientId, out Goal goal) ? goal : null;
    }

    public void CloseGoalCard()
    {
        // Close the goal card UI
    }

    [ServerRpc(RequireOwnership = false)]
    public void CloseGoalCardServerRpc()
    {
        CloseGoalCard();
        // Optionally, notify all clients to close their goal cards
        CloseGoalCardClientRpc();
    }

    [ClientRpc]
    public void CloseGoalCardClientRpc()
    {
        CloseGoalCard();
    }
}