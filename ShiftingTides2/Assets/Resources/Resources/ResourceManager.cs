using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Netcode;
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


    private int playerIndex;
    void Awake()
    {
        money = new NetworkList<int>();
        people = new NetworkList<int>();
        influence = new NetworkList<float>();
        loseList = new NetworkList<bool>();
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

        if (money[playerIndex] == 0)
        {
            loseList[playerIndex] = true;
        }
    }

    [ServerRpc]
    public void AddPeopleServerRpc(int playerIndex, int amount)
    {
        people[playerIndex] += amount;
        if (people[playerIndex] == 0)
        {
          loseList[playerIndex] = true;
        }
    }

    [ServerRpc]
    public void AddInfluenceServerRpc(int playerIndex, int amount)
    {
        influence[playerIndex] = Mathf.Clamp(influence[playerIndex] + amount, 0, 100);
        if (influence[playerIndex] == 0)
        {
           loseList[playerIndex] = true;
        }
    }


    void UpdateUI()
    {
        moneyCount.text = money[playerIndex].ToString();
        peopleCount.text = people[playerIndex].ToString();
        influenceSlider.value = influence[playerIndex];

        if (influenceCount != null)
        {
            influenceCount.text = influence[playerIndex] + "%";
        }
    }
}