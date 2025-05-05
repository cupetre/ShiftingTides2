using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ResourceManager : MonoBehaviour
{
    public int money = 0;
    public int people = 0;
    public int influence = 0;

    public TextMeshProUGUI moneyCount;
    public TextMeshProUGUI peopleCount;
    public Slider influenceSlider;
    public TextMeshProUGUI influenceCount;

    void Start()
    {
        moneyCount.text = "20";
        peopleCount.text = "30";
    }

    public void AddMoney(int amount)
    {
        money += amount;
        UpdateUI();
    }

    public void AddPeople(int amount)
    {
        people += amount;
        UpdateUI();
    }
    public void AddInfluence(int amount)
    {
        influence = Mathf.Clamp(influence + amount, 0, 100);
        UpdateUI();
    }

    void UpdateUI()
    {
        moneyCount.text = money.ToString();
        peopleCount.text = people.ToString();
        influenceSlider.value = influence;

        if (influenceCount != null) { 
            influenceCount.text = influence + "%";
        }
    }
}