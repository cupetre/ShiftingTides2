using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Netcode;
using UnityEngine.SceneManagement;

public class ScreenTransition : NetworkBehaviour
{
    [SerializeField] private Image transitionImage;
    [SerializeField] private TextMeshProUGUI transitionText;
    [SerializeField] private float returnToMenuDelay = 10f;

    private NetworkPlayer localPlayer;

    private void Awake()
    {
        if (transitionImage != null) transitionImage.gameObject.SetActive(false);
        if (transitionText != null) transitionText.gameObject.SetActive(false);
    }

    private void Start()
    {
        // Get the local player reference
        localPlayer = NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<NetworkPlayer>();
    }

    public void SetPlayerLost(bool lost, int targetPlayerIndex)
    {
        if (transitionText == null || transitionImage == null) return;

        if (localPlayer.playerIndex.Value == targetPlayerIndex)
        {
            transitionText.text = "YOU LOST";
        }
        else
        {
            transitionText.text = $"Player {targetPlayerIndex + 1} lost";
        }

        transitionImage.gameObject.SetActive(true);
        transitionText.gameObject.SetActive(true);

        StartCoroutine(ReturnToMenu());
    }

    public void SetPlayerWon(int targetPlayerIndex)
    {
        if (transitionText == null || transitionImage == null) return;

        if (localPlayer.playerIndex.Value == targetPlayerIndex)
        {
            transitionText.text = "YOU WON!";
        }
        else
        {
            transitionText.text = $"Player {targetPlayerIndex + 1} won the game";
        }

        transitionImage.gameObject.SetActive(true);
        transitionText.gameObject.SetActive(true);

        StartCoroutine(ReturnToMenu());
    }

    private System.Collections.IEnumerator ReturnToMenu()
    {
        yield return new WaitForSeconds(returnToMenuDelay);
        NetworkManager.Singleton.Shutdown();
        SceneManager.LoadScene("MainMenuScene");
    }
}
