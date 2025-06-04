using UnityEngine;
using TMPro;
using System.Collections;
using Unity.Netcode;
using UnityEngine.SceneManagement;
using TMPro.EditorUtilities;
using UnityEngine.UI;

public class TradeDisplay : NetworkBehaviour
{
    public RawImage imgForAnim;
    public TMP_Text txtForAnim;

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

        StartCoroutine(UIChange());
    }

    private IEnumerator UIChange()
    {
        float timePassed = 0f;
        float duration = 2.5f; // Duration of the transition in seconds
        while (timePassed < duration)
        {
            timePassed += Time.deltaTime;
            // We want to change the alpha of the imgForAnim
            float alpha = Mathf.Lerp(0f, 0.8f, timePassed / duration);
            // Set the alpha of the imgForAnim
            if (imgForAnim != null)
            {
                imgForAnim.color = new Color(imgForAnim.color.r, imgForAnim.color.g, imgForAnim.color.b, alpha);
                txtForAnim.gameObject.SetActive(true);
            }
            else
            {
                Debug.LogError("[TradeDisplayManager] tradePanel is not assigned in the Inspector.");
            }
            yield return null; // Wait for the next frame

            // Check if the duration has passed
            if (timePassed >= duration)
            {
                // Reset the alpha to 0 after the transition
                if (imgForAnim != null)
                {
                    imgForAnim.color = new Color(imgForAnim.color.r, imgForAnim.color.g, imgForAnim.color.b, 0f);
                    txtForAnim.gameObject.SetActive(false);
                }
                else
                {
                    Debug.LogError("[TradeDisplayManager] tradePanel is not assigned in the Inspector.");
                }
            }
        }
    }
}
