using UnityEngine;
using TMPro;
using Unity.Netcode;

public class GoalDisplay : MonoBehaviour
{
    public static GoalDisplay Instance;
    
    [SerializeField] private TextMeshProUGUI goalTitle;
    [SerializeField] private TextMeshProUGUI goalDescription;
    [SerializeField] private GameObject cardObject;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void DisplayGoal(Goal goal)
    {
        if (goal == null)
        {
            Debug.LogError("Received null goal!");
            return;
        }

        goalTitle.text = goal.title;
        goalDescription.text = goal.description;
        cardObject.SetActive(true);
        
        Debug.Log($"Displaying goal: {goal.title}");
    }

    public void CloseGoal() 
    {
        cardObject.SetActive(false);
    }
}