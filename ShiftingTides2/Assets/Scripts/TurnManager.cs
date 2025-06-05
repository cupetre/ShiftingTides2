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
    private GoalAchieveManager goalManager;
    private ScreenTransition screenTransition;
    private GoalDisplay goalDisplay;

    AudioManager audioManager;

    private Trade[] trades;
    private GameObject voteButtons;

    public NetworkVariable<int> currentPlayer = new NetworkVariable<int>(-1);
    public NetworkVariable<int> currentTurn = new NetworkVariable<int>(-1);
    public NetworkVariable<int> currentTrade = new NetworkVariable<int>(-1);

    public ulong[] clientIds;
    public bool turnActive = false;
    public int[] usedTrades;

    [SerializeField] private TMP_Text timerText;
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

        tradeManager = FindFirstObjectByType<TradeManager>();
        gameManager = FindFirstObjectByType<GameManager>();
        roundManager = FindFirstObjectByType<RoundManager>();
        resourceManager = FindFirstObjectByType<ResourceManager>();
        goalManager = FindFirstObjectByType<GoalAchieveManager>();
        tradeDisplay = FindFirstObjectByType<TradeDisplay>();
        goalDisplay = FindFirstObjectByType<GoalDisplay>();
        audioManager = FindFirstObjectByType<AudioManager>();

        voteButtons = GameObject.Find("YesNoButton");
        if (voteButtons == null)
        {
            Debug.LogError("[TurnManager] YesNoButton object not found in the scene.");
            return;
        }
        voteManager = voteButtons.GetComponent<VoteManager>();
        if (tradeDisplay == null || tradeManager == null || gameManager == null || voteManager == null)
        {
            Debug.LogError("[TurnManager] One or more managers not found.");
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
        for (int i = 0; i < numPlayers; i++)
        {
            if (gameManager.playerObjects[i] == null)
            {
                Debug.LogError($"[TurnManager] Player object at index {i} is null.");
                return;
            }
            if (!players.Contains(gameManager.playerObjects[i]))
            {
                players.Add(gameManager.playerObjects[i]);
                Debug.Log($"[TurnManager] Player object at index {i} added to the list.");
            }
        }

        StartTurnServerRpc();
    }

    private void FixedUpdate()
    {
        if (IsServer && tradeInProgress)
        {
            // Hide voting buttons for all players who have lost
            foreach (GameObject player in players)
            {
                var networkPlayer = player.GetComponent<NetworkPlayer>();
                if (networkPlayer != null && networkPlayer.playerLost.Value)
                {
                    voteManager.HideVoteButtonsClientRpc(clientIds[networkPlayer.playerIndex.Value]);
                }
            }
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void StartTurnServerRpc()
    {
        UpdatePlayersAliveServerRpc();
        if (currentPlayer.Value == -1)
        {
            currentPlayer.Value = 0;
            currentTurn.Value = 0;
        }
        else
        {
            // Revert scale for previous player
            if (currentPlayer.Value == 0)
            {
                for (int i = 3; i >= 0; i--)
                {
                    // Revert scale for the player whose turn just ended, but keep checking if they haven't lost (playerLost in NetworkPlayer)
                    var player = players[i];
                    var networkPlayer = player.GetComponent<NetworkPlayer>();
                    if (networkPlayer != null && !networkPlayer.playerLost.Value)
                    {
                        revertScaleClientRpc(i);
                        break;
                    }
                }
            }
            else
            {
                // Revert scale for the previous player, but keep checking if they haven't lost (playerLost in NetworkPlayer)
                for (int i = currentPlayer.Value - 1; i >= 0; i--)
                {
                    var player = players[i];
                    var networkPlayer = player.GetComponent<NetworkPlayer>();
                    if (networkPlayer != null && !networkPlayer.playerLost.Value)
                    {
                        revertScaleClientRpc(i);
                        break;
                    }
                }
            }
        }
        if (players[currentPlayer.Value].GetComponent<NetworkPlayer>().playerLost.Value)
        {
            currentPlayer.Value = (currentPlayer.Value + 1) % numPlayers;
        }

        int safeCounter = 0;
        while (safeCounter < numPlayers)
        {
            var player = players[currentPlayer.Value];
            var networkPlayer = player.GetComponent<NetworkPlayer>();

            if (networkPlayer != null && !networkPlayer.playerLost.Value)
            {
                StartTradeServerRpc(currentPlayer.Value);
                return;
            }

            currentPlayer.Value = (currentPlayer.Value + 1) % numPlayers;
            safeCounter++;
        }

        Debug.LogWarning("[TurnManager] No active players available to start turn.");
    }

    [ServerRpc(RequireOwnership = false)]
    public void StartTradeServerRpc(int playerIndex)
    {
        if (tradeInProgress) return;
        tradeInProgress = true;

        if (playerIndex < 0 || playerIndex >= numPlayers) return;

        var player = players[playerIndex];
        var networkPlayer = player.GetComponent<NetworkPlayer>();

        if (networkPlayer == null || networkPlayer.playerLost.Value)
        {
            Debug.Log("[TurnManager] Skipping trade for player who lost.");
            EndTurnDirectly(playerIndex);
            return;
        }

        if (currentPlayer.Value != playerIndex) return;

        Trade trade = tradeManager.GetRandomTrade();
        HiddenCard hiddenCard = HiddenCardManager.Instance.GetRandomHiddenCard();

        scalePlayerClientRpc(false, playerIndex);
        currentTrade.Value = trade.id;

        StartCoroutine(TradeCoroutine(playerIndex, trade, hiddenCard));
    }

    private void EndTurnDirectly(int playerIndex)
    {
        currentPlayer.Value = (currentPlayer.Value + 1) % numPlayers;
        currentTurn.Value++;
        currentTrade.Value = -1;
        voteManager.voteDone.Value = false;
        StartTurnServerRpc();
    }

    [ClientRpc]
    private void scalePlayerClientRpc(bool tradeEnd, int playerIndex)
    {
        if (IsServer && !IsClient) return;
        //Vector3 currScale = players[playerIndex].transform.localScale;
        //players[playerIndex].transform.localScale = tradeEnd ? new Vector3(currScale.x / 2f, currScale.y / 2f, 1f)
        //                                                     : new Vector3(currScale.x * 2f, currScale.y * 2f, 1f);

        var allPlayers = FindObjectsByType<NetworkPlayer>(FindObjectsSortMode.None);
        foreach (var player in allPlayers)
        {
            if (player.playerIndex.Value == playerIndex)
            {
                var tr = player.transform;
                Vector3 currScale = tr.localScale;
                tr.localScale = new Vector3(currScale.x * 2f, currScale.y * 2f, 1f);
                break;
            }
        }
    }

    [ClientRpc]
    private void revertScaleClientRpc(int playerIndex)
    {
        if (IsServer && !IsClient) return;

        // Revert scale for the player whose turn ended
        var allPlayers = FindObjectsByType<NetworkPlayer>(FindObjectsSortMode.None);
        foreach (var player in allPlayers)
        {
            if (player.playerIndex.Value == playerIndex)
            {
                var tr = player.transform;
                Vector3 currScale = tr.localScale;
                tr.localScale = new Vector3(currScale.x / 2f, currScale.y / 2f, 1f);
                break;
            }
        }
    }

    private IEnumerator TradeCoroutine(int playerIndex, Trade trade, HiddenCard hiddenCard)
    {
        voteManager.yesVotes.Value = 0;
        voteManager.noVotes.Value = 0;

        yield return new WaitForSeconds(2f);

        if (playerIndex < clientIds.Length)
        {
            tradeDisplay.DisplayTradeClientRpc(clientIds[playerIndex], trade);
            if (hiddenCard != null)
            {
                tradeDisplay.DisplayHiddenCardClientRpc(clientIds[playerIndex], hiddenCard);
            }
        }

        yield return new WaitForSeconds(3f);

        var player = players[playerIndex];
        var networkPlayer = player.GetComponent<NetworkPlayer>();
        if (networkPlayer != null)
        {
            voteManager.DisplayVoteButtonsServerRpc();
        }

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
                audioManager.StopHeartbeatSound();
                break;
            }
            else if (elapsed >= 40.0f)
            {
                // Play heartbeat sound if the player is running out of time
                // but only for the player whose turn it is
                if (audioManager == null)
                {
                    Debug.LogError("[TurnManager] AudioManager instance not found.");
                }
                else if (NetworkManager.Singleton.LocalClientId == clientIds[playerIndex])
                {
                    Debug.Log("[TurnManager] Playing heartbeat sound for player " + playerIndex);
                    audioManager.PlayHeartbeatSound();
                }
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

        PlayGavelHitSoundClientRpc();

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
            return;
        }
        int[] playerNo = new int[voteManager.playerNo.Count];
        for (int i = 0; i < voteManager.playerNo.Count; i++)
        {
            playerNo[i] = voteManager.playerNo[i];
        }
        if ((type == "against-all") && playerNo.Length == 0)
        {
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
            timerText.text = Mathf.CeilToInt(newValue) + "s";
        }
    }

    [ClientRpc]
    private void PlayGavelHitSoundClientRpc()
    {
        if (audioManager != null)
        {
            audioManager.PlayGavelHitSound();
        }
        else
        {
            Debug.LogError("[TurnManager] AudioManager instance not found.");
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void UpdatePlayersAliveServerRpc()
    {
        if (IsServer)
        {
            int playersInGame = 0;
            // Keep updating players array every turn, to prevent players who lost from being included in the turn order
            for (int i = 0; i < numPlayers; i++)
            {
                NetworkPlayer player = players[i].GetComponent<NetworkPlayer>();
                if (!player.playerLost.Value)
                {
                    playersInGame++;
                }
            }

            voteManager.votingPlayers.Value = playersInGame;
        }
    }

}

