using UnityEngine;
using TMPro;
using System.Collections;
using Unity.Netcode;
using UnityEngine.SceneManagement;

public class TradeDisplay : NetworkBehaviour
{

    public TMP_Text tradeDescription;

    public TMP_Text hiddenDescription;
    [SerializeField] private GameObject tradeCard;
    [SerializeField] private GameObject hiddenCard;
    private GameObject playerObject;
    private NetworkPlayer networkPlayer;
    private ulong clientId;
    private int playerIndex;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Start()
    {
        tradeCard.SetActive(false);
        hiddenCard.SetActive(false);
        // Get the client ID and player index
        clientId = NetworkManager.Singleton.LocalClientId;

        // Find the player through the NetworkManager
        playerObject = NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject.gameObject;
        if (playerObject == null)
        {
            Debug.LogError("[TradeDisplayManager] Player object not found.");
            return;
        }
        // Get the player index from the NetworkPlayer component
        networkPlayer = playerObject.GetComponent<NetworkPlayer>();
        if (networkPlayer == null)
        {
            Debug.LogError("[TradeDisplayManager] NetworkPlayer component not found on player object.");
            return;
        }
        playerIndex = networkPlayer.playerIndex.Value;
    }

    [ClientRpc]
    public void DisplayTradeClientRpc(ulong targetClientId, Trade assignedTrade)
    {

        // Check if is the trade owner
        if (NetworkManager.Singleton.LocalClientId != targetClientId)
        {
            return;
        }
        // Check if trades are loaded
        if (TradeManager.Instance.trades == null || TradeManager.Instance.trades.Length == 0)
        {
            Debug.LogError("[TradeDisplayManager] No trades loaded. Cannot initialize trade display.");
            return;
        }

        // Check if the player index is valid
        if (playerIndex < 0 || playerIndex >= 4)
        {
            Debug.LogError($"[TradeDisplayManager] Invalid player index: {playerIndex}. Cannot initialize trade display.");
            return;
        }

        if (assignedTrade == null)
        {
            Debug.LogError($"[TradeDisplayManager] Invalid trade index: {assignedTrade}. Cannot initialize trade display.");
            return;
        }

        // Set the trade description
        tradeCard.SetActive(true);
        tradeDescription.text = assignedTrade.description;

        Debug.Log($"[TradeDisplayManager] Trade Display initialized for player {playerIndex} with trade {assignedTrade}");
    }

    [ClientRpc]
    public void DisplayHiddenCardClientRpc(ulong targetClientId, HiddenCard assignedHidden)
    {
        // Check if is the trade owner
        if (NetworkManager.Singleton.LocalClientId != targetClientId)
        {
            return;
        }
        // Check if trades are loaded
        if (HiddenCardManager.Instance.hidden == null || HiddenCardManager.Instance.hidden.Length == 0)
        {
            Debug.LogError("[TradeDisplayManager] No hidden cards loaded. Cannot initialize hidden cards display.");
            return;
        }

        // Check if the player index is valid
        if (playerIndex < 0 || playerIndex >= 4)
        {
            Debug.LogError($"[TradeDisplayManager] Invalid player index: {playerIndex}. Cannot initialize hidden cards display.");
            return;
        }

        if (assignedHidden == null)
        {
            Debug.Log($"[TradeDisplayManager] No hiddenCard: {assignedHidden}.");
            return;
        }

        // Set the trade description
        hiddenCard.SetActive(true);
        hiddenDescription.text = assignedHidden.description;

        Debug.Log($"[TradeDisplayManager] Trade Display initialized for player {playerIndex} with hidden card {assignedHidden}");
    }

    [ClientRpc]
    public void closeCardsClientRpc(Trade assignedTrade, HiddenCard assignedHidden)
    {
        tradeCard.SetActive(false);
        hiddenCard.SetActive(false);
    }

    [ClientRpc]
     public void revealCardsForAllClientRpc(Trade assignedTrade, HiddenCard assignedHidden)
    {
        tradeDescription.text = assignedTrade.description;
        hiddenDescription.text = assignedHidden.description;
        tradeCard.SetActive(true);
        hiddenCard.SetActive(true);
    }
}
