using UnityEngine;
using TMPro;
using Unity.Netcode; 
public class CardUtils : MonoBehaviour
{
    [SerializeField] private GameObject card; 
    public void closeCard() {
         card.SetActive(false);
    }

    public void openCard() {
        card.SetActive(true);
    }
    
}