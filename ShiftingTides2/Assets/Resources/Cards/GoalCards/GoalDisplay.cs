using UnityEngine;
using TMPro;

public class GoalDisplay : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI goalText;
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
            Goal goal = GoalManager.Instance.GetGoal(localPlayer.goalIndex.Value); // Access the Value of the NetworkVariable
            goalText.text = goal.description;
        }
        else
        {
            Debug.LogError("Local player not found!");
        }
    }

    public void closeGoal() 
    {
         cardObject.SetActive(false);
    }
}
