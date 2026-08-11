using System.Collections.Generic;
using UnityEngine;
using YG;
using UnityEngine.UI;
using TMPro;

public class Shop : MonoBehaviour
{
    [Header("Материалы и цены")]
    [SerializeField] private Material[] materials;
    [SerializeField] private int[] costs;
    [SerializeField] private GameObject playerModel;

    [Header("UI")]
    [SerializeField] private GameObject[] materialButtons;
    [SerializeField] private GameObject scoreText;
    [SerializeField] private GameObject notEnoughPanel;

    [Header("Реклама")]
    [SerializeField] private string rewardID = "material";

    private SkinnedMeshRenderer playerRenderer;
    private int currentSelectedIndex = 0;
    private int pendingIndex = -1;

    void Start()
    {
        playerRenderer = playerModel.GetComponent<SkinnedMeshRenderer>();
        if (playerRenderer == null)
        {
            Debug.LogError("[Shop] SkinnedMeshRenderer не найден на модели игрока!");
            return;
        }

        if (notEnoughPanel != null) notEnoughPanel.SetActive(false);

        // Безопасная WebGL загрузка: ждем SDK Яндекса
        if (YG2.isSDKEnabled)
        {
            InitializeShop();
        }
        else
        {
            YG2.onGetSDKData += InitializeShop;
        }
    }

    private void OnEnable()
    {
        YG2.onRewardAdv += OnReward;
        if (notEnoughPanel != null) notEnoughPanel.SetActive(false);
        pendingIndex = -1;
    }

    private void OnDisable()
    {
        YG2.onRewardAdv -= OnReward;
    }

    private void OnDestroy()
    {
        YG2.onGetSDKData -= InitializeShop;
    }

    void InitializeShop()
    {
        LoadData();
        ApplyMaterial(currentSelectedIndex);
        UpdateUI();
        UpdateScore();

        // Автоматически обновляем аудио-клики на кнопках магазина, если они были созданы позже
        SoundManager soundManager = FindAnyObjectByType<SoundManager>();
        if (soundManager != null) soundManager.RefreshButtonListeners();
    }

    void LoadData()
    {
        if (YG2.saves.purchasedMaterials == null)
            YG2.saves.purchasedMaterials = new List<int>();

        if (!YG2.saves.purchasedMaterials.Contains(0))
            YG2.saves.purchasedMaterials.Add(0);

        currentSelectedIndex = YG2.saves.selectedMaterialIndex;
        if (!YG2.saves.purchasedMaterials.Contains(currentSelectedIndex))
        {
            currentSelectedIndex = 0;
            YG2.saves.selectedMaterialIndex = 0;
        }

        if (materials.Length != materialButtons.Length || materials.Length != costs.Length)
            Debug.LogWarning("[Shop] Длины массивов материалов, кнопок и цен не совпадают в инспекторе!");
    }

    public void OnButtonClick(int index)
    {
        if (index < 0 || index >= materials.Length) return;

        if (YG2.saves.purchasedMaterials.Contains(index))
        {
            SelectMaterial(index);
            return;
        }

        int cost = costs[index];
        int playerScore = YG2.saves.playerScore;

        if (playerScore >= cost)
        {
            YG2.saves.playerScore -= cost;
            YG2.saves.purchasedMaterials.Add(index);
            SelectMaterial(index);
            YG2.SaveProgress();
            UpdateUI();
            UpdateScore();
        }
        else
        {
            pendingIndex = index;
            if (notEnoughPanel != null) notEnoughPanel.SetActive(true);
        }
    }

    void SelectMaterial(int index)
    {
        if (!YG2.saves.purchasedMaterials.Contains(index)) return;

        currentSelectedIndex = index;
        YG2.saves.selectedMaterialIndex = index;

        ApplyMaterial(index);
        UpdateUI();
        YG2.SaveProgress();
    }

    void ApplyMaterial(int index)
    {
        if (playerRenderer != null && index >= 0 && index < materials.Length)
        {
            playerRenderer.material = materials[index];
        }
    }

    public void UpdateUI()
    {
        for (int i = 0; i < materialButtons.Length; i++)
        {
            GameObject btn = materialButtons[i];
            if (btn == null) continue;

            bool isPurchased = YG2.saves.purchasedMaterials.Contains(i);
            bool isSelected = (i == currentSelectedIndex);

            Transform border = btn.transform.Find("SelectionBorder");
            if (border != null)
            {
                border.gameObject.SetActive(isSelected);
            }

            TMP_Text priceText = btn.transform.Find("PriceText")?.GetComponent<TMP_Text>();
            if (priceText != null)
            {
                priceText.text = isPurchased ? "" : costs[i].ToString();
            }
        }
    }

    public void UpdateScore()
    {
        if (scoreText != null)
        {
            TMP_Text txt = scoreText.GetComponent<TMP_Text>();
            if (txt != null)
                txt.text = YG2.saves.playerScore.ToString();
        }
    }

    public void ShowAdForMaterial()
    {
        if (pendingIndex < 0) return;

        if (notEnoughPanel != null) notEnoughPanel.SetActive(false);
        YG2.RewardedAdvShow(rewardID);
    }

    public void CloseNotEnoughPanel()
    {
        if (notEnoughPanel != null) notEnoughPanel.SetActive(false);
        pendingIndex = -1;
    }

    private void OnReward(string id)
    {
        if (id != rewardID || pendingIndex < 0) return;

        int index = pendingIndex;

        if (!YG2.saves.purchasedMaterials.Contains(index))
        {
            YG2.saves.purchasedMaterials.Add(index);
            SelectMaterial(index);
            YG2.SaveProgress();
            UpdateUI();
            UpdateScore();
        }
        else
        {
            SelectMaterial(index);
        }

        pendingIndex = -1;
        if (notEnoughPanel != null) notEnoughPanel.SetActive(false);
    }
}
