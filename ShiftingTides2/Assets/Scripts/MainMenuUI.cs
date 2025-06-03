using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MainMenuUI : MonoBehaviour
{
    [SerializeField] private Button hostButton;
    [SerializeField] private Button clientButton;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private TMP_InputField nameInputField;
    public static string PlayerName { get; private set; }

    private void Start()
    {
        hostButton.onClick.AddListener(StartAsHost);
        clientButton.onClick.AddListener(StartAsClient);
    }

    private void StartAsHost()
    {
        SetPlayerName();
        NetworkManager.Singleton.StartHost();
        statusText.text = "Hosting...";
    }

    private void StartAsClient()
    {
        SetPlayerName();
        NetworkManager.Singleton.StartClient();
        statusText.text = "Connecting as Client...";
    }


    private void SetPlayerName()
    {
        PlayerName = string.IsNullOrWhiteSpace(nameInputField.text)
        ? "Player" + Random.Range(1000, 9999)
        : nameInputField.text;

         Debug.Log($"[MainMenuUI] Set player name to: {PlayerName}");
    }
}
