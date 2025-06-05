using TMPro;
using UnityEngine;
using UnityEngine.UI;

using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;

public class RelayManager : MonoBehaviour
{
    [SerializeField] Button hostButton;
    [SerializeField] Button joinButton;
    [SerializeField] TMP_InputField joinInput;
    [SerializeField] TextMeshProUGUI codeText;
    [SerializeField] TMP_InputField nameInputField;
    [SerializeField] TMP_Text playerNameText;
    [SerializeField] TMP_Text connectedNumText;

    public static string PlayerName { get; private set; }

    async void Start()
    {
        await UnityServices.InitializeAsync();
        await AuthenticationService.Instance.SignInAnonymouslyAsync();
        connectedNumText.gameObject.SetActive(false);

        hostButton.onClick.AddListener(async () =>
        {
            PlayerName = nameInputField.text;
            var allocation = await RelayService.Instance.CreateAllocationAsync(3);
            var joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
            codeText.text = $"Code: {joinCode}";

            // Converte para RelayServerData
            var relayData = AllocationUtils.ToRelayServerData(allocation, "dtls");
            NetworkManager.Singleton.GetComponent<UnityTransport>()
                         .SetRelayServerData(relayData);
            NetworkManager.Singleton.StartHost();

            hostButton.gameObject.SetActive(false);
            joinButton.gameObject.SetActive(false);
            joinInput.gameObject.SetActive(false);
            nameInputField.gameObject.SetActive(false);
            playerNameText.text = $"Player Name: {PlayerName}";
            connectedNumText.gameObject.SetActive(true);
        });

        joinButton.onClick.AddListener(async () =>
        {
            PlayerName = nameInputField.text;
            var allocation = await RelayService.Instance.JoinAllocationAsync(joinInput.text);
            var relayData = AllocationUtils.ToRelayServerData(allocation, "dtls");
            NetworkManager.Singleton.GetComponent<UnityTransport>()
                         .SetRelayServerData(relayData);
            NetworkManager.Singleton.StartClient();

            hostButton.gameObject.SetActive(false);
            joinButton.gameObject.SetActive(false);
            joinInput.gameObject.SetActive(false);
            nameInputField.gameObject.SetActive(false);
            playerNameText.text = $"Player Name: {PlayerName}";
            connectedNumText.gameObject.SetActive(true);
        });
    }

    private void FixedUpdate()
    {
        if (NetworkManager.Singleton.IsHost)
        {
            connectedNumText.text = $"Connected: {NetworkManager.Singleton.ConnectedClients.Count}/4";
        }
        else if (NetworkManager.Singleton.IsClient)
        {
            connectedNumText.text = $"Connected: {NetworkManager.Singleton.ConnectedClients.Count}/4";
        }
    }
}
