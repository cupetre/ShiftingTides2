using UnityEngine;
using TMPro;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;
    public TextMeshProUGUI connectionStatusText;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }
    
    public void ShowConnectedMessage()
    {
        connectionStatusText.text = "Successfully connected";
        connectionStatusText.gameObject.SetActive(true);
    }

}