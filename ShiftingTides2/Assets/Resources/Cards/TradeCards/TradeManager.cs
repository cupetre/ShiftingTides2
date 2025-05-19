using UnityEngine;
using System.Collections;
using System;
using System.Collections.Generic;
using Random = UnityEngine.Random;

public class TradeManager : MonoBehaviour
{
    public static TradeManager Instance;

    [System.Serializable]
    public class TradeArrayWrapper
    {
        public Trade[] actions;
    }
    public Trade[] trades;
    private bool tradesLoaded = false;
    private HashSet<int> assignedTradesIndices = new HashSet<int>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            StartCoroutine(LoadTrades()); // Initialize trade loading
        }
        else
        {
            Destroy(gameObject);
        }
    }
    private IEnumerator LoadTrades()
    {
        // Load JSON file from Resources
        TextAsset jsonFile = Resources.Load<TextAsset>("Cards/TradeCards/trade-cards");
        if (jsonFile == null)
        {
            Debug.LogError("[TradeManager] JSON file not found");
            yield break;
        }

        try
        {
            // Deserialize JSON data
            TradeArrayWrapper wrapper = JsonUtility.FromJson<TradeArrayWrapper>(jsonFile.text);
            //trades = JsonHelper.FromJson<Trade>(jsonFile.text);
            trades = wrapper?.actions;
            Debug.Log($"[TradeManager] Loaded {trades?.Length} trades");
            Debug.Log($"[TradeManager] Deserialized JSON: {JsonUtility.ToJson(trades, true)}");

            if (trades == null || trades.Length == 0)
            {
                Debug.LogError("[TradeManager] No trades loaded. Check:");
                Debug.LogError($"[TradeManager] 1. JSON validity: {jsonFile.text}");
                Debug.LogError("[TradeManager] 2. Trade class structure matches JSON");
            }
            else
            {
                // Log all loaded trades for debugging
                foreach (var trade in trades)
                {
                    Debug.Log($"[TradeManager] Loaded: {trade.title} (ID: {trade.id})");
                }
                tradesLoaded = true;
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[TradeManager] Error loading trades: {e.Message}");
        }

        yield return null;
    }

    public Trade GetRandomTrade()
    {
        if (!tradesLoaded || trades == null) return null;

        return trades[Random.Range(0, trades.Length)];
    }
}
