using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : NetworkBehaviour
{
    public static GameManager Instance;

    private int currentPlayerIndex = 0;

    [SerializeField] private int maxPlayers = 4;
 
    private void Awake()
    {
        if (Instance == null) 
            Instance = this;
    }

    private void Start()
    {
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
    }

    public int AssignPlayerIndex()
    {
        int assignedIndex = currentPlayerIndex;
        currentPlayerIndex++;
        return assignedIndex;
    }

private void OnClientConnected(ulong clientId)
{
    Debug.Log($"[GameManager] Player {clientId} connected. Total: {NetworkManager.Singleton.ConnectedClients.Count}");

    if (IsServer)
    {
        // Assign a unique index to the connecting player
        int playerIndex = AssignPlayerIndex();

        // Get the NetworkPlayer component of the newly connected client
        if (NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out NetworkClient client))
        {
            if (client.PlayerObject != null)
            {
                var networkPlayer = client.PlayerObject.GetComponent<NetworkPlayer>();
                if (networkPlayer != null)
                {
                    // Inform the client about their assigned index (using the public method)
                    networkPlayer.SetPlayerId(playerIndex);
                }
                else
                {
                    Debug.LogError($"[GameManager] NetworkPlayer component not found on PlayerObject for client: {clientId}");
                }
            }
            else
            {
                Debug.LogError($"[GameManager] PlayerObject is null for client: {clientId}");
            }
        }
        else
        {
            Debug.LogError($"[GameManager] Connected client not found: {clientId}");
        }

        // Check if all players are connected and load the next scene
        if (NetworkManager.Singleton.ConnectedClients.Count == maxPlayers)
        {
            Debug.Log("[GameManager] All players connected. Loading GameStartScene...");
            NetworkManager.Singleton.SceneManager.LoadScene("GameStartScene", LoadSceneMode.Single);
        }
    }
}

}
