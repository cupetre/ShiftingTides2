using UnityEngine;
using UnityEngine.Networking;
using Unity.Netcode;
using TMPro;
using System.Collections.Generic;
using System.Linq;
using System.Collections;
// using UnityEditor.Rendering.LookDev;

public class TurnManager : NetworkBehaviour
{
    public int numPlayers = 4;
    private bool tradeInProgress = false;
    public List<GameObject> players = new List<GameObject>();

    private TradeManager tradeManager;
    private GameManager gameManager;
    private TradeDisplay tradeDisplay;
    private VoteManager voteManager;
    private ResourceManager resourceManager;
    private RoundManager roundManager;
    private GoalAchieveManager goalManager;
    private ScreenTransition screenTransition;
    private GoalDisplay goalDisplay;

    private Trade[] trades;
    private GameObject voteButtons;

    public NetworkVariable<int> currentPlayer = new NetworkVariable<int>(-1);
    public NetworkVariable<int> currentTurn = new NetworkVariable<int>(-1);
    public NetworkVariable<int> currentTrade = new NetworkVariable<int>(-1);

    public ulong[] clientIds;
    public bool turnActive = false;
    public int[] usedTrades;

    //timer variables
    [SerializeField] private TMP_Text timerText; // Assign in inspector (optional, if UI is needed)
    private Coroutine timerCoroutine;
    [SerializeField] private float turnDuration = 45f;
    private NetworkVariable<float> remainingTime = new(writePerm: NetworkVariableWritePermission.Server);

    public override void OnNetworkSpawn()
    {
        if (IsClient)
        {
            remainingTime.OnValueChanged += UpdateTimerUI;
        }
    }
    void Start()
    {
        if (!IsServer) return;

        // Apply Instances
        tradeManager = FindFirstObjectByType<TradeManager>();
        gameManager = FindFirstObjectByType<GameManager>();
        roundManager = FindFirstObjectByType<RoundManager>();
        resourceManager = FindFirstObjectByType<ResourceManager>();
        goalManager = FindFirstObjectByType<GoalAchieveManager>();
        tradeDisplay = FindFirstObjectByType<TradeDisplay>();
        goalDisplay = FindFirstObjectByType<GoalDisplay>();

        // Find vote manager instance in the scene
        voteButtons = GameObject.Find("YesNoButton");
        if (voteButtons == null)
        {
            Debug.LogError("[TurnManager] YesNoButton object not found in the scene.");
            return;
        }
        voteManager = voteButtons.GetComponent<VoteManager>();
        if (tradeDisplay == null)
        {
            Debug.LogError("[TurnManager] TradeDisplay instance not found.");
            return;
        }
        if (tradeManager == null)
        {
            Debug.LogError("[TurnManager] TradeManager instance not found.");
            return;
        }
        if (gameManager == null)
        {
            Debug.LogError("[TurnManager] GameManager instance not found.");
            return;
        }
        if (voteManager == null)
        {
            Debug.LogError("[TurnManager] VoteManager instance not found.");
            return;
        }

        screenTransition = FindFirstObjectByType<ScreenTransition>();
        if (screenTransition == null)
            Debug.LogError("[TurnManager] ScreenTransition não encontrado.");

        trades = tradeManager.trades;
        clientIds = gameManager.clientIds.Value.ToArray();

        if (trades == null || trades.Length == 0)
        {
            Debug.LogError("[TurnManager] No trades available.");
            return;
        }

        gameManager.InitializePlayersIfNeeded();
        // Initialize the players list with player objects
        for (int i = 0; i < numPlayers; i++)
        {
            if (gameManager.playerObjects[i] == null)
            {
                Debug.LogError($"[TurnManager] Player object at index {i} is null.");
                return;
            }
            // Check if the player object is already in the list
            if (!players.Contains(gameManager.playerObjects[i]))
            {
                players.Add(gameManager.playerObjects[i]);
                Debug.Log($"[TurnManager] Player object at index {i} added to the list.");
            }
            else
            {
                Debug.LogWarning($"[TurnManager] Player object at index {i} is already in the list.");
            }
        }

        // Start the first turn
        StartTurnServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    public void StartTurnServerRpc()
    {
        GoalManager goalManager = FindFirstObjectByType<GoalManager>();
        if (goalManager == null)
        {
            Debug.LogError("[TurnManager] GoalManager instance not found.");
            return;
        }

        foreach (var player in players)
        {
            if (player == null)
            {
                Debug.LogError("[TurnManager] Player object is null.");
                return;
            }
            // You can add player-specific logic here if needed
            // No need to find GoalManager every time
        }

        if (currentPlayer.Value == -1)
        {
            currentPlayer.Value = 0;
            currentTurn.Value = 0;
        }
        // Start the turn for the current player
        StartTradeServerRpc(currentPlayer.Value);
    }

    [ServerRpc(RequireOwnership = false)]
    public void StartTradeServerRpc(int playerIndex)
    {
        // Check if trade is already in progress
        if (tradeInProgress)
        {
            Debug.LogError("[TurnManager] Trade is already in progress.");
            return;
        }
        tradeInProgress = true;

        if (playerIndex < 0 || playerIndex >= numPlayers)
        {
            Debug.LogError($"[TurnManager] Invalid player index: {playerIndex}");
            return;
        }
        // Check if the current player is the one whose turn it is
        if (currentPlayer.Value != playerIndex)
        {
            Debug.LogError($"[TurnManager] It's not player {playerIndex}'s turn.");
            return;
        }
        // Start the trade for the current player
        Trade trade = tradeManager.GetRandomTrade();
        HiddenCard hiddenCard = HiddenCardManager.Instance.GetRandomHiddenCard();

        // Make the player in turn object larger
        scalePlayerClientRpc(false, playerIndex);

        currentTrade.Value = trade.id;

        Debug.Log($"[TurnManager] Player {playerIndex} is starting trade {trade.title} (ID: {trade.id})");
        Debug.Log($"[TurnManager] Hidden Card: {hiddenCard.description}");
        StartCoroutine(TradeCoroutine(playerIndex, trade, hiddenCard));
    }

    [ClientRpc]
    private void scalePlayerClientRpc(bool tradeEnd, int playerIndex)
    {
        Vector3 currScale = players[playerIndex].gameObject.transform.localScale;
        if (tradeEnd)
        {
            players[playerIndex].gameObject.transform.localScale = new Vector3(currScale.x / 2.0f, currScale.y / 2.0f, 1.0f);
        }
        else
        {
            players[playerIndex].gameObject.transform.localScale = new Vector3(currScale.x * 2.0f, currScale.y * 2.0f, 1.0f);
        }

    }

    private IEnumerator TradeCoroutine(int playerIndex, Trade trade, HiddenCard hiddenCard)
    {
        Debug.Log($"[TurnManager] Starting trade coroutine for player {playerIndex} with trade {trade.title}");

        voteManager.yesVotes.Value = 0;
        voteManager.noVotes.Value = 0;

        // Optional: shorten or remove delay if unnecessary
        yield return new WaitForSeconds(2f); // instead of 10s if you want snappier

        // Display the trade to the current player safely
        if (playerIndex < clientIds.Length)
        {
            tradeDisplay.DisplayTradeClientRpc(clientIds[playerIndex], trade);
            if (hiddenCard != null)
            {
                tradeDisplay.DisplayHiddenCardClientRpc(clientIds[playerIndex], hiddenCard);
            }
        }
        else
        {
            Debug.LogError("[TurnManager] Invalid playerIndex or clientIds array.");
        }

        // Wait before showing vote buttons so player can read trade info
        yield return new WaitForSeconds(3f);

        voteManager.DisplayVoteButtonsServerRpc();

        // Hide vote buttons for current player if that is intentional
        voteManager.HideVoteButtonsClientRpc(clientIds[playerIndex]);

        timerCoroutine = StartCoroutine(TurnTimerRoutine(playerIndex, trade, hiddenCard));
    }

    private IEnumerator TurnTimerRoutine(int playerIndex, Trade trade, HiddenCard hiddenCard)
    {
        float elapsed = 0f;

        while (elapsed < turnDuration)
        {
            if (voteManager.voteDone.Value)
            {
                Debug.Log("[TurnManager] All players voted. Ending turn early.");
                break;
            }

            remainingTime.Value = turnDuration - elapsed;

            elapsed += Time.deltaTime;
            yield return null;
        }

        remainingTime.Value = 0f;

        voteManager.HideVoteButtonsClientRpc();

        StartCoroutine(ProcessTrade(playerIndex, trade, hiddenCard));
    }
    private IEnumerator ProcessTrade(int playerIndex, Trade trade, HiddenCard hiddenCard)
    {
        Debug.Log($"[TurnManager] Processing trade for player {playerIndex} with trade {trade.title}");

        // Get vote data 
        int[] playerYes = new int[voteManager.playerYes.Count];
        for (int i = 0; i < voteManager.playerYes.Count; i++)
        {
            playerYes[i] = voteManager.playerYes[i];
        }
        Debug.Log($"[TurnManager] Players who voted yes: {string.Join(", ", playerYes)}");

        // Check resource manager
        if (resourceManager == null)
        {
            Debug.LogError("[TurnManager] ResourceManager instance not found.");
            yield break;
        }

        // Apply self effects
        int yesVotesCount = voteManager.playerYes.Count;

        int totalSelfMoney = trade.effect.selfMoney * yesVotesCount;
        int totalSelfPeople = trade.effect.selfPeople * yesVotesCount;
        int totalSelfInfluence = trade.effect.selfInfluence * yesVotesCount;

        ApplyTradeEffects(playerIndex,
            totalSelfMoney,
            totalSelfPeople,
            totalSelfInfluence,
            isSelf: true);
        goalDisplay.UpdateProgressDisplay();

        tradeDisplay.revealCardsForAllClientRpc(trade, hiddenCard);
        float waitTime = 5f;
        float elapsedTime = 0f;

        while (elapsedTime < waitTime)
        {
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        tradeDisplay.closeCardsClientRpc(trade, hiddenCard);

        // Apply others effects
        foreach (int otherPlayerId in playerYes)
        {
            if (otherPlayerId != playerIndex && otherPlayerId >= 0 && otherPlayerId < numPlayers)
            {
                ApplyTradeEffects(otherPlayerId,
                                trade.effect.othersMoney,
                                trade.effect.othersPeople,
                                trade.effect.othersInfluence,
                                isSelf: false);
            }
        }
        if (hiddenCard != null)
        {
            Debug.Log("Applying Hidden Card Effects");
            ApplyHiddenCardEffects(playerIndex, hiddenCard);


        }
        bool checkWin = false;
        int winnerIndex = -1;
        for (int i = 0; i < numPlayers; i++)
        {
            checkWin = goalManager.CheckGoal(i);
            if (checkWin)
            {
                winnerIndex = i;
                break;
            }
        }
        if (checkWin && winnerIndex != -1)
        {
            Debug.Log($"[GoalAchieveManager] Player {winnerIndex} achieved the goal!");

            if (screenTransition != null)
                screenTransition.SetPlayerWonClientRpc(winnerIndex);

            tradeInProgress = false;

            yield break;
        }
        else
        {
            tradeInProgress = false;
            Debug.Log($"[TurnManager] Trade completed for player {playerIndex}");

            // Player didn’t win, continue to EndTurnCoroutine
            StartCoroutine(EndTurnCoroutine(playerIndex, trade, hiddenCard));
        }
        yield return null;
    }

    private IEnumerator EndTurnCoroutine(int playerIndex, Trade trade, HiddenCard hiddenCard)
    {
        // Wait for the trade to be processed
        yield return new WaitForSeconds(5f);

        roundManager.TurnEndedServerRpc(playerIndex);

        // End the turn for the current player
        Debug.Log($"[TurnManager] Ending turn for player {playerIndex}");

        currentPlayer.Value = (currentPlayer.Value + 1) % numPlayers;
        currentTurn.Value++;

        currentTrade.Value = -1; // Reset trade index for the next round

        voteManager.voteDone.Value = false;
        Debug.Log($"[TurnManager] Player {currentPlayer.Value}'s turn started.");

        players[playerIndex].gameObject.transform.localScale = Vector3.one;

        foreach (var player in players)
        {
            var networkPlayer = player.GetComponent<NetworkPlayer>();
            if (networkPlayer != null)
            {
                networkPlayer.emotionState.Value = EmotionState.Neutral;
            }
        }

        StartTurnServerRpc();
    }

    private void ApplyTradeEffects(int playerId, int money, int people, int influence, bool isSelf)
    {
        string target = isSelf ? "SELF" : "OTHER";

        if (money != 0)
        {
            resourceManager.AddMoneyServerRpc(playerId, money);
            Debug.Log($"[TurnManager] {target} Player {playerId} received {money} money. (Current: {resourceManager.GetMoney(playerId)})");
        }

        if (people != 0)
        {
            resourceManager.AddPeopleServerRpc(playerId, people);
            Debug.Log($"[TurnManager] {target} Player {playerId} received {people} people. (Current: {resourceManager.GetPeople(playerId)})");
        }

        if (influence != 0)
        {
            resourceManager.AddInfluenceServerRpc(playerId, influence);
            Debug.Log($"[TurnManager] {target} Player {playerId} received {influence} influence. (Current: {resourceManager.GetInfluence(playerId)})");
        }
    }
    private void ApplyHiddenCardEffects(int playerId, HiddenCard hidden)
    {
        string type = hidden.type;

        int[] playerYes = new int[voteManager.playerYes.Count];
        for (int i = 0; i < voteManager.playerYes.Count; i++)
        {
            playerYes[i] = voteManager.playerYes[i];
        }

        if (((type == "compensation" || type == "against-one") && voteManager.playerYes.Count == hidden.counts) ||
        (type == "against-yes-voters" && voteManager.playerYes.Count >= hidden.counts))
        {
            if (hidden.effect.selfMoney != 0)
            {
                resourceManager.AddMoneyServerRpc(playerId, hidden.effect.selfMoney);
            }
            if (hidden.effect.othersMoney != 0)
            {
                foreach (int yesVoter in playerYes)
                {
                    resourceManager.AddMoneyServerRpc(yesVoter, hidden.effect.othersMoney);
                }
            }
            if (hidden.effect.selfPeople != 0)
            {
                resourceManager.AddPeopleServerRpc(playerId, hidden.effect.selfPeople);
            }
            if (hidden.effect.othersPeople != 0)
            {
                foreach (int yesVoter in playerYes)
                {
                    resourceManager.AddPeopleServerRpc(yesVoter, hidden.effect.othersPeople);
                }
            }
            if (hidden.effect.selfInfluence != 0)
            {
                resourceManager.AddInfluenceServerRpc(playerId, hidden.effect.selfInfluence);
            }
            if (hidden.effect.othersInfluence != 0)
            {
                foreach (int yesVoter in playerYes)
                {

                    resourceManager.AddInfluenceServerRpc(yesVoter, hidden.effect.othersInfluence);
                }
            }
        }
        if ((type == "against-all") && voteManager.playerYes.Count == hidden.counts)
        {
            int[] playerNo = new int[voteManager.playerNo.Count];
            for (int i = 0; i < voteManager.playerNo.Count; i++)
            {
                playerNo[i] = voteManager.playerNo[i];
            }
            if (hidden.effect.selfMoney != 0)
            {
                resourceManager.AddMoneyServerRpc(playerId, hidden.effect.selfMoney);
            }
            if (hidden.effect.othersMoney != 0)
            {
                foreach (int noVoter in playerNo)
                {
                    resourceManager.AddMoneyServerRpc(noVoter, hidden.effect.othersMoney);
                }
            }
            if (hidden.effect.selfPeople != 0)
            {
                resourceManager.AddPeopleServerRpc(playerId, hidden.effect.selfPeople);
            }
            if (hidden.effect.othersPeople != 0)
            {
                foreach (int noVoter in playerNo)
                {
                    resourceManager.AddPeopleServerRpc(noVoter, hidden.effect.othersPeople);
                }
            }
            if (hidden.effect.selfInfluence != 0)
            {
                resourceManager.AddInfluenceServerRpc(playerId, hidden.effect.selfInfluence);
            }
            if (hidden.effect.othersInfluence != 0)
            {
                foreach (int noVoter in playerNo)
                {
                    resourceManager.AddInfluenceServerRpc(noVoter, hidden.effect.othersInfluence);
                }
            }

        }

    }

    private void UpdateTimerUI(float oldValue, float newValue)
    {
        if (timerText != null)
        {
            timerText.text =Mathf.CeilToInt(newValue) + "s";
        }
    }

}

