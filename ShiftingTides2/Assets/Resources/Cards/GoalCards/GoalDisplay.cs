using UnityEngine;
using TMPro;
using System.Collections;
using Unity.Netcode;
using UnityEngine.SceneManagement;

public class GoalDisplay : NetworkBehaviour
{
    public TMP_Text goalTitle;
    public TMP_Text goalDescription;

    private GameObject playerObject;
    private NetworkPlayer networkPlayer;
    public GameObject goalCard;
    public GameObject progressCard;
    public TMP_Text progressText;

    private ulong clientId;
    private int playerIndex;
    private int indexGoal;

    private Goal assignedGoal;
    private ResourceManager resourceManager;

    private void Start()
    {
        // 1) Encontra o ResourceManager
        resourceManager = FindFirstObjectByType<ResourceManager>();
        if (resourceManager == null)
        {
            Debug.LogError("[GoalDisplay] ResourceManager não encontrado.");
            return;
        }

        // 2) Inscreve-se nos eventos de mudança de lista
        resourceManager.money.OnListChanged += OnResourceChanged;
        resourceManager.people.OnListChanged += OnResourceChanged;
        resourceManager.influence.OnListChanged += OnResourceChanged;

        // 3) Pega o NetworkPlayer do cliente local
        clientId = NetworkManager.Singleton.LocalClientId;
        playerObject = NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject.gameObject;
        if (playerObject == null)
        {
            Debug.LogError("[GoalDisplay] Player object not found.");
            return;
        }

        networkPlayer = playerObject.GetComponent<NetworkPlayer>();
        if (networkPlayer == null)
        {
            Debug.LogError("[GoalDisplay] NetworkPlayer component not found on player object.");
            return;
        }
        playerIndex = networkPlayer.playerIndex.Value;

        // 4) Inicializa a exibição de goal e a progress
        InitializeGoalDisplay();
        UpdateProgressDisplay();
    }

    private void OnDestroy()
    {
        // Desinscrever para evitar vazamentos de memória
        if (resourceManager != null)
        {
            resourceManager.money.OnListChanged -= OnResourceChanged;
            resourceManager.people.OnListChanged -= OnResourceChanged;
            resourceManager.influence.OnListChanged -= OnResourceChanged;
        }
    }

    private void OnResourceChanged(NetworkListEvent<int> change)
    {
        UpdateProgressDisplay();
    }

    private void OnResourceChanged(NetworkListEvent<float> change)
    {
        UpdateProgressDisplay();
    }

    private void InitializeGoalDisplay()
    {
        if (GoalManager.Instance.goals == null || GoalManager.Instance.goals.Length == 0)
        {
            Debug.LogError("[GoalDisplay] No goals loaded. Cannot initialize assignedGoal display.");
            return;
        }

        indexGoal = networkPlayer.goalIndex.Value;
        if (indexGoal < 0 || indexGoal >= GoalManager.Instance.goals.Length)
        {
            Debug.LogError($"[GoalDisplay] Invalid goal index: {indexGoal}");
            return;
        }

        assignedGoal = GoalManager.Instance.goals[indexGoal];
        goalTitle.text = assignedGoal.title;
        goalDescription.text = assignedGoal.description;
        Debug.Log($"[GoalDisplay] Goal Display initialized for player {playerIndex} with assignedGoal {indexGoal}");

        // Fecha a tela de goal após 10 segundos e exibe a progressCard
        StartCoroutine(CloseGoalCard());
    }

    public void UpdateProgressDisplay()
    {
        if (assignedGoal == null) return;

        int curMoney = resourceManager.GetMoney(playerIndex);
        int curInfluence = resourceManager.GetInfluence(playerIndex);
        int curPeople = resourceManager.GetPeople(playerIndex);

        progressText.text = "";

        // Apenas trata goals do tipo Self; se seu goal for de outro tipo,
        // é preciso implementar outra lógica aqui
        if (assignedGoal.Target == Goal.TargetType.Self)
        {
            // Dinheiro
            if (curMoney >= assignedGoal.resources.money || assignedGoal.resources.money == 0)
                progressText.text += "Money: OK!\n";
            else
                progressText.text += $"Money: {curMoney}/{assignedGoal.resources.money}\n";

            // Pessoas
            if (curPeople >= assignedGoal.resources.people || assignedGoal.resources.people == 0)
                progressText.text += "People: OK!\n";
            else
                progressText.text += $"People: {curPeople}/{assignedGoal.resources.people}\n";

            // Influência
            if (curInfluence >= assignedGoal.resources.influence || assignedGoal.resources.influence == 0)
                progressText.text += "Influence: OK!\n";
            else
                progressText.text += $"Influence: {curInfluence}/{assignedGoal.resources.influence}\n";
        }

        Debug.Log($"[GoalDisplay] Progress updated para o Player {playerIndex}: \n{progressText.text}");
    }

    private IEnumerator CloseGoalCard()
    {
        yield return new WaitForSeconds(10f);
        goalCard.SetActive(false);
        progressCard.SetActive(true);
    }
}
