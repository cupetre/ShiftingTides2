using TMPro;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Rendering;

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
    [SerializeField] private TMP_Text nameText;
    
    public NetworkVariable<FixedString128Bytes> playerName = new NetworkVariable<FixedString128Bytes>(
    "", NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
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

 public NetworkVariable<bool> playerLost = new NetworkVariable<bool>(false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);
    public override void OnNetworkSpawn()
    {
        if (hasSpawned) return;
        hasSpawned = true;

        if (IsOwner)
        {
            SetNameServerRpc(RelayManager.PlayerName);
        }

        if (playerIndex.Value >= 0)
        {
            AssignSprite();
            SetPlayerPosition();
        }

        playerIndex.OnValueChanged += OnPlayerIndexChanged;
        emotionState.OnValueChanged += OnEmotionChanged;
        playerName.OnValueChanged += OnPlayerNameChanged;

        OnEmotionChanged(emotionState.Value, EmotionState.Neutral);
        nameText.text = playerName.Value.ToString();

        Debug.Log($"[NetworkPlayer] OnNetworkSpawn for ClientID: {OwnerClientId}, Name: {playerName.Value}");
    }

    private void OnPlayerNameChanged(FixedString128Bytes previous, FixedString128Bytes current)
    {
        nameText.text = current.ToString();

    }

    [ServerRpc]
    private void SetNameServerRpc(string name)
    {
        playerName.Value = new FixedString128Bytes(name);
        Debug.Log($"[NetworkPlayer] Set name on server: {name}");
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


    private float NormalizeCharacterScale(SpriteRenderer renderer, float desiredHeight = 4f)
    {
        if (renderer.sprite == null) return 1f;

        float spriteHeight = renderer.sprite.bounds.size.y;
        float scale = desiredHeight / spriteHeight;
        renderer.transform.localScale = new Vector3(scale, scale, 1f);
        return scale;
    }

    [ClientRpc]
    private void UpdateScaleClientRpc(float scale)
    {
        transform.localScale = new Vector3(scale, scale, 1f);
    }

    private void AssignSprite()
    {
        if (spriteRenderer == null || playerSprites.Length == 0) return;

        if (playerIndex.Value < 0 || playerIndex.Value >= playerSprites.Length)
        {
            Debug.LogError($"[NetworkPlayer] Invalid player index: {playerIndex.Value}, cannot assign sprite.");
            return;
        }

        int spriteIndex = playerIndex.Value % playerSprites.Length;
        spriteRenderer.sprite = playerSprites[spriteIndex].neutral;

        float newScale = NormalizeCharacterScale(spriteRenderer, 4f);
        UpdateScaleClientRpc(newScale);


        spriteRenderer.sortingOrder = 1;
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
                new Vector3(-3f, -0.5f, 0f),   // Player 1
                new Vector3(-0f, -0.5f, 0f),  // Player 2
                new Vector3(3f, -0.5f, 0f),   // Player 3
                new Vector3(6f, -0.5f, 0f)   // Player 4
        };

        if (playerIndex.Value >= 0 && playerIndex.Value < positions.Length)
        {
            transform.position = positions[playerIndex.Value];
            //transform.localScale = new Vector3(2f, 2f, 1f); // Reset scale
            // For sprites with different sizes reset scale
            //if (playerIndex.Value >= 2)
            //{
            //transform.localScale = new Vector3(1f, 1f, 1f);
            //}
            Debug.Log($"[NetworkPlayer] Player {playerIndex.Value} positioned at {positions[playerIndex.Value]}");

            Canvas playerCanvas = gameObject.GetComponentInChildren<Canvas>();
            if (playerCanvas != null)
            {
                // Get the text component from the canvas
                TextMeshProUGUI playerText = playerCanvas.GetComponentInChildren<TextMeshProUGUI>();
                if (playerText != null)
                {
                    // Set the text to the player index if name not set in NetworkPlayer
                    string playerNameString = playerName.Value.ToString();
                    playerText.text = playerNameString;
                    if (string.IsNullOrEmpty(playerNameString))
                    {
                        playerText.text = $"Player {playerIndex.Value + 1}";
                        playerName.Value = new FixedString128Bytes(playerText.text); // Update playerName on server
                    }
                    Debug.Log($"[GameManager] Set player {playerIndex.Value + 1} text to '{playerText.text}'");

                    // Set the position of the text to be below the player sprite, using values from an array of positions based on top, bottom, left, right
                    // Position 1: Left 200, Right 500, Top 330, Botton 130
                    Vector2[] textPositions = new Vector2[]
                    {
                            new Vector2(-320.0f, -300.0f),
                            new Vector2(-0.0f, -300.0f),
                            new Vector2(320.0f, -300.0f),
                            new Vector2(640.0f, -300.0f),
                    };

                    playerText.rectTransform.anchoredPosition = textPositions[playerIndex.Value];
                    playerText.fontSize = 36;
                }
                else
                {
                    Debug.LogError("[GameManager] Player canvas does not have a TextMeshProUGUI component.");
                }
            }
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
        if (spriteRenderer != null && playerIndex.Value == targetPlayerIndex)
        {
            spriteRenderer.enabled = false;
            gameObject.SetActive(false); // Hide the player gameObject
            Debug.Log($"[NetworkPlayer] Player {playerIndex.Value} sprite hidden on client.");
            playerLost.Value = true;
        }
    }




}
