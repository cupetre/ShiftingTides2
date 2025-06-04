using Unity.Netcode;
using UnityEngine;
using System.Linq;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using TMPro;
using Unity.Collections;

public class GameManager : NetworkBehaviour
{
    public static GameManager Instance;

    // NetworkVariable to store all client IDs
    public NetworkVariable<List<ulong>> clientIds = new NetworkVariable<List<ulong>>(new List<ulong>(0),
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    private HashSet<int> assignedIndices = new HashSet<int>();

    [SerializeField] private int maxPlayers = 4;
    [SerializeField] private GameObject playerPrefab;

    public GameObject[] playerObjects;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        if (playerObjects == null || playerObjects.Length != maxPlayers)
        {
            playerObjects = new GameObject[maxPlayers];
        }
    }

    public override void OnNetworkSpawn()
    {
        if (SceneManager.GetActiveScene().name == "GameStartScene")
        {
            SetPlayerPositions();
        }
        // Keep only one subscription
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
    }

    public void SetPlayerPositions()
    {
        if (playerObjects == null || playerObjects.Length != maxPlayers)
        {
            Debug.LogError("[GameManager] Player objects array is not initialized or has incorrect size.");
            return;
        }
        for (int i = 0; i < playerObjects.Length; i++)
        {
            if (playerObjects[i] != null)
            {
                Vector3[] positions = new Vector3[]
                {
                    new Vector3(-4f, -1f, 0f),   // Player 1
                    new Vector3(-2f, -1f, 0f),  // Player 2
                    new Vector3(0f, -1f, 0f),   // Player 3
                    new Vector3(2f, -1f, 0f)    // Player 4
                };
                playerObjects[i].transform.position = positions[i];

                Canvas playerCanvas = playerObjects[i].GetComponentInChildren<Canvas>();
                if (playerCanvas != null)
                {
                    // Get the text component from the canvas
                    TextMeshProUGUI playerText = playerCanvas.GetComponentInChildren<TextMeshProUGUI>();
                    if (playerText != null)
                    {
                        // Set the text to the player index if name not set in NetworkPlayer
                        NetworkPlayer networkPlayer = playerObjects[i].GetComponent<NetworkPlayer>();
                        FixedString128Bytes playerName = networkPlayer != null ? networkPlayer.playerName.Value : new FixedString128Bytes($"Player {i + 1}");
                        string playerNameString = playerName.ToString();
                        playerText.text = playerNameString;
                        Debug.Log($"[GameManager] Set player {i + 1} text to '{playerText.text}'");

                        // Set the position of the text to be below the player sprite, using values from an array of positions based on top, bottom, left, right
                        // Position 1: Left 200, Right 500, Top 330, Botton 130
                        Vector2[] textPositions = new Vector2[]
                        {
                            new Vector2(-150.0f, -150.0f),
                            new Vector2(-50.0f, -150.0f),
                            new Vector2(50.0f, -150.0f),
                            new Vector2(150.0f, -150.0f)
                        };

                        if (i < textPositions.Length)
                        {
                            playerText.rectTransform.anchoredPosition = textPositions[i];
                            Debug.Log($"[GameManager] Set player {i + 1} text position to {textPositions[i]}");
                        }
                        else
                        {
                            Debug.LogError($"[GameManager] Invalid player index {i} for text position.");
                        }
                    }
                    else
                    {
                        Debug.LogError("[GameManager] Player canvas does not have a TextMeshProUGUI component.");
                    }
                }
            }
        }
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
        if (SceneManager.GetActiveScene().name != "MainMenuScene")
        {
            Debug.LogError("[GameManager] OnClientConnected called outside MainMenuScene. Skipping.");
            return;
        }
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

        // Store the player instance in the playerObjects array
        int playerIndex = player.GetComponent<NetworkPlayer>().playerIndex.Value;
        if (playerIndex >= 0 && playerIndex < maxPlayers)
        {
            playerObjects[playerIndex] = playerInstance;
            Debug.Log($"[GameManager] Player instance stored at index {playerIndex}");
        }
        else
        {
            Debug.LogError($"[GameManager] Invalid player index: {playerIndex}. Cannot store player instance.");
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
    public void InitializePlayersIfNeeded()
    {
        if (playerObjects == null || playerObjects.Length == 0)
        {
            Debug.Log("[GameManager] Reinitializing player objects in the new scene.");
            playerObjects = new GameObject[maxPlayers];
            for (int i = 0; i < maxPlayers; i++)
            {
                var playerNetworkObject = NetworkManager.Singleton.SpawnManager.GetPlayerNetworkObject(NetworkManager.Singleton.ConnectedClientsIds.ElementAt(i));
                if (playerNetworkObject != null)
                {
                    playerObjects[i] = playerNetworkObject.gameObject;
                    Debug.Log($"[GameManager] Player {i} reinitialized.");
                }
                else
                {
                    Debug.LogError($"[GameManager] Failed to find player object for client {i}.");
                }
            }
        }
    }
}