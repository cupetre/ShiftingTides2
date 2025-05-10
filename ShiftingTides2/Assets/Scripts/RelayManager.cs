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

    async void Start()
    {
        await UnityServices.InitializeAsync();
        await AuthenticationService.Instance.SignInAnonymouslyAsync();

        hostButton.onClick.AddListener(async () => {
            var allocation = await RelayService.Instance.CreateAllocationAsync(3);
            var joinCode   = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
            codeText.text  = $"Code: {joinCode}";

            // Converte para RelayServerData
            var relayData = AllocationUtils.ToRelayServerData(allocation, "dtls");
            NetworkManager.Singleton.GetComponent<UnityTransport>()
                         .SetRelayServerData(relayData);
            NetworkManager.Singleton.StartHost();
        });

        joinButton.onClick.AddListener(async () => {
            var allocation = await RelayService.Instance.JoinAllocationAsync(joinInput.text);
            var relayData  = AllocationUtils.ToRelayServerData(allocation, "dtls");
            NetworkManager.Singleton.GetComponent<UnityTransport>()
                         .SetRelayServerData(relayData);
            NetworkManager.Singleton.StartClient();
        });
    }
}
