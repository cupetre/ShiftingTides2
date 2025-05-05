using UnityEngine;
using Unity.Netcode;

public class Player : NetworkBehaviour
{
    // Player Resources (using NetworkVariables for synchronization)
    public NetworkVariable<int> Money = new NetworkVariable<int>(0);
    public NetworkVariable<int> People = new NetworkVariable<int>(0);
    public NetworkVariable<int> Influence = new NetworkVariable<int>(0);

    // Goal Card Data
    public GoalCardData GoalCard { get; private set; } // You'll need a GoalCardData class/struct

    // Server-only method to assign the goal card
    [ServerRpc]
    public void AssignGoalCardServerRpc(GoalCardData card)
    {
        GoalCard = card;
        Debug.Log($"[Player - Server] Assigned goal card '{card.title}' to ClientId: {OwnerClientId}");
        RpcAssignGoalCardClientRpc(card); // Tell the client their goal card
    }

    // ClientRPC to receive the assigned goal card
    [ClientRpc]
    private void RpcAssignGoalCardClientRpc(GoalCardData card)
    {
        GoalCard = card;
        Debug.Log($"[Player - Client] Received goal card '{card.title}'");
        // You might want to trigger UI updates here to show the goal card to the player
    }

    // Example method to add resources (can be called on the server)
    [ServerRpc]
    public void AddResourcesServerRpc(int money, int people, int influence)
    {
        Money.Value += money;
        People.Value += people;
        Influence.Value += influence;
    }

    // You might have other player-related logic here in the future
}

// Define a class or struct to hold the goal card data
[System.Serializable]
public struct GoalCardData
{
    public int id;
    public string type;
    public string title;
    public string description;
    public ResourceSet resources;
    public int rounds;
    public string target;
}

[System.Serializable]
public struct ResourceSet
{
    public int money;
    public int people;
    public int influence;
}