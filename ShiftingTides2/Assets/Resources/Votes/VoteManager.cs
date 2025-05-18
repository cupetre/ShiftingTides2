using UnityEngine;
using Unity.Netcode;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;

public class VoteManager : NetworkBehaviour
{
    public NetworkVariable<int> yesVotes = new NetworkVariable<int>(0);
    public NetworkVariable<int> noVotes = new NetworkVariable<int>(0);
    public NetworkVariable<bool> voteDone = new NetworkVariable<bool>(false);

    public NetworkList<int> playerYes;
    public NetworkList<int> playerNo;

    public Button yesButton;
    public Button noButton;
    private AudioManager audioManager;

    private void Awake()
    {

        playerYes = new NetworkList<int>();
        playerNo = new NetworkList<int>();
    }

    void Start()
    {
        audioManager = FindFirstObjectByType<AudioManager>();
        yesButton.onClick.AddListener(VoteYes);
        noButton.onClick.AddListener(VoteNo);

        yesVotes.OnValueChanged += OnYesVotesChanged;
        noVotes.OnValueChanged += OnNoVotesChanged;
    }

    void Update()
    {
        if ((yesVotes.Value + noVotes.Value >= 3) && !voteDone.Value)
        {
            voteDone.Value = true;
            yesButton.gameObject.SetActive(false);
            noButton.gameObject.SetActive(false);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void CastVoteServerRpc(bool vote, ServerRpcParams rpcParams = default)
    {
        if (voteDone.Value) return;

        ulong clientId = rpcParams.Receive.SenderClientId;

        // Obtém o playerIndex da mesma forma que você faz normalmente
        NetworkObject playerObject = NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject;
        if (playerObject == null)
        {
            Debug.LogError("[VoteManager] Player object not found for clientId: " + clientId);
            return;
        }

        NetworkPlayer networkPlayer = playerObject.GetComponent<NetworkPlayer>();
        if (networkPlayer == null)
        {
            Debug.LogError("[VoteManager] NetworkPlayer component not found");
            return;
        }

        int playerIndex = networkPlayer.playerIndex.Value;

        if (vote)
        {
            yesVotes.Value++;
            if (!playerYes.Contains(playerIndex))
            {
                playerYes.Add(playerIndex);
            }
            Debug.Log($"[VoteManager] Player {playerIndex} (Client: {clientId}) voted YES");
        }
        else
        {
            noVotes.Value++;
            if (!playerNo.Contains(playerIndex))
            {
                playerNo.Add(playerIndex);
            }
            Debug.Log($"[VoteManager] Player {playerIndex} (Client: {clientId}) voted NO");
        }

        networkPlayer.emotionState.Value = vote ? EmotionState.Happy : EmotionState.Angry;
        HideVoteButtonsClientRpc(clientId);
    }

    private void VoteYes()
    {
        if (audioManager != null)
        {
            audioManager.PlayCorrIncorrSound(true);
        }
        CastVoteServerRpc(true);
    }

    private void VoteNo()
    {
        if (audioManager != null)
        {
            audioManager.PlayCorrIncorrSound(false);
        }
        CastVoteServerRpc(false);
    }

    [ServerRpc(RequireOwnership = false)]
    public void DisplayVoteButtonsServerRpc()
    {

        yesVotes.Value = 0;
        noVotes.Value = 0;
        voteDone.Value = false;
        playerYes.Clear();
        playerNo.Clear();

        DisplayVoteButtonsClientRpc();
    }

    [ClientRpc]
    public void DisplayVoteButtonsClientRpc()
    {
        yesButton.gameObject.SetActive(true);
        noButton.gameObject.SetActive(true);
    }

    [ClientRpc]
    public void HideVoteButtonsClientRpc(ulong clientId)
    {
        if (NetworkManager.Singleton.LocalClientId == clientId)
        {
            yesButton.gameObject.SetActive(false);
            noButton.gameObject.SetActive(false);
        }
    }
    [ClientRpc]
    public void HideVoteButtonsClientRpc()
    {
        yesButton.gameObject.SetActive(false);
        noButton.gameObject.SetActive(false);
    }

    private void OnYesVotesChanged(int previousValue, int newValue)
    {
        Debug.Log($"Yes votes changed from {previousValue} to {newValue}");
    }

    private void OnNoVotesChanged(int previousValue, int newValue)
    {
        Debug.Log($"No votes changed from {previousValue} to {newValue}");
    }

    private void OnDestroy()
    {
        yesVotes.OnValueChanged -= OnYesVotesChanged;
        noVotes.OnValueChanged -= OnNoVotesChanged;
    }
}