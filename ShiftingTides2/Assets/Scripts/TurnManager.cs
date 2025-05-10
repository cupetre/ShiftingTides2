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
            float goalCloseDelay = 10.0f;
            float timeElapsed = 0.0f;
            while (timeElapsed < goalCloseDelay)
            {
                timeElapsed += Time.deltaTime;
            }
            goalManager.CloseGoalCardServerRpc();
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

        while (usedTrades.Contains(trade.id)) {
            Debug.LogError($"[TurnManager] Trade {trade.id} has already been used. Getting new one");
            trade = tradeManager.GetRandomTrade();
        }

        usedTrades.Append(trade.id);
        currentTrade.Value = trade.id;
        Debug.Log($"[TurnManager] Player {playerIndex} is starting trade {trade.title} (ID: {trade.id})");

        StartCoroutine(TradeCoroutine(playerIndex, trade));
    }

    private IEnumerator TradeCoroutine(int playerIndex, Trade trade)
    {
        Debug.Log($"[TurnManager] Starting trade coroutine for player {playerIndex} with trade {trade.title}");
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
                Debug.Log("[TurnManager] Vote completed.");
                break;
            }
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        Debug.Log($"[TurnManager] Vote completed or timed out. Elapsed time: {elapsedTime}");

        // Process the trade after the vote
        StartCoroutine(ProcessTrade(playerIndex, trade));
    }

    private IEnumerator ProcessTrade(int playerIndex, Trade trade)
    {
        Debug.Log($"[TurnManager] Processing trade for player {playerIndex} with trade {trade.title}");

        // Check which player voted yes or no
        int yesVotes = voteManager.yesVotes.Value;
        int noVotes = voteManager.noVotes.Value;
        Debug.Log($"[TurnManager] Number of votes: Yes: {yesVotes}, No: {noVotes}");

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
            resourceManager = FindFirstObjectByType<ResourceManager>();
            if (resourceManager == null)
            {
                Debug.LogError("[TurnManager] ResourceManager instance not found.");
                yield break;
            }

            if (moneySelf != 0)
            {
                resourceManager.AddMoneyServerRpc(playerIndex, moneySelf);
                Debug.Log($"[TurnManager] Player {playerIndex} received {moneySelf} money.");
                for (int i = 0; i < playerYes.Length; i++)
                {
                    resourceManager.AddMoneyServerRpc(playerYes[i], moneyOther);
                    Debug.Log($"[TurnManager] Player {playerYes[i]} received {moneyOther} money.");
                }
            }

            if (peopleSelf != 0)
            {
                resourceManager.AddPeopleServerRpc(playerIndex, peopleSelf);
                Debug.Log($"[TurnManager] Player {playerIndex} received {peopleSelf} people.");
                for (int i = 0; i < playerYes.Length; i++)
                {
                    resourceManager.AddPeopleServerRpc(playerYes[i], peopleOther);
                    Debug.Log($"[TurnManager] Player {playerYes[i]} received {peopleOther} people.");
                }
            }

            if (influenceSelf != 0)
            {
                resourceManager.AddInfluenceServerRpc(playerIndex, influenceSelf);
                Debug.Log($"[TurnManager] Player {playerIndex} received {influenceSelf} influence.");
                for (int i = 0; i < playerYes.Length; i++)
                {
                    resourceManager.AddInfluenceServerRpc(playerYes[i], influenceOther);
                    Debug.Log($"[TurnManager] Player {playerYes[i]} received {influenceOther} influence.");
                }
            }
        }
        else
        {
             Debug.LogError($"[TurnManager] Unknown trade type: {tradeType}");
        }


        // End the turn
        StartCoroutine(EndTurnCoroutine(playerIndex));
        tradeInProgress = false;
        Debug.Log($"[TurnManager] Ending trade for player {playerIndex}");
    }

    private IEnumerator EndTurnCoroutine(int playerIndex)
    {
        // Wait for the trade to be processed
        yield return new WaitForSeconds(5f);

        // Verify if the player has achieved their goal
        FindObjectOfType<GoalAchieveManager>()
            .CheckGoalServerRpc(playerIndex);

        // End the turn for the current player
        Debug.Log($"[TurnManager] Ending turn for player {playerIndex}");

        currentPlayer.Value = (currentPlayer.Value + 1) % numPlayers;
        currentTurn.Value++;

        currentTrade.Value = -1; // Reset trade index for the next round

        voteManager.voteDone.Value = false;

        Debug.Log($"[TurnManager] Player {currentPlayer.Value}'s turn started.");

        StartTurnServerRpc();
    }
}
