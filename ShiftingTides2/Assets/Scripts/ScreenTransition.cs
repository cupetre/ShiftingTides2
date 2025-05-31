using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Netcode;
using UnityEngine.SceneManagement;
using System.Collections;


public class ScreenTransition : NetworkBehaviour
{
    [SerializeField] private Image transitionImage;
    [SerializeField] private TextMeshProUGUI transitionText;
    [SerializeField] private float returnToMenuDelay = 10f;

    private NetworkVariable<int> localPlayer;

    private void Awake()
    {
        if (transitionImage != null) transitionImage.gameObject.SetActive(false);
        if (transitionText != null) transitionText.gameObject.SetActive(false);
    }

    private void Start()
    {
        // Get the local player reference
        localPlayer.Value = NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<NetworkPlayer>().playerIndex.Value;
    }

    [ClientRpc]
    public void SetPlayerLostClientRpc(bool lost, int targetPlayerIndex)
    {
        StartCoroutine(HandlePlayerLost(lost, targetPlayerIndex));
    }

    private IEnumerator HandlePlayerLost(bool lost, int targetPlayerIndex)
    {
        if (transitionText == null || transitionImage == null) yield break;

        if (localPlayer.Value == targetPlayerIndex)
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

        yield return ReturnToMenu();
    }

    [ClientRpc]
    public void SetPlayerWonClientRpc(int targetPlayerIndex)
    {
        StartCoroutine(HandlePlayerWon(targetPlayerIndex));
    }

    private IEnumerator HandlePlayerWon(int targetPlayerIndex)
    {
        if (transitionText == null || transitionImage == null) yield break;

        if (localPlayer.Value == targetPlayerIndex)
        {
            transitionText.text = "YOU WON!";
        }
        else
        {
            transitionText.text = $"You suck, Player {targetPlayerIndex + 1} won the game";
        }

        transitionImage.gameObject.SetActive(true);
        transitionText.gameObject.SetActive(true);

        yield return new WaitForSeconds(3f);

        transitionImage.gameObject.SetActive(false);
        transitionText.gameObject.SetActive(false);

        yield return ReturnToMenu();
    }



    private System.Collections.IEnumerator ReturnToMenu()
    {
        yield return new WaitForSeconds(returnToMenuDelay);
        NetworkManager.Singleton.Shutdown();
        SceneManager.LoadScene("MainMenuScene");
    }

}
