using Unity.Netcode;
using UnityEngine;

public class ConnectionManager : MonoBehaviour
{
    private void OnEnable()
    {
        NetworkManager.Singleton.OnClientConnectedCallback += HandleClientConnected;
    }

    private void OnDisable()
    {
        NetworkManager.Singleton.OnClientConnectedCallback -= HandleClientConnected;
    }

    private void HandleClientConnected(ulong clientId)
    {
        Debug.Log($"Client {clientId} connected. Total: {NetworkManager.Singleton.ConnectedClients.Count}");
    }
}
