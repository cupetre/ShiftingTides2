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
        if (IsOwner)
        {
            audioManager = FindFirstObjectByType<AudioManager>();
            yesButton.onClick.AddListener(VoteYes);
            noButton.onClick.AddListener(VoteNo);
        }

        yesVotes.OnValueChanged += OnYesVotesChanged;
        noVotes.OnValueChanged += OnNoVotesChanged;
    }

    void FixedUpdate()
    {
        if (voteDone.Value)
        {
            yesButton.gameObject.SetActive(false);
            noButton.gameObject.SetActive(false);

            yesVotes.Value = -1;
            noVotes.Value = -1;

            voteDone.Value = false;
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
        }
        else
        {
            noVotes.Value++;
        }
    }

    private void VoteYes()
    {
        if (audioManager != null)
        {
            audioManager.PlayCorrIncorrSound(true);
        }
        CastVoteServerRpc(true);
        // Add player index
        GameManager gameManager = FindFirstObjectByType<GameManager>();
        if (gameManager != null)
        {
            int playerIndex = gameManager.clientIds.Value.IndexOf(NetworkManager.Singleton.LocalClientId);
            if (playerIndex >= 0 && playerIndex < 4)
            {
                playerYes.Add(new NetworkVariable<int>(playerIndex, NetworkVariableReadPermission.Everyone));
            }
        }

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

        // Add player index
        GameManager gameManager = FindFirstObjectByType<GameManager>();
        if (gameManager != null)
        {
            int playerIndex = gameManager.clientIds.Value.IndexOf(NetworkManager.Singleton.LocalClientId);
            if (playerIndex >= 0 && playerIndex < 4)
            {
                playerNo.Add(new NetworkVariable<int>(playerIndex, NetworkVariableReadPermission.Everyone));
            }
        }

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
        if (IsOwner)
        {
            yesButton.gameObject.SetActive(true);
            noButton.gameObject.SetActive(true);
        }
    }
}
