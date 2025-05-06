using UnityEngine;
using Unity.Netcode;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;

public class VoteManager : NetworkBehaviour
{
    NetworkVariable<int> yesVotes = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone);
    NetworkVariable<int> noVotes = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone);

    public Button yesButton;
    public Button noButton;

    private GameObject audioManagerObject;
    private AudioManager audioManager;

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
}
