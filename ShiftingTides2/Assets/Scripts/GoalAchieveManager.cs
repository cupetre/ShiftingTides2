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

    // Goal checking logic
    public bool CheckGoal(int playerIndex)
    {
        if (achieved[playerIndex]) return false;  // already achieved

        // Get the players goal
        var playerObject = gameManager.playerObjects[playerIndex];
        var netPlayer    = playerObject.GetComponent<NetworkPlayer>();
        int goalIdx      = netPlayer.goalIndex.Value;
        var goal         = goalManager.GetGoal(goalIdx);
        if (goal == null) return false;

        // Current player resources
        int curMoney     = resourceManager.GetMoney(playerIndex);
        int curInfluence = resourceManager.GetInfluence(playerIndex);
        int curPeople    = resourceManager.GetPeople(playerIndex);

        bool ok = false;
        // Goal targeting self
        if (goal.Target == Goal.TargetType.Self)
        {
            ok = curMoney     >= goal.resources.money
              && curInfluence >= goal.resources.influence
              && curPeople    >= goal.resources.people;
        }

        if (ok)
        {
            achieved[playerIndex] = true;
            return true;
        }

        goalDisplay.UpdateProgressDisplay();
    return false;
        
    }
}
