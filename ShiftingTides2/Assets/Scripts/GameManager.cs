using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : NetworkBehaviour
{
    public static GameManager Instance;

    private int currentPlayerIndex = 0;

    [SerializeField] private int maxPlayers = 4;

    [SerializeField] private GameObject playerPrefab;
 
    private void Awake()
    {
        if (Instance == null) 
            Instance = this;
        else
            Destroy(gameObject);
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

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
    }

    private void OnClientConnected(ulong clientId)
    {
        Debug.Log($"[GameManager] Player {clientId} connected. Total: {NetworkManager.Singleton.ConnectedClients.Count}");

        if (IsServer)
        {
            // Spawn a temporary representation of the player in the main menu (optional, for visual feedback)
            GameObject playerInstance = Instantiate(playerPrefab, Vector3.zero, Quaternion.identity); // Adjust position as needed
            NetworkObject networkObject = playerInstance.GetComponent<NetworkObject>();
            if (networkObject != null)
            {
                networkObject.SpawnAsPlayerObject(clientId);
            }
            else
            {
                Debug.LogError($"[GameManager] Player prefab does not have a NetworkObject component!");
            }

            // Check if all players are connected
            if (NetworkManager.Singleton.ConnectedClients.Count == maxPlayers)
            {
                Debug.Log("[GameManager] All players connected in the main menu. Assigning goal cards...");
                AssignGoalCardsToPlayers();
                Debug.Log("[GameManager] Loading GameStartScene...");
                NetworkManager.Singleton.SceneManager.LoadScene("GameStartScene", LoadSceneMode.Single);
            }
        }
    }

    private void AssignGoalCardsToPlayers()
    {
        if (IsServer && GoalManager.Instance != null && GoalManager.Instance.AreGoalsLoaded())
        {
            List<ulong> clientIds = NetworkManager.Singleton.ConnectedClientsIds.ToList();
            if (clientIds.Count == maxPlayers)
            {
                foreach (ulong clientId in clientIds)
                {
                    if (NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out NetworkClient client) && client.PlayerObject != null)
                    {
                        var networkPlayer = client.PlayerObject.GetComponent<NetworkPlayer>();
                        if (networkPlayer != null)
                        {
                            int randomGoalIndex = GoalManager.Instance.GetRandomGoalIndex();
                            networkPlayer.goalIndex.Value = randomGoalIndex;
                            GoalManager.Instance.AssignGoalToPlayer(clientId, randomGoalIndex);
                            Debug.Log($"[GameManager] Assigned goal index {randomGoalIndex} to client {clientId} in the main menu.");
                        }
                        else
                        {
                            Debug.LogError($"[GameManager] NetworkPlayer component not found for client {clientId} during goal card assignment.");
                        }
                    }
                    else
                    {
                        Debug.LogError($"[GameManager] PlayerObject is null for client {clientId} during goal card assignment.");
                    }
                }
            }
            else
            {
                Debug.LogError("[GameManager] Not enough players connected to assign goal cards.");
            }
        }
        else
        {
            Debug.LogError("[GameManager] GoalManager not initialized or goals not loaded when trying to assign cards.");
        }
    }

}
