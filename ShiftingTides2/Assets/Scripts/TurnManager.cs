using UnityEngine;
using UnityEngine.Networking;
using Unity.Netcode;
using TMPro;
using System.Collections.Generic;
using System.Linq;
using System.Collections;

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

    private Trade[] trades;
    private GameObject tradeCard;
    private GameObject voteButtons;

    public NetworkVariable<int> currentPlayer = new NetworkVariable<int>(-1);
    public NetworkVariable<int> currentTurn = new NetworkVariable<int>(-1);
    public NetworkVariable<int> currentTrade = new NetworkVariable<int>(-1);

    public ulong[] clientIds;
    public bool turnActive = false;

    public int[] usedTrades;

    void Start()
    {
        if (!IsServer) return;
        // Find trade manager and game manager instances in the scene
        tradeManager = FindFirstObjectByType<TradeManager>();
        gameManager = FindFirstObjectByType<GameManager>();
        roundManager = FindFirstObjectByType<RoundManager>();
        // Find trade display script inside the same object
        tradeDisplay = FindFirstObjectByType<TradeDisplay>();
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

    void FixedUpdate()
    {

    }

    [ServerRpc(RequireOwnership = false)]
    public void StartTurnServerRpc()
    {
        foreach (var player in players)
        {
            if (player == null)
            {
                Debug.LogError("[TurnManager] Player object is null.");
                return;
            }
            // Hide goal card for all players
            GoalManager goalManager = FindFirstObjectByType<GoalManager>();
            if (goalManager == null)
            {
                Debug.LogError("[TurnManager] GoalManager instance not found.");
                return;
            }

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

        // Make the player in turn object larger
        Vector3 currScale = players[playerIndex].gameObject.transform.localScale;
        players[playerIndex].gameObject.transform.localScale = new Vector3(currScale.x * 2.0f, currScale.y * 2.0f, 1.0f);

        // while (usedTrades.Contains(trade.id))
        // {
        //     Debug.LogError($"[TurnManager] Trade {trade.id} has already been used. Getting new one");
        //     trade = tradeManager.GetRandomTrade();
        // }

        // usedTrades.Append(trade.id);
        currentTrade.Value = trade.id;
        Debug.Log($"[TurnManager] Player {playerIndex} is starting trade {trade.title} (ID: {trade.id})");

        StartCoroutine(TradeCoroutine(playerIndex, trade));
    }

    private IEnumerator TradeCoroutine(int playerIndex, Trade trade)
    {
        Debug.Log($"[TurnManager] Starting trade coroutine for player {playerIndex} with trade {trade.title}");
        voteManager.yesVotes.Value = 0;
        voteManager.noVotes.Value = 0;

        yield return new WaitForSeconds(10f);
        // Display the trade to the current player
        tradeDisplay.displayTradeClientRpc(clientIds[playerIndex], trade);
        // Wait for 5 seconds before displaying the vote buttons
        yield return new WaitForSeconds(5f);

        voteManager.DisplayVoteButtonsServerRpc();
        voteManager.HideVoteButtonsClientRpc(clientIds[playerIndex]);

        // Wait for the vote to be completed or 40s to pass
        float waitTime = 40f;
        float elapsedTime = 0f;
        while (elapsedTime < waitTime)
        {
            if (voteManager.voteDone.Value)
            {
                Debug.Log("[TurnManager] Vote completed.");
                break;
            }
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        Debug.Log($"[TurnManager] Vote completed or timed out. Elapsed time: {elapsedTime}");
        voteManager.HideVoteButtonsClientRpc();
        // Process the trade after the vote
        StartCoroutine(ProcessTrade(playerIndex, trade));
    }

    private IEnumerator ProcessTrade(int playerIndex, Trade trade)
    {
        Debug.Log($"[TurnManager] Processing trade for player {playerIndex} with trade {trade.title}");

        // Get vote data 
        int[] playerYes = new int[voteManager.playerYes.Count];
        for (int i = 0; i < voteManager.playerYes.Count; i++)
        {
            playerYes[i] = voteManager.playerYes[i];
        }
        Debug.Log($"[TurnManager] Players who voted yes: {string.Join(", ", playerYes)}");

        // Get resource manager
        resourceManager = FindFirstObjectByType<ResourceManager>();
        if (resourceManager == null)
        {
            Debug.LogError("[TurnManager] ResourceManager instance not found.");
            yield break;
        }

        // Apply self effects
        int yesVotesRatio = voteManager.playerYes.Count / numPlayers;
        ApplyTradeEffects(playerIndex,
                        trade.effect.selfMoney * yesVotesRatio,
                        trade.effect.selfPeople * yesVotesRatio,
                        trade.effect.selfInfluence * yesVotesRatio,
                        isSelf: true);

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

        // End the turn
        StartCoroutine(EndTurnCoroutine(playerIndex));
        tradeInProgress = false;
        Debug.Log($"[TurnManager] Trade completed for player {playerIndex}");
        yield return null;
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
    private IEnumerator EndTurnCoroutine(int playerIndex)
    {
        // Wait for the trade to be processed
        yield return new WaitForSeconds(5f);

        // Verify if the player has lost
        FindFirstObjectByType<ResourceManager>().CheckGoalTimeoutServerRpc(playerIndex);

        // Verify if the player has achieved their goal
        FindFirstObjectByType<GoalAchieveManager>()
            .CheckGoalServerRpc(playerIndex);
        roundManager.TurnEndedServerRpc(playerIndex);

        // End the turn for the current player
        Debug.Log($"[TurnManager] Ending turn for player {playerIndex}");

        currentPlayer.Value = (currentPlayer.Value + 1) % numPlayers;
        currentTurn.Value++;

        currentTrade.Value = -1; // Reset trade index for the next round

        voteManager.voteDone.Value = false;
        Debug.Log($"[TurnManager] Player {currentPlayer.Value}'s turn started.");

        Vector3 currScale = players[playerIndex].gameObject.transform.localScale;
        players[playerIndex].gameObject.transform.localScale = new Vector3(currScale.x / 2.0f, currScale.y / 2.0f, 1.0f);
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
}
