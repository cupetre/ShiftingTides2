using Unity.Netcode;
using UnityEngine;
using System.Linq;
using UnityEngine.SceneManagement;

public class GameManager : NetworkBehaviour
{
    public static GameManager Instance;

    [SerializeField] private int maxPlayers = 4;
    [SerializeField] private GameObject playerPrefab;
    private int currentPlayerIndex = 0;

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    private void Start()
    {
        
    }

    public override void OnNetworkSpawn()
    {
        // Keep only one subscription
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
    }

    public int AssignPlayerIndex()
    {
        return currentPlayerIndex++;
    }

    private void OnClientConnected(ulong clientId)
{
    Debug.Log($"[GameManager] Player {clientId} connected.");

    if (IsServer)
    {
        // Spawn the player
        GameObject playerInstance = Instantiate(playerPrefab, Vector3.zero, Quaternion.identity);
        NetworkObject networkObject = playerInstance.GetComponent<NetworkObject>();
        networkObject.SpawnAsPlayerObject(clientId);

        // === ADD GOAL ASSIGNMENT HERE === //
        NetworkPlayer player = playerInstance.GetComponent<NetworkPlayer>();
        if (player != null)
        {
            int goalIdx = GoalManager.Instance.GetRandomGoalIndex();
            player.goalIndex.Value = goalIdx; // Syncs to all clients
            Debug.Log($"[GameManager] Assigned goal {goalIdx} to player {clientId}");
        }
        else
        {
            Debug.LogError("[GameManager] Player prefab missing NetworkPlayer component!");
        }
        // ================================ //

        // Check if all players are connected
        if (NetworkManager.Singleton.ConnectedClients.Count == maxPlayers)
        {
            Debug.Log("[GameManager] All players connected. Loading GameStartScene...");
            NetworkManager.Singleton.SceneManager.LoadScene("GameStartScene", LoadSceneMode.Single);
        }
    }
}

    private void AssignGoalCardsToPlayers()
    {
        if (!IsServer || GoalManager.Instance == null) return;

        foreach (var client in NetworkManager.Singleton.ConnectedClients)
        {
            var player = client.Value.PlayerObject.GetComponent<NetworkPlayer>();
            if (player != null)
            {
                player.goalIndexVariable.Value = GoalManager.Instance.GetRandomGoalIndex();
            }
        }
    }
}