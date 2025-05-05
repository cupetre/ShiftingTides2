using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class ConnectionManager : MonoBehaviour
{
    private const int maxPlayers = 4;

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
        int connectedCount = NetworkManager.Singleton.ConnectedClients.Count;
        Debug.Log($"Client connected. Total: {connectedCount}");

        if (NetworkManager.Singleton.IsHost && connectedCount == maxPlayers)
        {
            Debug.Log("All players connected. Loading GameStartScene...");
            NetworkManager.Singleton.SceneManager.LoadScene("GameStartScene", LoadSceneMode.Single);
        }
    }
}
