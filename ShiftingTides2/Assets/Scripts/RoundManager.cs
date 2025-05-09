using UnityEngine;
using UnityEngine.Networking;
using Unity.Netcode;
using TMPro;
using System.Collections.Generic;
public class RoundManager : NetworkBehaviour
{
    public NetworkVariable<int> round = new NetworkVariable<int>();
    public NetworkList<bool> playersTurnTracker;

    public TextMeshProUGUI roundCount;

    public void Awake()
    {
        playersTurnTracker = new NetworkList<bool>();
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            round.Value = 1;
            for (int i = 0; i < 4; i++)
            {
                playersTurnTracker.Add(false);
            }
        }

        round.OnValueChanged += (oldVal, newVal) => UpdateUI();
        UpdateUI();
    }

    [ServerRpc]
    public void TurnEndedServerRpc(int playerIndex)
    {
        playersTurnTracker[playerIndex] = true;
        CheckNewRoundServerRpc();
    }

    [ServerRpc]
    void CheckNewRoundServerRpc()
    {
        bool newTurn = true;
        foreach (var playedTurn in playersTurnTracker)
        {
            if (!playedTurn)
            {
                newTurn = false;
                break;
            }
        }

        if (newTurn)
        {
            round.Value += 1;

            // reset turn tracker for new turn
            for (int i = 0; i < playersTurnTracker.Count; i++)
            {
                playersTurnTracker[i] = false;
            }
        }

    }


    void UpdateUI()
    {
        roundCount.text = "Round " + round.Value.ToString();
    }
}
