using UnityEngine;
using Unity.Netcode;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;

public class VoteManager : NetworkBehaviour
{
    public NetworkVariable<int> yesVotes = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone);
    public NetworkVariable<int> noVotes = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone);

    public List<NetworkVariable<int>> playerYes = new List<NetworkVariable<int>>();
    public List<NetworkVariable<int>> playerNo = new List<NetworkVariable<int>>();

    public Button yesButton;
    public Button noButton;

    private GameObject audioManagerObject;
    private AudioManager audioManager;

    public NetworkVariable<bool> voteDone = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone);

    void Start()
    {
        audioManager = FindFirstObjectByType<AudioManager>();
        yesButton.onClick.AddListener(VoteYes);
        noButton.onClick.AddListener(VoteNo);

        yesVotes.OnValueChanged += OnYesVotesChanged;
        noVotes.OnValueChanged += OnNoVotesChanged;
    }

    void FixedUpdate()
    {
        if ((yesVotes.Value + noVotes.Value >= 3) && !voteDone.Value)
        {
            voteDone.Value = true;
            yesButton.gameObject.SetActive(false);
            noButton.gameObject.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        yesVotes.OnValueChanged -= OnYesVotesChanged;
        noVotes.OnValueChanged -= OnNoVotesChanged;
    }

    [ServerRpc(RequireOwnership = false)]
    public void CastVoteServerRpc(bool vote)
    {
        if (vote)
        {
            yesVotes.Value++;
            // Add player index not clientId
            ulong clientId = NetworkManager.Singleton.LocalClient.ClientId;
            // Find player index through player objects on network
            GameManager gameManager = FindFirstObjectByType<GameManager>();
            int playerIndex = gameManager.clientIds.Value.IndexOf(clientId);
            if (playerIndex >= 0 && playerIndex < 4)
            {
                playerYes.Add(new NetworkVariable<int>(playerIndex, NetworkVariableReadPermission.Everyone));
                Debug.Log($"[VoteManager] Player {playerIndex} voted YES");
            }
        }
        else
        {
            noVotes.Value++;
            ulong clientId = NetworkManager.Singleton.LocalClient.ClientId;
            GameManager gameManager = FindFirstObjectByType<GameManager>();
            int playerIndex = gameManager.clientIds.Value.IndexOf(clientId);
            if (playerIndex >= 0 && playerIndex < 4)
            {
                playerNo.Add(new NetworkVariable<int>(playerIndex, NetworkVariableReadPermission.Everyone));
                Debug.Log($"[VoteManager] Player {playerIndex} voted NO");
            }
        }
    }

    private void VoteYes()
    {
        if (audioManager != null)
        {
            audioManager.PlayCorrIncorrSound(true);
        }
        CastVoteServerRpc(true);

        yesButton.gameObject.SetActive(false);
        noButton.gameObject.SetActive(false);
    }

    private void VoteNo()
    {
        if (audioManager != null)
        {
            audioManager.PlayCorrIncorrSound(false);
        }
        CastVoteServerRpc(false);

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

    // Function to display vote buttons
    [ServerRpc(RequireOwnership = false)]
    public void DisplayVoteButtonsServerRpc()
    {
        DisplayVoteButtonsClientRpc();
    }

    [ClientRpc]
    public void DisplayVoteButtonsClientRpc()
    {
        // Show buttons to all clients
        yesButton.gameObject.SetActive(true);
        noButton.gameObject.SetActive(true);
    }

    [ClientRpc]
    public void HideVoteButtonsClientRpc(ulong clientId)
    {
        // Hide buttons for the client who voted
        if (NetworkManager.Singleton.LocalClientId == clientId)
        {
            yesButton.gameObject.SetActive(false);
            noButton.gameObject.SetActive(false);
        }
    }
}
