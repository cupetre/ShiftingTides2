using Unity.Netcode;
using UnityEngine;

public enum EmotionState
    {
        Neutral,
        Happy,
        Angry
    }
public class NetworkPlayer : NetworkBehaviour
{
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Character[] playerSprites; // 4 sprites
    [SerializeField] private ScreenTransition lostScreenTransition;
    public NetworkVariable<EmotionState> emotionState = new NetworkVariable<EmotionState>(
        EmotionState.Neutral,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private bool hasSpawned = false;

    public NetworkVariable<int> playerIndex = new NetworkVariable<int>(-1,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    public NetworkVariable<int> goalIndex = new NetworkVariable<int>(-1,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    public override void OnNetworkSpawn()
    {
        if (hasSpawned) return; // Prevent multiple spawns
        hasSpawned = true;

        if (playerIndex.Value >= 0)
        {
            AssignSprite();
            SetPlayerPosition();
        }

        playerIndex.OnValueChanged += OnPlayerIndexChanged;
        emotionState.OnValueChanged += OnEmotionChanged;

        OnEmotionChanged(emotionState.Value, EmotionState.Neutral);

    }
    private void OnEmotionChanged(EmotionState oldValue, EmotionState newValue)
{
    switch (newValue)
    {
        case EmotionState.Happy:
            changeToHappySprite();
            break;
        case EmotionState.Angry:
            changeToAngrySprite();
            break;
        default:
            changeToNeutralSprite();
            break;
    }
}


    private void AssignSprite()
    {
        if (spriteRenderer == null || playerSprites.Length == 0) return;

        if (playerIndex.Value < 0 || playerIndex.Value >= playerSprites.Length)
        {
            Debug.LogError($"[NetworkPlayer] Invalid player index: {playerIndex.Value}, cannot assign sprite.");
            return;
        }

        // Ensure unique sprites per player
        int spriteIndex = playerIndex.Value % playerSprites.Length;
        spriteRenderer.sprite = playerSprites[spriteIndex].neutral;
        // Ensure sprite scale is reset
        spriteRenderer.transform.localScale = new Vector3(1f, 1f, 1f);
        spriteRenderer.sortingOrder = 1; // Set sorting order to 1
        Debug.Log($"[NetworkPlayer] Assigned sprite {spriteIndex} to player {playerIndex.Value}");
    }

    public void changeToAngrySprite()
    {
        int spriteIndex = playerIndex.Value % playerSprites.Length;
        spriteRenderer.sprite = playerSprites[spriteIndex].angry;
    }

    public void changeToHappySprite()
    {
        int spriteIndex = playerIndex.Value % playerSprites.Length;
        spriteRenderer.sprite = playerSprites[spriteIndex].smiling;
    }

    public void changeToNeutralSprite()
    {
        int spriteIndex = playerIndex.Value % playerSprites.Length;
        spriteRenderer.sprite = playerSprites[spriteIndex].neutral;
    }
    private void SetPlayerPosition()
    {
        Vector3[] positions = new Vector3[]
        {
                new Vector3(-3f, -2f, 0f),   // Player 1
                new Vector3(-1f, -2f, 0f),  // Player 2
                new Vector3(1f, -2f, 0f),   // Player 3
                new Vector3(3f, -2f, 0f)   // Player 4
        };

        if (playerIndex.Value >= 0 && playerIndex.Value < positions.Length)
        {
            transform.position = positions[playerIndex.Value];
            transform.localScale = new Vector3(2f, 2f, 1f); // Reset scale
            // For sprites with different sizes reset scale
            if (playerIndex.Value >= 2)
            {
                transform.localScale = new Vector3(1f, 1f, 1f);
            }
            Debug.Log($"[NetworkPlayer] Player {playerIndex.Value} positioned at {positions[playerIndex.Value]}");
        }
        else
        {
            Debug.LogError($"[NetworkPlayer] Invalid position index: {playerIndex.Value}");
        }
    }

    private void OnPlayerIndexChanged(int oldIndex, int newIndex)
    {
        Debug.Log($"[NetworkPlayer] Player index changed from {oldIndex} to {newIndex}");
        AssignSprite();
        SetPlayerPosition();
    }

    public override void OnNetworkDespawn()
    {
        playerIndex.OnValueChanged -= OnPlayerIndexChanged; // Cleanup
    }

    [ClientRpc]
    public void HandleLostClientRpc(int targetPlayerIndex)
    {
        lostScreenTransition = FindObjectOfType<ScreenTransition>();

        if (spriteRenderer != null && playerIndex.Value == targetPlayerIndex)
        {
            spriteRenderer.enabled = false;
            Debug.Log($"[NetworkPlayer] Player {playerIndex.Value} sprite hidden on client.");
        }

        lostScreenTransition?.SetPlayerLost(playerIndex.Value == targetPlayerIndex, targetPlayerIndex);
    }


}
