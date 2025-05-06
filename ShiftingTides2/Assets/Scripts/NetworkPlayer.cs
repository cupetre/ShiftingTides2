using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class NetworkPlayer : NetworkBehaviour
{
    [SerializeField] private SpriteRenderer spriteRenderer;

    [SerializeField] private Sprite[] playerSprites; // 4 pixel art characters

    private NetworkVariable<int> playerId = new NetworkVariable<int>(-1); // Unique ID
    public NetworkVariable<int> goalIndex = new NetworkVariable<int>(-1, 
        NetworkVariableReadPermission.Everyone, 
        NetworkVariableWritePermission.Server
        );
    public NetworkVariable<int> goalIndexVariable = new NetworkVariable<int>(-1);

    public void SetPlayerId(int id)
    {
        if (IsServer)
        {
            playerId.Value = id;
        }
        else
        {
            Debug.LogWarning($"[NetworkPlayer] Attempted to set playerId on client. This should only be done on the server.");
        }
    }
    public override void OnNetworkSpawn()
{
    if (IsServer)
    {
        int index = GameManager.Instance.AssignPlayerIndex();
        SetPlayerPositionClientRpc(index);

        // Assign a random goal index from GoalManager to the player
        int goalIndex = GoalManager.Instance.GetRandomGoalIndex();
        goalIndexVariable.Value = goalIndex; // Set the value for the player
    }

    if (IsOwner)
    {
        AssignSprite();
        UIManager.Instance.ShowConnectedMessage();
    }
}

    private void AssignSprite()
    {
        ulong id = OwnerClientId; // From Netcode
        int index = (int)(id % (ulong)playerSprites.Length);
        spriteRenderer.sprite = playerSprites[index];
    }

    [ClientRpc]
    public void SetPlayerPositionClientRpc(int index)
    {
        Vector3[] positions = new Vector3[]
        {
        new Vector3(4f, 1f, 0f),   // Player 1
        new Vector3(4f, -1f, 0f),  // Player 2
        new Vector3(2f, 1f, 0f),   // Player 3
        new Vector3(2f, -1f, 0f)   // Player 4
        };

        if (index >= 0 && index < positions.Length)
        {
            transform.position = positions[index];
        }
        else
        {
            Debug.LogError($"[NetworkPlayer] Invalid player index received: {index}");
        }
    }


    private void OnPlayerIdChanged(int oldId, int newId)
    {
        if (spriteRenderer != null && newId >= 0 && newId < playerSprites.Length)
        {
            spriteRenderer.sprite = playerSprites[newId];
        }
        else if (spriteRenderer == null)
        {
            Debug.LogError("[NetworkPlayer] spriteRenderer is null!");
        }

        // Now that we have the playerId, set the initial position
        if (IsOwner && newId >= 0)
        {
            SetPlayerPositionClientRpc(newId); // Call the ClientRpc on the owner to set their position
            Debug.Log($"[NetworkPlayer] You are Player {newId}");
        }
        else if (!IsOwner && newId >= 0)
        {
            SetPlayerPositionClientRpc(newId); // Let the server tell all clients their position
            Debug.Log($"[NetworkPlayer] Client received playerId {newId}, setting position.");
        }
    }
}