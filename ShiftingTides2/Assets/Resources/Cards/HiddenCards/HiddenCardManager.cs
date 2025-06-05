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
        public HiddenCard[] hidden;
    }

    public HiddenCard[] hidden;
    private bool hiddenLoaded = false;

    private HashSet<int> assignedHiddenCardsIndices = new HashSet<int>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            StartCoroutine(LoadHiddenCards()); // Initialize hiddenCard loading]
        }
        else
        {
            Destroy(gameObject);
        }
    }
    private IEnumerator LoadHiddenCards()
    {
        // Load JSON file from Resources
        TextAsset jsonFile = Resources.Load<TextAsset>("Cards/HiddenCards/hidden-cards");
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
            hidden = wrapper?.hidden;
            Debug.Log($"[HiddenManager] Loaded {hidden?.Length} hidden");
            Debug.Log($"[HiddenManager] Deserialized JSON: {JsonUtility.ToJson(hidden, true)}");

            if (hidden == null || hidden.Length == 0)
            {
                Debug.LogError("[HiddenManager] No hidden loaded. Check:");
                Debug.LogError($"[HiddenManager] 1. JSON validity: {jsonFile.text}");
                Debug.LogError("[HiddenManager] 2. Hidden class structure matches JSON");
            }
            else
            {
                // Log all loaded hidden for debugging
                foreach (var hiddenCard in hidden)
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
        if (!hiddenLoaded || hidden == null) return null;

        /*// 25% channce to get a card
        if (Random.Range(0, 4) == 0)
        {
            // Ensure we don't return the same card twice
            int index;
            do
            {
                index = Random.Range(0, hidden.Length);
            } while (assignedHiddenCardsIndices.Contains(index));
            assignedHiddenCardsIndices.Add(index);
            return hidden[index];
        }
        else
        {
            return null;
        }*/
        return hidden[Random.Range(0, hidden.Length)];

        // }
    }

    public bool AreHiddenCardsLoaded()
    {
        return hiddenLoaded;
    }

}
