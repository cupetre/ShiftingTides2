using Unity.Netcode;
using UnityEngine;
using System.Linq;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class GameManager : NetworkBehaviour
{
    public static GameManager Instance;

    // NetworkVariable to store all client IDs
    public NetworkVariable<List<ulong>> clientIds = new NetworkVariable<List<ulong>>(new List<ulong>(0),
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);
    private int nextPlayerIndex = 0;

    private HashSet<int> assignedIndices = new HashSet<int>();

    [SerializeField] private int maxPlayers = 4;
    [SerializeField] private GameObject playerPrefab;

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
        for (int i = 0; i < maxPlayers; i++)
        {
            if (!assignedIndices.Contains(i))
            {
                assignedIndices.Add(i);
                Debug.Log($"[GameManager] Assigned player index {i}");
                return i;
            }
        }
        Debug.LogError("[GameManager] No available player indices!");
        return -1;
    }

    private void OnClientConnected(ulong clientId)
    {
        Debug.Log($"[GameManager] Player {clientId} connected.");

        if (!IsServer)
        {
            Debug.LogWarning("[GameManager] OnClientConnected called on non-server instance. Skipping.");
        }

        GameObject playerInstance;

        // Check if the player already has a NetworkObject
        if (NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject != null)
        {
            playerInstance = NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject.gameObject;
            Debug.Log($"[GameManager] Player {clientId} already has a player instance.");
        }
        else
        {
            playerInstance = Instantiate(playerPrefab, Vector3.zero, Quaternion.identity);
            NetworkObject networkObject = playerInstance.GetComponent<NetworkObject>();

            if (networkObject == null)
            {
                Debug.LogError("[GameManager] Player prefab does not have a NetworkObject component.");
                Destroy(playerInstance);
                return;
            }

            networkObject.SpawnAsPlayerObject(clientId);
        }

        InitializePlayer(playerInstance, clientId);
    }

    private void InitializePlayer(GameObject playerInstance, ulong clientId)
    {
        // Assign a player index
        NetworkPlayer player = playerInstance.GetComponent<NetworkPlayer>();
        if (player != null)
        {
            int index = AssignPlayerIndex();
            if (index >= 0)
            {
                player.playerIndex.Value = index; // Syncs to all clients
                Debug.Log($"[GameManager] Assigned player index {index} to client {clientId}");
            }
            else
            {
                Debug.LogError("[GameManager] No available player indices!");
            }
        }
        else
        {
            Debug.LogError("[GameManager] Player prefab does not have a NetworkPlayer component.");
        }

        // Check if all players are connected
        if (NetworkManager.Singleton.ConnectedClients.Count == maxPlayers)
        {
            Debug.Log("[GameManager] All players connected. Loading GameStartScene...");
            NetworkManager.Singleton.SceneManager.LoadScene("GameStartScene", LoadSceneMode.Single);
            // Assign goals to players
            AssignGoalsToPlayers();
        }

        // Add clientId to clientIds
        if (!clientIds.Value.Contains(clientId))
        {
            clientIds.Value.Add(clientId);
            Debug.Log($"[GameManager] Added clientId {clientId} to clientIds.");
        }
        else
        {
            Debug.LogWarning($"[GameManager] clientId {clientId} already exists in clientIds.");
        }

    }

    private void AssignGoalsToPlayers()
    {
        foreach (var clientId in NetworkManager.Singleton.ConnectedClientsIds)
        {
            NetworkPlayer player = NetworkManager.ConnectedClients[clientId].PlayerObject.GetComponent<NetworkPlayer>();
            if (player != null && player.goalIndex.Value == -1)
            {
                int goalIndex = GoalManager.Instance.GetRandomGoalIndex();
                if (goalIndex >= 0)
                {
                    player.goalIndex.Value = goalIndex;
                    Debug.Log($"[GameManager] Assigned goal {goalIndex} to player {clientId}");
                }
            }
            else
            {
                Debug.LogError("[GameManager] Player either missing, no more unique goals or already assigned goal!");
            }
        }
    }
}