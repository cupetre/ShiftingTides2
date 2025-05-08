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

    public List<GameObject> players = new List<GameObject>();

    private TradeManager tradeManager;
    private GameManager gameManager;
    private TradeDisplay tradeDisplay;
    private VoteManager voteManager;
    private ResourceManager resourceManager;

    private Trade[] trades;

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
        // Find trade display instance in the TradeCard object in the scene
        tradeDisplay = FindFirstObjectByType<TradeDisplay>();
        // Find vote manager instance in the scene
        voteManager = FindFirstObjectByType<VoteManager>();
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

        for (int i = 0; i < numPlayers; i++)
        {
            GameObject player = NetworkManager.Singleton.SpawnManager.GetPlayerNetworkObject(clientIds[i]).gameObject;
            players.Add(player);
            Debug.Log($"[TurnManager] Player {i} assigned to client ID {clientIds[i]}");
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
        if (currentPlayer.Value == -1)
        {
            currentPlayer.Value = 0;
            currentTurn.Value = 0;
        }
        else
        {
            currentPlayer.Value = (currentPlayer.Value + 1) % numPlayers;
            currentTurn.Value++;
        }
        // Check if all players have taken their turns
        if (currentTurn.Value >= numPlayers)
        {
            currentTurn.Value = 0;
            currentTrade.Value = -1; // Reset trade index for the next round
        }
        // Start the turn for the current player
        StartTradeServerRpc(currentPlayer.Value);
    }

    [ServerRpc(RequireOwnership = false)]
    public void StartTradeServerRpc(int playerIndex)
    {
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

        while (usedTrades.Contains(trade.id)) {
            Debug.LogError($"[TurnManager] Trade {trade.id} has already been used. Getting new one");
            trade = tradeManager.GetRandomTrade();
        }

        usedTrades.Append(trade.id);
        currentTrade.Value = trade.id;
        Debug.Log($"[TurnManager] Player {playerIndex} is starting trade {trade.title} (ID: {trade.id})");

        StartCoroutine(TradeCoroutine(playerIndex, trade));

        // Process the trade and resources based on votes and the trade type
        StartCoroutine(ProcessTrade(playerIndex, trade));
    }

    private IEnumerator TradeCoroutine(int playerIndex, Trade trade)
    {
        voteManager.yesVotes.Value = 0;
        voteManager.noVotes.Value = 0;

        // Display the trade to the current player
        tradeDisplay.displayTradeClientRpc(clientIds[playerIndex], trade);
        // Wait for 5 seconds before displaying the vote buttons
        yield return new WaitForSeconds(5f);

        voteManager.DisplayVoteButtonsServerRpc();

        // Wait for the vote to be completed or 40s to pass
        float waitTime = 40f;
        float elapsedTime = 0f;
        while (elapsedTime < waitTime)
        {
            if (voteManager.voteDone.Value)
            {
                break;
            }
            elapsedTime += Time.deltaTime;
            yield return null;
        }
    }

    private IEnumerator ProcessTrade(int playerIndex, Trade trade)
    {
        // Wait for the vote to be completed
        yield return new WaitUntil(() => voteManager.voteDone.Value);

        // Check which player voted yes or no
        int yesVotes = voteManager.yesVotes.Value;
        int noVotes = voteManager.noVotes.Value;

        Debug.Log($"[TurnManager] Player {playerIndex} received {yesVotes} yes votes and {noVotes} no votes.");

        int[] playerYes = voteManager.playerYes.Select(v => v.Value).ToArray();
        int[] playerNo = voteManager.playerNo.Select(v => v.Value).ToArray();

        Debug.Log($"[TurnManager] Players who voted yes: {string.Join(", ", playerYes)}");
        Debug.Log($"[TurnManager] Players who voted no: {string.Join(", ", playerNo)}");

        string tradeType = trade.type;

        if (tradeType == "normal")
        {
            int moneySelf = trade.effect.selfMoney;
            int moneyOther = trade.effect.othersMoney;
            int peopleSelf = trade.effect.selfPeople;
            int peopleOther = trade.effect.othersPeople;
            int influenceSelf = trade.effect.selfInfluence;
            int influenceOther = trade.effect.othersInfluence;

            // Apply the trade effects
            
        }
    }
}
