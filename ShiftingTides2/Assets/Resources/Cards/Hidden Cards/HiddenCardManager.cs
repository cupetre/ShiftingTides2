using UnityEngine;
using System.Collections;
using System;
using System.Collections.Generic;
using Random = UnityEngine.Random;

public class HiddenCardManager : MonoBehaviour
{
    public static HiddenCardManager Instance;

    [System.Serializable]
    public class HiddenArrayWrapper
    {
        public HiddenCard[] hiddenCards;
    }

    public HiddenCard[] hiddenCards;
    private bool hiddenLoaded = false;

    private HashSet<int> assignedHiddenCardsIndices = new HashSet<int>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            StartCoroutine(LoadHiddenCards()); // Initialize hiddenCard loading
        }
        else
        {
            Destroy(gameObject);
        }
    }
    private IEnumerator LoadHiddenCards()
    {
        // Load JSON file from Resources
        TextAsset jsonFile = Resources.Load<TextAsset>("Cards/hiddenCardCards/hiddenCard-cards");
        if (jsonFile == null)
        {
            Debug.LogError("[HiddenManager] JSON file not found");
            yield break;
        }

        try
        {
            // Deserialize JSON data
            HiddenArrayWrapper wrapper = JsonUtility.FromJson<HiddenArrayWrapper>(jsonFile.text);
            //hidden = JsonHelper.FromJson<Hidden>(jsonFile.text);
            hiddenCards = wrapper?.hiddenCards;
            Debug.Log($"[HiddenManager] Loaded {hiddenCards?.Length} hidden");
            Debug.Log($"[HiddenManager] Deserialized JSON: {JsonUtility.ToJson(hiddenCards, true)}");

            if (hiddenCards == null || hiddenCards.Length == 0)
            {
                Debug.LogError("[HiddenManager] No hidden loaded. Check:");
                Debug.LogError($"[HiddenManager] 1. JSON validity: {jsonFile.text}");
                Debug.LogError("[HiddenManager] 2. Hidden class structure matches JSON");
            }
            else
            {
                // Log all loaded hidden for debugging
                foreach (var hiddenCard in hiddenCards)
                {
                    Debug.Log($"[HiddenManager] Loaded: {hiddenCard.title} (ID: {hiddenCard.id})");
                }
                hiddenLoaded = true;
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[HiddenManager] Error loading hidden: {e.Message}");
        }

        yield return null;
    }

    // public Hidden GethiddenCard(int index)
    // {
    //     // Safe array access with bounds checking
    //     return (index >= 0 && index < hidden.Length) ? hidden[index] : null;
    // }

    public HiddenCard GetRandomHiddenCard()
    {
        if (!hiddenLoaded || hiddenCards == null) return null;

        if (Random.Range(0, 3) == 0) 
        {
           return hiddenCards[Random.Range(0, hiddenCards.Length)];

        }

        return null;
    }

    public bool AreHiddenCardsLoaded()
    {
        return hiddenLoaded;
    }

}
