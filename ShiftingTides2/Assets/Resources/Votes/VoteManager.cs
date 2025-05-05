using UnityEngine;

public class VoteManager : MonoBehaviour
{
    public static int totalYes = 0;
    public static int totalNo = 0;
    
    public GameObject yesButton;  // Reference to the Yes button
    public GameObject noButton;   // Reference to the No button

    void Start()
    {
        // Initially hide the buttons
        yesButton.SetActive(false);
        noButton.SetActive(false);

        ShowButtons();
    }

    void Update()
    {
        // Optionally, you can check for certain conditions to show the buttons (if needed)
    }

    // Method to show the buttons when needed
    public void ShowButtons()
    {
        yesButton.SetActive(true);
        noButton.SetActive(true);
    }

    // Method for Yes vote
    public void voteYes()
    {
        totalYes++;
    }

    // Method for No vote
    public void voteNo()
    {
        totalNo++;
    }
}