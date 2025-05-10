using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Netcode;
using System;
public class ResourceManager : NetworkBehaviour
{
    public NetworkList<int> money;
    public NetworkList<int> people;
    public NetworkList<float> influence;

    public NetworkList<bool> loseList;

    public TextMeshProUGUI moneyCount;
    public TextMeshProUGUI peopleCount;
    public Slider influenceSlider;
    public TextMeshProUGUI influenceCount;
    private GameObject playerObject;
    private NetworkPlayer networkPlayer;

    private ulong clientId;
    private int playerIndex;



    void Awake()
    {
        money = new NetworkList<int>();
        people = new NetworkList<int>();
        influence = new NetworkList<float>();
        loseList = new NetworkList<bool>();
    }

    private void Start()
    {
        // Get the client ID and player index
        clientId = NetworkManager.Singleton.LocalClientId;

        // Find the player through the NetworkManager
        playerObject = NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject.gameObject;
        if (playerObject == null)
        {
            Debug.LogError("[ResourceManager] Player object not found.");
            return;
        }
        // Get the player index from the NetworkPlayer component
        networkPlayer = playerObject.GetComponent<NetworkPlayer>();
        if (networkPlayer == null)
        {
            Debug.LogError("[ResourceManager] NetworkPlayer component not found on player object.");
            return;
        }
        playerIndex = networkPlayer.playerIndex.Value;
    }
    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            for (int i = 0; i < 4; i++)
            {
                money.Add(50);
                people.Add(50);
                influence.Add(50f);
                loseList.Add(false);
            }
        }

        money.OnListChanged += OnResourceChanged;
        people.OnListChanged += OnResourceChanged;
        influence.OnListChanged += OnResourceChanged;

        UpdateUI();
    }

    private void OnResourceChanged(NetworkListEvent<int> changeEvent)
    {
        UpdateUI();
    }

    private void OnResourceChanged(NetworkListEvent<float> changeEvent)
    {
        UpdateUI();
    }

    [ServerRpc]
    public void AddMoneyServerRpc(int playerIndex, int amount)
    {
        money[playerIndex] += amount;

        if (money[playerIndex].Value == 0)
        {
            loseList[playerIndex] = true;
            callLoseScene(playerIndex);
        }
    }

    [ServerRpc]
    public void AddPeopleServerRpc(int playerIndex, int amount)
    {
        people[playerIndex] += amount;
        if (people[playerIndex] == 0)
        {
            loseList[playerIndex] = true;
             callLoseScene(playerIndex);
        }
    }

    [ServerRpc]
    public void AddInfluenceServerRpc(int playerIndex, int amount)
    {
        influence[playerIndex] = Mathf.Clamp(influence[playerIndex] + amount, 0, 100);
        if (influence[playerIndex] == 0)
        {
            loseList[playerIndex] = true;
             callLoseScene(playerIndex);
        }
    }


    void UpdateUI()
    {
        if (!IsOwner && !IsClient) return;  // opcional, evita rodar no servidor ou outro cliente que não é dono

        moneyCount.text = money[playerIndex].ToString();
        peopleCount.text = people[playerIndex].ToString();
        influenceSlider.value = influence[playerIndex];

        if (influenceCount != null)
        {
            influenceCount.text = influence[playerIndex] + "%";
        }
    }

    public int GetMoney(int playerIndex)
    {
        return money[playerIndex];
    }

    public int GetPeople(int playerIndex)
    {
        return people[playerIndex];
    }

    public int GetInfluence(int playerIndex)
    {
        return Mathf.RoundToInt(influence[playerIndex]);
    }

    public void callLoseScene(int targetPlayerIndex)
    {
        networkPlayer.HandleLostClientRpc(targetPlayerIndex);   

    }

}