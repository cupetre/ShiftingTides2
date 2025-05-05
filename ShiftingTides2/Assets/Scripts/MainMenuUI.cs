using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MainMenuUI : MonoBehaviour
{
    [SerializeField] private Button hostButton;
    [SerializeField] private Button clientButton;
    [SerializeField] private TMP_Text statusText;

    private void Start()
    {
        hostButton.onClick.AddListener(StartAsHost);
        clientButton.onClick.AddListener(StartAsClient);
    }

    private void StartAsHost()
    {
        NetworkManager.Singleton.StartHost();
        statusText.text = "Hosting...";
    }

    private void StartAsClient()
    {
        NetworkManager.Singleton.StartClient();
        statusText.text = "Connecting as Client...";
    }
}
