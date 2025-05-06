using UnityEngine;
using TMPro;
using Unity.Netcode;

public class GoalDisplay : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI goalTitle;
    [SerializeField] private TextMeshProUGUI goalDescription;
    [SerializeField] private GameObject cardObject; 
    void Start()
    {
        NetworkPlayer localPlayer = null;
        
        foreach (var player in FindObjectsByType<NetworkPlayer>(FindObjectsSortMode.None))
        {
            if (player.IsOwner)
            {
                localPlayer = player;
                break;
            }
        }

        if (localPlayer != null)
        {
            Goal goal = GoalManager.Instance.GetGoal(localPlayer.goalIndex.Value);
            goalDescription.text = goal.description;
            goalTitle.text = goal.title;
        }
        else
        {
            Debug.LogError("Local player not found!");
        }
    }

    public void Show(Goal goal)
    {
        goalTitle.text = goal.title;
        goalDescription.text = goal.description;
        cardObject.SetActive(true);
    }


    public void closeGoal() 
    {
         cardObject.SetActive(false);
    }
}
