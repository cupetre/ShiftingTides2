using Unity.Netcode;
using UnityEngine;

public class NetworkPlayer : NetworkBehaviour
{
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Sprite[] playerSprites; // 4 sprites

    private NetworkVariable<int> playerId = new NetworkVariable<int>(-1);
    public NetworkVariable<int> goalIndex = new NetworkVariable<int>(-1, 
    NetworkVariableReadPermission.Everyone,
    NetworkVariableWritePermission.Server);
    public NetworkVariable<int> goalIndexVariable = new NetworkVariable<int>(-1);

    public override void OnNetworkSpawn()
    {
        if (IsOwner && goalIndex.Value >= 0)
        {
            GoalDisplay.Instance.DisplayGoal(GoalManager.Instance.GetGoal(goalIndex.Value));
        }
        // Server assigns ID, position, and goal
        if (IsServer)
        {
            int index = GameManager.Instance.AssignPlayerIndex();
            playerId.Value = index;
            SetPlayerPositionClientRpc(index);
            goalIndexVariable.Value = GoalManager.Instance.GetRandomGoalIndex();
        }

        // Owner sets sprite and UI
        if (IsOwner)
        {
            AssignSprite();
            if (UIManager.Instance != null)
                UIManager.Instance.ShowConnectedMessage();
        }

        // Listen for ID changes (though server sets it once)
        playerId.OnValueChanged += OnPlayerIdChanged;
    }

    private void AssignSprite()
    {
        if (spriteRenderer == null || playerSprites.Length == 0) return;

        // Ensure unique sprites per player
        int spriteIndex = (int)(OwnerClientId % (ulong)playerSprites.Length);
        spriteRenderer.sprite = playerSprites[spriteIndex];
        Debug.Log($"[NetworkPlayer] Assigned sprite {spriteIndex} to client {OwnerClientId}");
    }

    [ClientRpc]
    private void SetPlayerPositionClientRpc(int index)
    {
        Vector3[] positions = new Vector3[]
        {
            new Vector3(4f, 1f, 0f),   // Player 1
            new Vector3(4f, -1f, 0f),  // Player 2
            new Vector3(2f, 1f, 0f),    // Player 3
            new Vector3(2f, -1f, 0f)    // Player 4
        };

        if (index >= 0 && index < positions.Length)
        {
            transform.position = positions[index];
            Debug.Log($"[NetworkPlayer] Player {index} positioned at {positions[index]}");
        }
        else
        {
            Debug.LogError($"[NetworkPlayer] Invalid position index: {index}");
        }
    }

    private void OnPlayerIdChanged(int oldId, int newId)
    {
        Debug.Log($"[NetworkPlayer] ID changed from {oldId} to {newId}");
        // Optional: Add logic if IDs change dynamically later
    }

    public override void OnNetworkDespawn()
    {
        playerId.OnValueChanged -= OnPlayerIdChanged; // Cleanup
    }
}