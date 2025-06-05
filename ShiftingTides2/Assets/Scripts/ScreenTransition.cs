using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Netcode;
using UnityEngine.SceneManagement;
using System.Collections;
using Unity.Collections;

public class ScreenTransition : NetworkBehaviour
{
    [Header("Referências de UI")]
    [SerializeField] private Image transitionImage;
    [SerializeField] private TextMeshProUGUI transitionText;
    [SerializeField] private float returnToMenuDelay = 10f;

    private int localPlayerIndex = -1;

    public NetworkList<FixedString128Bytes> playerNames = new();

    private void Awake()
    {
        if (transitionImage != null) transitionImage.gameObject.SetActive(false);
        if (transitionText != null) transitionText.gameObject.SetActive(false);
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            playerNames.Clear();

            foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
            {
                var netPlayer = client.PlayerObject.GetComponent<NetworkPlayer>();
                if (netPlayer != null)
                {
                    playerNames.Add(netPlayer.playerName.Value);
                }
            }
        }

        if (IsClient && NetworkManager.Singleton.LocalClient != null)
        {
            var playerObj = NetworkManager.Singleton.LocalClient.PlayerObject;
            if (playerObj != null)
            {
                var netPlayer = playerObj.GetComponent<NetworkPlayer>();
                if (netPlayer != null)
                {
                    localPlayerIndex = netPlayer.playerIndex.Value;
                    Debug.Log($"[ScreenTransition] Cliente inicializado como Player {localPlayerIndex}");
                    return;
                }
            }

            Debug.LogWarning("[ScreenTransition] Não foi possível obter NetworkPlayer do LocalClient.");
        }
    }

    [ClientRpc]
    public void SetPlayerLostClientRpc(bool lost, int targetPlayerIndex)
    {
        Debug.Log($"[ScreenTransition] SetPlayerLostClientRpc chamado no cliente {localPlayerIndex}. targetPlayerIndex = {targetPlayerIndex}");
        StartCoroutine(HandlePlayerLost(lost, targetPlayerIndex));
    }

    private IEnumerator HandlePlayerLost(bool lost, int targetPlayerIndex)
    {
        if (transitionText == null || transitionImage == null)
        {
            Debug.LogError("[ScreenTransition] transitionText ou transitionImage não atribuído no Inspector!");
            yield break;
        }

        // Fallback seguro para evitar IndexOutOfRange
        string targetName = (targetPlayerIndex >= 0 && targetPlayerIndex < playerNames.Count)
            ? playerNames[targetPlayerIndex].ToString()
            : $"Player {targetPlayerIndex}";

        if (localPlayerIndex == targetPlayerIndex)
        {
            transitionText.text = "YOU LOST";
        }
        else
        {
            transitionText.text = $"{targetName} lost";
        }

        transitionImage.gameObject.SetActive(true);
        transitionText.gameObject.SetActive(true);

        yield return new WaitForSeconds(3f);

        transitionImage.gameObject.SetActive(false);
        transitionText.gameObject.SetActive(false);

    }

    [ClientRpc]
    public void SetPlayerWonClientRpc(int targetPlayerIndex)
    {
        Debug.Log($"[ScreenTransition] SetPlayerWonClientRpc chamado no cliente {localPlayerIndex}. targetPlayer = {targetPlayerIndex}");
        StartCoroutine(HandlePlayerWon(targetPlayerIndex));
    }

    private IEnumerator HandlePlayerWon(int targetPlayerIndex)
    {
        if (transitionText == null || transitionImage == null)
        {
            Debug.LogError("[ScreenTransition] transitionText ou transitionImage não atribuído no Inspector!");
            yield break;
        }

        // Fallback seguro para evitar IndexOutOfRange
        string targetName = (targetPlayerIndex >= 0 && targetPlayerIndex < playerNames.Count)
            ? playerNames[targetPlayerIndex].ToString()
            : $"Player {targetPlayerIndex}";

        if (localPlayerIndex == targetPlayerIndex)
        {
            transitionText.text = "YOU WON!";
        }
        else
        {
            transitionText.text = $"{targetName} won the game";
        }

        transitionImage.gameObject.SetActive(true);
        transitionText.gameObject.SetActive(true);

        yield return new WaitForSeconds(3f);

        transitionImage.gameObject.SetActive(false);
        transitionText.gameObject.SetActive(false);

        yield return ReturnToMenu();
    }

    private IEnumerator ReturnToMenu()
    {
        yield return new WaitForSeconds(returnToMenuDelay);
        SceneManager.LoadScene("MainMenuScene");
    }
}
