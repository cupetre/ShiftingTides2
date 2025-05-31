using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Netcode;
using UnityEngine.SceneManagement;
using System.Collections;

public class ScreenTransition : NetworkBehaviour
{
    [Header("Referências de UI")]
    [SerializeField] private Image transitionImage;
    [SerializeField] private TextMeshProUGUI transitionText;
    [SerializeField] private float returnToMenuDelay = 10f;

    private int localPlayerIndex = -1;

    private void Awake()
    {
        transitionImage.gameObject.SetActive(false);
        transitionText.gameObject.SetActive(false);
        if (transitionImage != null) transitionImage.gameObject.SetActive(false);
        if (transitionText != null) transitionText.gameObject.SetActive(false);
    }

    private void Start()
    {
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

        if (localPlayerIndex == targetPlayerIndex)
        {
            transitionText.text = "YOU LOST";
        }
        else
        {
            transitionText.text = $"Player {targetPlayerIndex + 1} lost";
        }

        transitionImage.gameObject.SetActive(true);
        transitionText.gameObject.SetActive(true);

        yield return new WaitForSeconds(3f);

        transitionImage.gameObject.SetActive(false);
        transitionText.gameObject.SetActive(false);

        if (localPlayerIndex == targetPlayerIndex)
        {
            yield return ReturnToMenu();
        }
    }

    [ClientRpc]
    public void SetPlayerWonClientRpc(int targetPlayerIndex)
    {
        Debug.Log($"[ScreenTransition] SetPlayerWonClientRpc chamado no cliente {localPlayerIndex}. targetPlayerIndex = {targetPlayerIndex}");
        StartCoroutine(HandlePlayerWon(targetPlayerIndex));
    }

    private IEnumerator HandlePlayerWon(int targetPlayerIndex)
    {
        if (transitionText == null || transitionImage == null)
        {
            Debug.LogError("[ScreenTransition] transitionText ou transitionImage não atribuído no Inspector!");
            yield break;
        }

        if (localPlayerIndex == targetPlayerIndex)
        {
            transitionText.text = "YOU WON!";
        }
        else
        {
            transitionText.text = $"Player {targetPlayerIndex + 1} won the game";
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
