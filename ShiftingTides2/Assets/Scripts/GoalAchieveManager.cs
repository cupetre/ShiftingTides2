using Unity.Netcode;
using UnityEngine;
using TMPro;

public class GoalAchieveManager : NetworkBehaviour
{
    [Tooltip("For each player: false = not achieved; true = achieved")]
    public NetworkList<bool> achieved = new NetworkList<bool>(
        default, 
        NetworkVariableReadPermission.Everyone, 
        NetworkVariableWritePermission.Server
    );

    private ResourceManager resourceManager;
    private GameManager gameManager;
    private GoalManager goalManager;

    [SerializeField] private GameObject LostCut;
    [SerializeField] private GameObject ProgressCard;
    private ScreenTransition screenTransition;
    
    private GoalDisplay goalDisplay;

    void Awake()
    {
        resourceManager = FindFirstObjectByType<ResourceManager>();
        gameManager     = FindFirstObjectByType<GameManager>();
        goalManager     = FindFirstObjectByType<GoalManager>();
        screenTransition = LostCut.GetComponent<ScreenTransition>();
        goalDisplay = FindFirstObjectByType<GoalDisplay>();
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            // Initialize with 4 false values 
            for (int i = 0; i < 4; i++) achieved.Add(false);
        }

        // For testing purposes
        // screenTransition.SetPlayerLost(false, 0);
    }

    // Call this from the TurnManager at the end of the turn
    [ServerRpc(RequireOwnership = false)]
    public void CheckGoalServerRpc(int playerIndex)
    {
        CheckGoal(playerIndex);
    }

    // Goal checking logic
    private void CheckGoal(int playerIndex)
    {
        if (achieved[playerIndex]) return; // Already achieved

        // Search for the player object
        var playerObject = gameManager.playerObjects[playerIndex];
        var netPlayer = playerObject.GetComponent<NetworkPlayer>();
        int goalIdx = netPlayer.goalIndex.Value;
        var goal = goalManager.GetGoal(goalIdx);
        if (goal == null) return;

        // Search for the player's resources
        int curMoney = resourceManager.GetMoney(playerIndex);
        int curInfluence = resourceManager.GetInfluence(playerIndex);
        int curPeople = resourceManager.GetPeople(playerIndex);

        // Search for the current round
        int currentRound = FindFirstObjectByType<RoundManager>().round.Value;

        bool goalMet = false;

        if (goal.Target == Goal.TargetType.Self)
        {
            // Type achieve (can be achieved at any time)
            if (goal.Type == "achieve")
            {
                goalMet = curMoney >= goal.resources.money
                    && curInfluence >= goal.resources.influence
                    && curPeople >= goal.resources.people;
            }
            // Type rounds (must be achieved in a certain round)
            else if (goal.Type == "rounds")
            {
                if (currentRound <= goal.rounds)
                {
                    goalMet = curMoney >= goal.resources.money
                        && curInfluence >= goal.resources.influence
                        && curPeople >= goal.resources.people;
                }
            }
        }

        if (goalMet)
        {
            achieved[playerIndex] = true;
            Debug.Log($"[GoalAchieveManager] Player {playerIndex} achieved the goal {goal.title} (ID:{goal.id})");
            screenTransition.SetPlayerWon(playerIndex);
        }

        goalDisplay.UpdateProgressDisplay();
    }
}
