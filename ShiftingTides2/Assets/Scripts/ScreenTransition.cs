using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;
using UnityEngine.SceneManagement;
using Unity.Netcode;

public class ScreenTransition : NetworkBehaviour
{
    [SerializeField] private Image transitionImage;
    [SerializeField] private TextMeshProUGUI transitionText;
    [SerializeField] private float fadeDuration = 1.0f;
    [SerializeField] private float fadeOutDuration = 2.0f;
    
    private NetworkPlayer localPlayer;

    private void Awake()
    {
        if (transitionImage != null && transitionText != null)
        {
            transitionImage.gameObject.SetActive(false);
            transitionText.gameObject.SetActive(false);
        }
    }

    private void Start()
    {
        // Encontra o jogador local
        localPlayer = NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<NetworkPlayer>();
    }

    public void SetPlayerLost(bool lost, int targetPlayerIndex)
    {
        // Só mostra a mensagem se for o jogador que perdeu
        if (localPlayer.playerIndex.Value == targetPlayerIndex)
        {
            int displayIndex = targetPlayerIndex + 1;
            transitionText.text = $"Player {displayIndex} lost";
            StartCoroutine(FadeInCoroutine());
            StartCoroutine(LeaveGame());
        }
        else
        {
            // Para outros jogadores, mostra mensagem diferente
            int displayIndex = targetPlayerIndex + 1;
            transitionText.text = $"Player {displayIndex} was eliminated";
            StartCoroutine(ShowTemporaryMessage());
        }
    }

    public void SetPlayerWon(int targetPlayerIndex) 
    {
        // Só mostra vitória se for o jogador que ganhou
        if (localPlayer.playerIndex.Value == targetPlayerIndex)
        {
            int displayIndex = targetPlayerIndex + 1;
            transitionText.text = $"Player {displayIndex} won!";
            StartCoroutine(FadeInCoroutine());
            StartCoroutine(LeaveGame());
        }
        else
        {
            // Para outros jogadores
            int displayIndex = targetPlayerIndex + 1;
            transitionText.text = $"Player {displayIndex} won the game";
            StartCoroutine(ShowTemporaryMessage());
        }
    }

    private IEnumerator ShowTemporaryMessage()
    {
        yield return StartCoroutine(FadeInCoroutine());
        yield return new WaitForSeconds(3f); // Mostra a mensagem por 3 segundos
        yield return StartCoroutine(FadeOutCoroutine());
    }

    private IEnumerator FadeInCoroutine()
    {
        transitionImage.gameObject.SetActive(true);
        transitionText.gameObject.SetActive(true);
        
        Color colorImage = transitionImage.color;
        Color colorText = transitionText.color;

        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            colorImage.a = Mathf.Lerp(0f, 1f, t / fadeDuration);
            colorText.a = Mathf.Lerp(0f, 1f, t / fadeDuration);
            transitionImage.color = colorImage;
            transitionText.color = colorText;
            yield return null;
        }
    }

    private IEnumerator FadeOutCoroutine()
    {
        Color colorImage = transitionImage.color;
        Color colorText = transitionText.color;

        float t = 0f;
        while (t < fadeOutDuration)
        {
            t += Time.deltaTime;
            colorImage.a = Mathf.Lerp(1f, 0f, t / fadeOutDuration);
            colorText.a = Mathf.Lerp(1f, 0f, t / fadeOutDuration);
            transitionImage.color = colorImage;
            transitionText.color = colorText;
            yield return null;
        }

        transitionImage.gameObject.SetActive(false);
        transitionText.gameObject.SetActive(false);
    }

    private IEnumerator LeaveGame()
    {
        yield return new WaitForSeconds(10f);
        NetworkManager.Singleton.Shutdown();
        SceneManager.LoadScene("MainMenuScene");
    }
}