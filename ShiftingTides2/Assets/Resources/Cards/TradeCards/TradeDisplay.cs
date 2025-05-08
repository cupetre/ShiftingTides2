using UnityEngine;
using TMPro;
using System.Collections;
using Unity.Netcode;
using UnityEngine.SceneManagement;

public class TradeDisplay : NetworkBehaviour
{

    public TMP_Text tradeDescription;

    [SerializeField] private GameObject tradeCard;
    private GameObject playerObject;
    private NetworkPlayer networkPlayer;
    private ulong clientId;
    private int playerIndex;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Start()
    {
        tradeCard.SetActive(false);
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
    public void displayTradeClientRpc(ulong targetClientId, Trade assignedTrade)
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

        // Set the trade title and description
        // tradeTitle.text = assignedTrade.title;
        tradeCard.SetActive(true);
        tradeDescription.text = assignedTrade.description;

        //HiddenCard assignedHidden = HiddenCardManager.Instance.GetRandomHiddenCard();

        //if(assignedHidden != null) {
        //    tradeDescription.text += "...But" + assignedHidden.description;
        //}

        Debug.Log($"[TradeDisplayManager] Trade Display initialized for player {playerIndex} with trade {assignedTrade}");
    }
}
