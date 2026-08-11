using System.Collections.Generic;
using UnityEngine;
using YG;
using UnityEngine.UI;
using TMPro;

public class Shop : MonoBehaviour
{
    [Header("Материалы и цены")]
    [SerializeField] private Material[] materials;    // все материалы (индекс = ID)
    [SerializeField] private int[] costs;             // стоимость каждого
    [SerializeField] private GameObject playerModel;  // модель игрока

    [Header("UI")]
    [SerializeField] private GameObject[] materialButtons; // кнопки для каждого материала
    [SerializeField] private GameObject scoreText;        // текст с очками
    [SerializeField] private GameObject notEnoughPanel;   // панель "не хватает денег"

    [Header("Реклама")]
    [SerializeField] private string rewardID = "material";

    private SkinnedMeshRenderer playerRenderer;
    private int currentSelectedIndex = 0;
    private int pendingIndex = -1; // индекс, который пытаемся купить

    void Start()
    {
        playerRenderer = playerModel.GetComponent<SkinnedMeshRenderer>();
        if (playerRenderer == null)
        {
            Debug.LogError("SkinnedMeshRenderer не найден!");
            return;
        }

        // Гасим панель
        if (notEnoughPanel != null) notEnoughPanel.SetActive(false);

        // Загружаем сохранения
        LoadData();
        ApplyMaterial(currentSelectedIndex);
        UpdateUI();
        UpdateScore();
    }

    private void OnEnable()
    {
        YG2.onRewardAdv += OnReward;
        // При повторном открытии магазина скрываем панель
        if (notEnoughPanel != null) notEnoughPanel.SetActive(false);
        pendingIndex = -1;
    }

    private void OnDisable()
    {
        YG2.onRewardAdv -= OnReward;
    }

    // ===== ЗАГРУЗКА =====
    void LoadData()
    {
        // Список купленных
        if (YG2.saves.purchasedMaterials == null)
            YG2.saves.purchasedMaterials = new List<int>();

        // Белый (индекс 0) всегда куплен
        if (!YG2.saves.purchasedMaterials.Contains(0))
            YG2.saves.purchasedMaterials.Add(0);

        // Текущий выбранный
        currentSelectedIndex = YG2.saves.selectedMaterialIndex;
        if (!YG2.saves.purchasedMaterials.Contains(currentSelectedIndex))
        {
            currentSelectedIndex = 0;
            YG2.saves.selectedMaterialIndex = 0;
        }

        // Проверка размеров массивов
        if (materials.Length != materialButtons.Length || materials.Length != costs.Length)
            Debug.LogWarning("Количество материалов, кнопок и цен должно совпадать!");
    }

    // ===== КЛИК ПО КНОПКЕ =====
    public void OnButtonClick(int index)
    {
        Debug.Log($"[Shop] Нажата кнопка с индексом {index}");

        // Проверка на выход за границы
        if (index < 0 || index >= materials.Length)
        {
            Debug.LogError($"[Shop] Индекс {index} вне диапазона!");
            return;
        }

        // Если уже куплен – просто выбираем
        if (YG2.saves.purchasedMaterials.Contains(index))
        {
            Debug.Log($"[Shop] Материал {index} уже куплен, выбираем");
            SelectMaterial(index);
            return;
        }

        // Если не куплен – пробуем купить
        int cost = costs[index];
        int playerScore = YG2.saves.playerScore;
        Debug.Log($"[Shop] Стоимость: {cost}, очков: {playerScore}");

        if (playerScore >= cost)
        {
            // Покупка за очки
            Debug.Log($"[Shop] Покупка за очки");
            YG2.saves.playerScore -= cost;
            YG2.saves.purchasedMaterials.Add(index);
            SelectMaterial(index);
            YG2.SaveProgress();
            UpdateUI();
            UpdateScore();
        }
        else
        {
            // Недостаточно средств – показываем панель
            Debug.Log($"[Shop] Недостаточно средств, показываем панель");
            pendingIndex = index;
            if (notEnoughPanel != null)
                notEnoughPanel.SetActive(true);
            else
                Debug.LogError("[Shop] notEnoughPanel не назначена!");
        }
    }

    // ===== ВЫБОР МАТЕРИАЛА =====
    void SelectMaterial(int index)
    {
        if (!YG2.saves.purchasedMaterials.Contains(index))
        {
            Debug.LogWarning($"[Shop] Попытка выбрать некупленный материал {index}");
            return;
        }

        Debug.Log($"[Shop] Выбран материал {index}");
        currentSelectedIndex = index;
        YG2.saves.selectedMaterialIndex = index;

        ApplyMaterial(index);
        UpdateUI();
        YG2.SaveProgress();
    }

    // ===== ПРИМЕНЕНИЕ =====
    void ApplyMaterial(int index)
    {
        if (playerRenderer != null && index >= 0 && index < materials.Length)
        {
            playerRenderer.material = materials[index];
            Debug.Log($"[Shop] Материал {index} применён");
        }
    }

    // ===== UI =====
    public void UpdateUI()
    {
        for (int i = 0; i < materialButtons.Length; i++)
        {
            GameObject btn = materialButtons[i];
            if (btn == null) continue;

            bool isPurchased = YG2.saves.purchasedMaterials.Contains(i);
            bool isSelected = (i == currentSelectedIndex);

            // Рамка выбранного
            Transform border = btn.transform.Find("SelectionBorder");
            if (border != null)
            {
                border.gameObject.SetActive(isSelected);
                Image borderImg = border.GetComponent<Image>();
                if (borderImg != null && isSelected)
                    borderImg.color = Color.green; // можно сделать настраиваемым
            }

            // Цена / галочка
            TMP_Text priceText = btn.transform.Find("PriceText")?.GetComponent<TMP_Text>();
            if (priceText != null)
            {
                if (isPurchased)
                    priceText.text = "";
                else
                    priceText.text = costs[i].ToString();
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

    // ===== КНОПКИ ПАНЕЛИ =====
    public void ShowAdForMaterial()
    {
        if (pendingIndex < 0)
        {
            Debug.LogWarning("[Shop] Нет ожидающего материала");
            return;
        }

        Debug.Log($"[Shop] Запуск рекламы для материала {pendingIndex}");
        if (notEnoughPanel != null)
            notEnoughPanel.SetActive(false);

        YG2.RewardedAdvShow(rewardID);
    }

    public void CloseNotEnoughPanel()
    {
        Debug.Log("[Shop] Закрытие панели (Нет)");
        if (notEnoughPanel != null)
            notEnoughPanel.SetActive(false);
        pendingIndex = -1;
    }

    // ===== НАГРАДА ЗА РЕКЛАМУ =====
    private void OnReward(string id)
    {
        Debug.Log($"[Shop] OnReward вызван с id = {id}, ожидаем {rewardID}");

        if (id != rewardID)
        {
            Debug.Log($"[Shop] ID не совпадает, игнорируем");
            return;
        }

        if (pendingIndex < 0)
        {
            Debug.LogWarning("[Shop] Нет ожидающего материала (pendingIndex < 0)");
            return;
        }

        int index = pendingIndex;
        Debug.Log($"[Shop] Выдаём материал {index} за рекламу");

        // Проверяем, не куплен ли уже (на случай двойного вызова)
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
            Debug.Log($"[Shop] Материал {index} уже был куплен, просто выбираем");
            SelectMaterial(index);
        }

        pendingIndex = -1;
        if (notEnoughPanel != null)
            notEnoughPanel.SetActive(false);
    }
}