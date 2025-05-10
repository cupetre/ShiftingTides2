using UnityEngine;
using TMPro;
using System.Collections;
using Unity.Netcode;
using UnityEngine.SceneManagement;

public class GoalDisplay : NetworkBehaviour
{
    int numResources = 3;
    public TMP_Text goalTitle;
    public TMP_Text goalDescription;

    private GameObject playerObject;
    private NetworkPlayer networkPlayer;
    public GameObject goalCard;

    public GameObject progressCard;

    public TMP_Text progressText;

    private ulong clientId;
    private int playerIndex;
    private int indexGoal;

    private Goal assignedGoal;
    private ResourceManager resourceManager;
    private void Start()
    {
        resourceManager = FindFirstObjectByType<ResourceManager>();
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

        // Initialize the assignedGoal display
        InitializeGoalDisplay();
        UpdateProgressDisplay();
    }

    private void InitializeGoalDisplay()
    {
        // Check if goals are loaded
        if (GoalManager.Instance.goals == null || GoalManager.Instance.goals.Length == 0)
        {
            Debug.LogError("[GoalDisplayManager] No goals loaded. Cannot initialize assignedGoal display.");
            return;
        }

        // Check if the player index is valid
        if (playerIndex < 0 || playerIndex >= 4)
        {
            Debug.LogError($"[GoalDisplayManager] Invalid player index: {playerIndex}. Cannot initialize assignedGoal display.");
            return;
        }

        // Get the assignedGoal for the player
        indexGoal = networkPlayer.goalIndex.Value;

        if (indexGoal < 0 || indexGoal >= GoalManager.Instance.goals.Length)
        {
            Debug.LogError($"[GoalDisplayManager] Invalid assignedGoal index: {indexGoal}. Cannot initialize assignedGoal display.");
            return;
        }

        assignedGoal = GoalManager.Instance.goals[indexGoal];
        // Set the assignedGoal title and description
        goalTitle.text = assignedGoal.title;
        goalDescription.text = assignedGoal.description;
        Debug.Log($"[GoalDisplayManager] Goal Display initialized for player {playerIndex} with assignedGoal {indexGoal}");

        StartCoroutine(CloseGoalCard());
    }

    public void UpdateProgressDisplay()
    {
        int curMoney = resourceManager.GetMoney(playerIndex);
        int curInfluence = resourceManager.GetInfluence(playerIndex);
        int curPeople = resourceManager.GetPeople(playerIndex);

        progressText.text = ""; // Initialize or clear previous text
        if (assignedGoal.Target == Goal.TargetType.Self)
        {

            if (assignedGoal.resources.money == curMoney || assignedGoal.resources.money == 0)
            {
                progressText.text += "OK!\n";
            }
            else
            {
                progressText.text += $"{curMoney}/{assignedGoal.resources.money}\n";
            }


            if (assignedGoal.resources.people == curPeople || assignedGoal.resources.people == 0)
            {
                progressText.text += "OK!\n";
            }
            else
            {
                progressText.text += $"{curPeople}/{assignedGoal.resources.people}\n";
            }

            if (assignedGoal.resources.influence == curInfluence || assignedGoal.resources.influence == 0)
            {
                progressText.text += "OK!\n";
            }
            else
            {
                progressText.text += $"{curInfluence}/{assignedGoal.resources.influence}";
            }


        }

    }

    private IEnumerator CloseGoalCard()
    {
        // Wait for 10 seconds
        yield return new WaitForSeconds(10f);
        // Close the assignedGoal card
        goalCard.SetActive(false);
        progressCard.SetActive(true);
    }
}
