using Unity.Netcode;
using UnityEngine;

public class NetworkPlayer : NetworkBehaviour
{
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Sprite[] playerSprites; // 4 pixel art characters
    private NetworkVariable<int> playerId = new NetworkVariable<int>(-1); // Unique ID

    public NetworkVariable<string> PlayerName = new NetworkVariable<string>("Player"); // Player name
    public NetworkVariable<int> goalIndex = new NetworkVariable<int>(-1); // Goal index

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
        playerId.OnValueChanged += OnPlayerIdChanged;

        if (IsServer)
        {
            int index = GameManager.Instance.AssignPlayerIndex();
            playerId.Value = index; // Set the playerId on the server, which will sync to clients
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