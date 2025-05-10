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
    [SerializeField] private float fadeOutDuration = 2.0f;  // Apenas para os outros jogadores
    private bool isPlayerLost = false;
    private void Awake()
    {
        if (transitionImage != null && transitionText != null)
            transitionImage.gameObject.SetActive(false);
        transitionText.gameObject.SetActive(false);
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

        colorImage.a = 1f;
        colorText.a = 1f;
        transitionImage.color = colorImage;
        transitionText.color = colorText;

        StartCoroutine(LeaveGame());
    }

    private IEnumerator FadeOutCoroutine()
    {
        float t = 0f;
        Color colorImage = transitionImage.color;
        Color colorText = transitionText.color;

        while (t < fadeOutDuration)
        {
            t += Time.deltaTime;
            colorImage.a = Mathf.Lerp(1f, 0f, t / fadeOutDuration);
            colorText.a = Mathf.Lerp(1f, 0f, t / fadeOutDuration);
            transitionImage.color = colorImage;
            transitionText.color = colorText;
            yield return null;
        }

        colorImage.a = 0f;
        colorText.a = 0f;
        transitionImage.color = colorImage;
        transitionText.color = colorText;
        transitionImage.gameObject.SetActive(false);
        transitionText.gameObject.SetActive(false);
    }

    private IEnumerator LeaveGame()
    {
        // Wait 10 seconds before leaving the game
        yield return new WaitForSeconds(10f);
        NetworkManager.Singleton.Shutdown();
        // Load the Main Menu Scene
        SceneManager.LoadScene("MainMenuScene");
    }

    public void SetPlayerLost(bool lost, int targetPlayerIndex)
    {
        isPlayerLost = lost;
        int playerIndex = targetPlayerIndex+1;
        transitionText.text = "Player " + playerIndex + "lost";
        if (!isPlayerLost) transitionText.text = "Player " + targetPlayerIndex + 1 + " won";
        if (transitionImage != null && transitionText != null) StartCoroutine(FadeInCoroutine());
    }
}
