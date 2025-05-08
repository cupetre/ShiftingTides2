using Unity.Netcode;
using UnityEngine;

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

    void Awake()
    {
        resourceManager = FindObjectOfType<ResourceManager>();
        gameManager     = FindObjectOfType<GameManager>();
        goalManager     = FindObjectOfType<GoalManager>();
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            // Initialize with 4 false values 
            for (int i = 0; i < 4; i++) achieved.Add(false);
        }
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
        if (achieved[playerIndex]) return;  // already achieved

        // Get the players goal
        var playerObject = gameManager.playerObjects[playerIndex];
        var netPlayer    = playerObject.GetComponent<NetworkPlayer>();
        int goalIdx      = netPlayer.goalIndex.Value;
        var goal         = goalManager.GetGoal(goalIdx);
        if (goal == null) return;

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
        else // Goal targeting opponents 
        {
            ok = true;
            for (int i = 0; i < 4; i++)
            {
                if (i == playerIndex) continue;
                int oMoney     = resourceManager.GetMoney(i);
                int oInfluence = resourceManager.GetInfluence(i);
                int oPeople    = resourceManager.GetPeople(i);
                if (oMoney     < goal.resources.money
                 || oInfluence < goal.resources.influence
                 || oPeople    < goal.resources.people)
                {
                    ok = false; break;
                }
            }
        }

        if (ok)
        {
            achieved[playerIndex] = true;
            Debug.Log($"[GoalAchieveManager] Player {playerIndex} achieved the goal {goal.title} (ID:{goal.id})");
            // Add some trigger UI later.
        }
    }
}
