using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;

public class RelayManager : MonoBehaviour
{
    [SerializeField] private Button hostButton;
    [SerializeField] private Button joinButton;
    [SerializeField] private TMP_InputField joinInput;
    [SerializeField] private TextMeshProUGUI codeText;
    [SerializeField] private TMP_InputField nameInputField;
    [SerializeField] private TMP_Text playerNameText;
    [SerializeField] private TMP_Text connectedNumText;

    private string joinCode;                                   // <-- agora string simples
    public static string PlayerName { get; private set; }

    async void Start()
    {
        await UnityServices.InitializeAsync();
        await AuthenticationService.Instance.SignInAnonymouslyAsync();

        connectedNumText.gameObject.SetActive(false);

        hostButton.onClick.AddListener(async () =>
        {
            PlayerName = nameInputField.text.Trim();

            var allocation = await RelayService.Instance.CreateAllocationAsync(3);
            joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
            codeText.text = $"Code: {joinCode}";               // mostra de imediato

            var relayData = AllocationUtils.ToRelayServerData(allocation, "dtls");
            NetworkManager.Singleton.GetComponent<UnityTransport>()
                                     .SetRelayServerData(relayData);
            NetworkManager.Singleton.StartHost();

            ToggleLobbyUI(false);
        });

        joinButton.onClick.AddListener(async () =>
        {
            PlayerName = nameInputField.text.Trim();

            var allocation = await RelayService.Instance.JoinAllocationAsync(joinInput.text.Trim());
            var relayData = AllocationUtils.ToRelayServerData(allocation, "dtls");
            NetworkManager.Singleton.GetComponent<UnityTransport>()
                                     .SetRelayServerData(relayData);
            NetworkManager.Singleton.StartClient();

            codeText.text = $"Code: {joinInput.text.Trim()}"; // usa o código digitado
            ToggleLobbyUI(false);
        });
    }

    private void ToggleLobbyUI(bool show)
    {
        hostButton.gameObject.SetActive(show);
        joinButton.gameObject.SetActive(show);
        joinInput.gameObject.SetActive(show);
        nameInputField.gameObject.SetActive(show);
        playerNameText.text = string.Empty;
        connectedNumText.gameObject.SetActive(!show);
    }

    private void FixedUpdate()
    {
        if (NetworkManager.Singleton == null) return;
        if (NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsClient)
        {
            connectedNumText.text = $"Connected: {NetworkManager.Singleton.ConnectedClients.Count}/4";
        }
    }
}