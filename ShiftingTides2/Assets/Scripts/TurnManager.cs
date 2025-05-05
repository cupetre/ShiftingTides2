using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using System.Linq;

public class TurnManager : NetworkBehaviour
{
    public static TurnManager Instance;

    private NetworkList<ulong> playerOrder; // List to store the order of players by ClientId

    private NetworkVariable<int> currentTurnIndex = new NetworkVariable<int>(0);

    [SerializeField] private float turnDuration = 30f; // Example turn duration

    private float turnStartTime;

    [SerializeField] private GameStartSceneUI gameStartUIManager; // Reference to the UI Manager

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

        playerOrder = new NetworkList<ulong>();
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            // When the game starts on the server, populate the player order
            foreach (var client in NetworkManager.Singleton.ConnectedClients)
            {
                playerOrder.Add(client.Key);
            }

            if (playerOrder.Count > 0)
            {
                StartNextTurn();
            }
        }
    }

    private void Update()
    {
        if (IsServer)
        {
            if (NetworkManager.Singleton.ConnectedClients.Count == 0) 
                return; // Avoid errors if no clients

            if (Time.time - turnStartTime >= turnDuration)
            {
                Debug.Log($"[Server] Turn for client {playerOrder[currentTurnIndex.Value]} timed out.");
                // Implement logic for turn timeout (e.g., force end turn, skip player)
                AdvanceTurn();
            }
        }
    }

    private void StartNextTurn()
    {
        if (playerOrder.Count == 0) return;

        ulong currentClientId = playerOrder[currentTurnIndex.Value];
        Debug.Log($"[Server] Starting turn for client: {currentClientId}");

        // Inform all clients about the start of the turn
        RpcStartTurnClientRpc(currentClientId);
        turnStartTime = Time.time;
    }

    public void EndTurn()
    {
        if (IsServer)
        {
            AdvanceTurn();
        }
        else
        {
            // If a client wants to end their turn, they need to send a Command to the server
            RequestEndTurnServerRpc();
        }
    }

    private void AdvanceTurn()
    {
        if (playerOrder.Count == 0) return;

        currentTurnIndex.Value = (currentTurnIndex.Value + 1) % playerOrder.Count;
        StartNextTurn();
    }

    [ServerRpc]
    private void RequestEndTurnServerRpc(ServerRpcParams rpcParams = default)
    {
        int clientId = playerOrder.IndexOf(rpcParams.Receive.SenderClientId);
        Debug.Log($"[Server] Client " + clientId + " requested to end their turn.");
        AdvanceTurn();
    }

    [ClientRpc]
    private void RpcStartTurnClientRpc(ulong clientId)
    {
        Debug.Log($"[Client] Turn started for client: {clientId}. Local client ID: {NetworkManager.Singleton.LocalClientId}");

        // Get the NetworkPlayer of the client whose turn it is
        if (NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out NetworkClient client) && client.PlayerObject != null)
        {
            NetworkPlayer networkPlayer = client.PlayerObject.GetComponent<NetworkPlayer>();
            if (networkPlayer != null && gameStartUIManager != null)
            {
                gameStartUIManager.UpdateCurrentPlayerDisplayClientRpc(networkPlayer.PlayerName.Value);

                // Show/hide action buttons based on whether it's the local player's turn
                bool isLocalPlayerTurn = clientId == NetworkManager.Singleton.LocalClientId;
                gameStartUIManager.SetActionButtonsVisibilityClientRpc(isLocalPlayerTurn);
            }
        }
    }
}