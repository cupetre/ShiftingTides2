using UnityEngine;
using TMPro;
using System.Collections;
using Unity.Netcode;
using UnityEngine.SceneManagement;

public class GoalDisplay : NetworkBehaviour
{
    public TMP_Text goalTitle;
    public TMP_Text goalDescription;

    private GameObject playerObject;
    private NetworkPlayer networkPlayer;
    public GameObject goalCard;

    private ulong clientId;
    private int playerIndex;

    private void Start()
    {
        // Get the client ID and player index
        clientId = NetworkManager.Singleton.LocalClientId;

        // Find the player through the NetworkManager
        playerObject = NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject.gameObject;
        if (playerObject == null)
        {
            Debug.LogError("[GoalDisplayManager] Player object not found.");
            return;
        }
        // Get the player index from the NetworkPlayer component
        networkPlayer = playerObject.GetComponent<NetworkPlayer>();
        if (networkPlayer == null)
        {
            Debug.LogError("[GoalDisplayManager] NetworkPlayer component not found on player object.");
            return;
        }
        playerIndex = networkPlayer.playerIndex.Value;

        // Initialize the goal display
        InitializeGoalDisplay();
    }

    private void InitializeGoalDisplay()
    {
        // Check if goals are loaded
        if (GoalManager.Instance.goals == null || GoalManager.Instance.goals.Length == 0)
        {
            Debug.LogError("[GoalDisplayManager] No goals loaded. Cannot initialize goal display.");
            return;
        }

        // Check if the player index is valid
        if (playerIndex < 0 || playerIndex >= 4)
        {
            Debug.LogError($"[GoalDisplayManager] Invalid player index: {playerIndex}. Cannot initialize goal display.");
            return;
        }

        // Get the goal for the player
        int assignedGoal = networkPlayer.goalIndex.Value;

        if (assignedGoal < 0 || assignedGoal >= GoalManager.Instance.goals.Length)
        {
            Debug.LogError($"[GoalDisplayManager] Invalid goal index: {assignedGoal}. Cannot initialize goal display.");
            return;
        }

        // Set the goal title and description
        goalTitle.text = GoalManager.Instance.goals[assignedGoal].title;
        goalDescription.text = GoalManager.Instance.goals[assignedGoal].description;

        Debug.Log($"[GoalDisplayManager] Goal Display initialized for player {playerIndex} with goal {assignedGoal}");

        StartCoroutine(CloseGoalCard());
    }

    private IEnumerator CloseGoalCard()
    {
        // Wait for 10 seconds
        yield return new WaitForSeconds(10f);
        // Close the goal card
        goalCard.SetActive(false);
    }
}
