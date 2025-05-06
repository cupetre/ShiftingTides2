using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Netcode;

public class GameStartSceneUI: NetworkBehaviour
{
    [Header("Action Buttons")]
    [SerializeField] private Button yesButton;
    [SerializeField] private Button noButton;

    [Header("Turn Display")]
    [SerializeField] private TMP_Text currentPlayerText;
    [SerializeField] private GameObject yourTurnIndicator; // Optional visual indicator

    [Header("Local Player Resources")]
    [SerializeField] private TMP_Text localMoneyText;
    [SerializeField] private TMP_Text localInfluenceText;
    [SerializeField] private TMP_Text localPeopleText;

    [Header("Game State")]
    [SerializeField] private TMP_Text roundNumberText;
    [SerializeField] private TMP_Text gameEventText;

    [Header("Vote Feedback")]
    [SerializeField] private TMP_Text voteFeedbackText;
    [SerializeField] private float voteFeedbackDuration = 2f;
    private float voteFeedbackTimer = 0f;

    [Header("End Game")]
    [SerializeField] private GameObject endGamePanel;
    [SerializeField] private TMP_Text winnerText;
    [SerializeField] private Button returnToMenuButton; // Assuming you have one

    private bool isVoteFeedbackVisible = false;

    public override void OnNetworkSpawn()
    {
        if (IsClient)
        {
            // Initialize UI based on initial game state (if available on spawn)
            UpdateLocalPlayerResourcesClientRpc(0, 0, 0); // Example initial values
            UpdateRoundDisplayClientRpc(0); // Example initial value
            HideActionButtonsClientRpc();
            HideEndGamePanelClientRpc();
            HideVoteFeedbackTextClientRpc();

            // Example: If you have an initial current player
            // UpdateCurrentPlayerDisplayClientRpc("Player Name");
        }
    }

    private void Update()
    {
        if (isVoteFeedbackVisible)
        {
            voteFeedbackTimer -= Time.deltaTime;
            if (voteFeedbackTimer <= 0f)
            {
                HideVoteFeedbackTextClientRpc();
            }
        }
    }

    [ClientRpc]
    public void SetActionButtonsVisibilityClientRpc(bool visible)
    {
        if (yesButton != null) yesButton.gameObject.SetActive(visible);
        if (noButton != null) noButton.gameObject.SetActive(visible);
    }

    [ClientRpc]
    public void UpdateCurrentPlayerDisplayClientRpc(string playerName)
    {
        if (currentPlayerText != null) currentPlayerText.text = $"Current Turn: {playerName}";
        if (yourTurnIndicator != null)
        {
            yourTurnIndicator.SetActive(playerName == NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<NetworkPlayer>().PlayerName.Value);
        }
    }

    [ClientRpc]
    public void UpdateLocalPlayerResourcesClientRpc(int money, int influence, int people)
    {
        if (localMoneyText != null) localMoneyText.text = $"Money: {money}";
        if (localInfluenceText != null) localInfluenceText.text = $"Influence: {influence}";
        if (localPeopleText != null) localPeopleText.text = $"People: {people}";
    }

    [ClientRpc]
    public void UpdateRoundDisplayClientRpc(int roundNumber)
    {
        if (roundNumberText != null) roundNumberText.text = $"Round: {roundNumber}";
    }

    [ClientRpc]
    public void ShowGameEventMessageClientRpc(string message)
    {
        if (gameEventText != null)
        {
            gameEventText.text = message;
            // Optionally add logic to show/hide the message after a delay
        }
    }

    [ClientRpc]
    public void ShowEndGamePanelClientRpc(string winner, string reason)
    {
        if (endGamePanel != null) endGamePanel.SetActive(true);
        if (winnerText != null) winnerText.text = $"Winner: {winner}\nReason: {reason}";
    }

    [ClientRpc]
    public void HideEndGamePanelClientRpc()
    {
        if (endGamePanel != null) endGamePanel.SetActive(false);
    }

    [ClientRpc]
    public void HideActionButtonsClientRpc()
    {
        if (yesButton != null) yesButton.gameObject.SetActive(false);
        if (noButton != null) noButton.gameObject.SetActive(false);
    }

    [ClientRpc]
    private void ShowVoteFeedbackTextClientRpc(string message)
    {
        if (voteFeedbackText != null)
        {
            voteFeedbackText.text = message;
            voteFeedbackText.gameObject.SetActive(true);
            isVoteFeedbackVisible = true;
            voteFeedbackTimer = voteFeedbackDuration;
        }
    }

    [ClientRpc]
    private void HideVoteFeedbackTextClientRpc()
    {
        if (voteFeedbackText != null)
        {
            voteFeedbackText.gameObject.SetActive(false);
            isVoteFeedbackVisible = false;
            voteFeedbackTimer = 0f;
        }
    }

    // These public methods will be called by the OnClick() event of the buttons in the Inspector
    public void OnYesButtonClicked()
    {
        ShowVoteFeedbackTextClientRpc("You pressed Yes!");
        // Send a Command to the server about the "yes" vote (you'll need a script on the player or a game controller for this)
        if (NetworkManager.Singleton.LocalClient.PlayerObject != null)
        {
            NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<YourVotingScript>()?.SubmitVoteServerRpc(true);
        }
    }

    public void OnNoButtonClicked()
    {
        ShowVoteFeedbackTextClientRpc("You pressed No!");
        // Send a Command to the server about the "no" vote (you'll need a script on the player or a game controller for this)
        if (NetworkManager.Singleton.LocalClient.PlayerObject != null)
        {
            NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<YourVotingScript>()?.SubmitVoteServerRpc(false);
        }
    }
}