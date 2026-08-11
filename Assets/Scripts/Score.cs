using UnityEngine;
using TMPro;
using YG;

public class Score : MonoBehaviour
{
    [Header("UI Текст")]
    [SerializeField] private TMP_Text scoreText;     // Для текущего счета на экране
    [SerializeField] private TMP_Text bestScoreText; // Для лучшего счета в меню/на экране смерти

    private int currentLiveScore = 0;

    private void Start()
    {
        // При старте безопасно выводим лучший счет из облака Яндекса, если текст привязан
        UpdateBestScoreUI();
    }

    private void OnEnable()
    {
        // Подписываемся на событие проигрыша, чтобы зафиксировать и отправить рекорд
        EventBus.isWallHit += SaveAndUploadRecord;
    }

    private void OnDisable()
    {
        EventBus.isWallHit -= SaveAndUploadRecord;
    }

    // Этот метод вызывается из PlayerMovement.cs каждый кадр во время бега
    public void ReturnScore(int score)
    {
        currentLiveScore = score;

        // Оптимизация: Просто обновляем текст текущего счета на экране, никакой работы с сетью и YG2!
        if (scoreText != null)
        {
            scoreText.text = currentLiveScore.ToString();
        }
    }

    // Метод вызывается строго один раз при смерти игрока
    private void SaveAndUploadRecord()
    {
        // 1. Проверяем, побит ли локальный рекорд из сохранений
        if (currentLiveScore > YG2.saves.bestRunScore)
        {
            YG2.saves.bestRunScore = currentLiveScore;

            // 2. Локально обновляем текст рекорда на UI
            UpdateBestScoreUI();

            // 3. Отправляем рекорд в таблицу лидеров Яндекса (Строго 1 запрос)
            // Убедитесь, что техническое название таблицы в консоли Яндекса совпадает с "Leaderboard"
            YG2.SetLeaderboard("Leaderboard", YG2.saves.bestRunScore); // [source: 1.4.1]

            // 4. Сохраняем прогресс в облачное хранилище Яндекса
            YG2.SaveProgress();

            Debug.Log($"[Score] Новый рекорд зафиксирован и отправлен в Яндекс: {YG2.saves.bestRunScore}");
        }
        else
        {
            Debug.Log("[Score] Забег окончен. Рекорд не побит, сетевые запросы сэкономлены.");
        }
    }

    // Вспомогательный метод для обновления UI лучшего счета
    public void UpdateBestScoreUI()
    {
        if (bestScoreText != null)
        {
            bestScoreText.text = "Best: " + YG2.saves.bestRunScore.ToString();
        }
    }

    // Возвращает текущий набранный счет (нужно для прибавления денег в PlayerMovement.Restart)
    public int GetScore()
    {
        return currentLiveScore;
    }
}
