using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using YG;

public class LevelGenerator : MonoBehaviour
{
    // Структура для отслеживания чанков в пуле
    private struct ActiveChunkData
    {
        public GameObject prefabOrigin; // Какому префабу принадлежит
        public GameObject instance;     // Сам объект на сцене
    }

    [Header("Префабы чанков")]
    [SerializeField] private GameObject[] emptyPrefabs;
    [SerializeField] private GameObject[] normalPrefabs;

    [Header("Настройки генерации")]
    [SerializeField] private int startEmptyCount = 4;
    [SerializeField] private int preloadChunks = 10;
    [SerializeField] private float segmentLength = 10f;
    [SerializeField] private Transform player;
    [SerializeField] private float destroyDistance = 30f;
    [SerializeField] private float emptyChance = 0.2f;

    [Header("UI Панели (ПК)")]
    [SerializeField] private GameObject pauseMenu;
    [SerializeField] private GameObject settingsMenu;
    [SerializeField] private GameObject pauseButton;
    [SerializeField] private GameObject scoreText;
    [SerializeField] private GameObject mainMenu;
    [SerializeField] private GameObject continueUI;
    [SerializeField] private GameObject deathScreen;
    [SerializeField] private GameObject scoreUI;

    private List<ActiveChunkData> activeChunks = new List<ActiveChunkData>();
    private Vector3 nextSpawnPos;

    private GameObject lastEmptyPrefab = null;
    private GameObject lastNormalPrefab = null;
    private ChunkPool chunkPool;
    private bool isPlay = false;

    void Start()
    {
        // Создаем или находим компонент пула объектов
        chunkPool = gameObject.GetComponent<ChunkPool>();
        if (chunkPool == null) chunkPool = gameObject.AddComponent<ChunkPool>();

        if (deathScreen != null) deathScreen.SetActive(false);
        if (continueUI != null) continueUI.SetActive(false);
        if (mainMenu != null) mainMenu.SetActive(true);
        if (scoreUI != null) scoreUI.SetActive(false);
        if (pauseButton != null) pauseButton.SetActive(true);

        Camera mainCamera = Camera.main;
        if (mainCamera != null)
            camAnimator = mainCamera.GetComponent<Animator>();

        if (emptyPrefabs == null || emptyPrefabs.Length == 0 || normalPrefabs == null || normalPrefabs.Length == 0)
        {
            Debug.LogError("[LevelGenerator] Массивы префабов не заполнены!");
            enabled = false;
            return;
        }

        ResetGeneratorState();
    }

    private Animator camAnimator;

    void Update()
    {
        if (!isPlay || player == null) return;

        float playerZ = player.position.z;

        // Вместо Destroy возвращаем чанки в пул объектов
        for (int i = activeChunks.Count - 1; i >= 0; i--)
        {
            ActiveChunkData chunkData = activeChunks[i];
            if (chunkData.instance != null && chunkData.instance.transform.position.z < playerZ - destroyDistance)
            {
                chunkPool.ReturnChunk(chunkData.prefabOrigin, chunkData.instance);
                activeChunks.RemoveAt(i);
            }
        }

        while (activeChunks.Count < preloadChunks)
        {
            bool isEmpty = Random.value < emptyChance;
            SpawnChunk(isEmpty);
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Pause();
        }
    }

    void SpawnChunk(bool forceEmpty)
    {
        GameObject prefabOrigin = forceEmpty ? GetRandomPrefab(emptyPrefabs, ref lastEmptyPrefab) : GetRandomPrefab(normalPrefabs, ref lastNormalPrefab);
        if (prefabOrigin == null) return;

        float randomY = Random.Range(0, 2) == 0 ? 90f : -90f;

        // Запрашиваем объект из пула вместо создания
        GameObject chunkInstance = chunkPool.GetChunk(prefabOrigin, nextSpawnPos, Quaternion.Euler(0, randomY, 0));

        ActiveChunkData data = new ActiveChunkData
        {
            prefabOrigin = prefabOrigin,
            instance = chunkInstance
        };

        activeChunks.Add(data);
        nextSpawnPos.z += segmentLength;
    }

    GameObject GetRandomPrefab(GameObject[] array, ref GameObject lastUsed)
    {
        List<GameObject> available = new List<GameObject>();
        foreach (var prefab in array)
            if (prefab != null && prefab != lastUsed) available.Add(prefab);

        if (available.Count == 0) available = new List<GameObject>(array);
        available.RemoveAll(p => p == null);

        if (available.Count == 0) return null;

        GameObject chosen = available[Random.Range(0, available.Count)];
        lastUsed = chosen;
        return chosen;
    }

    public void ResetGenerator()
    {
        ResetGeneratorState();
        EventBus.isContitue?.Invoke(); // Оповещаем другие скрипты через шину событий
    }

    void ResetGeneratorState()
    {
        // Возвращаем все активные чанки обратно в пул
        foreach (var chunkData in activeChunks)
        {
            if (chunkData.instance != null)
                chunkPool.ReturnChunk(chunkData.prefabOrigin, chunkData.instance);
        }
        activeChunks.Clear();

        nextSpawnPos = Vector3.zero;
        lastEmptyPrefab = null;
        lastNormalPrefab = null;

        for (int i = 0; i < startEmptyCount; i++) SpawnChunk(true);
        for (int i = startEmptyCount; i < preloadChunks; i++) SpawnChunk(false);

        ClearUIFocus();
    }

    public void Play()
    {
        isPlay = true;
        if (camAnimator != null)
        {
            camAnimator.ResetTrigger("Void");
            camAnimator.SetTrigger("Start");
        }
        if (mainMenu != null) mainMenu.SetActive(false);
        PauseGameYG.SetState(1, false, true);
        if (scoreText != null) scoreText.SetActive(true);
        if (scoreUI != null) scoreUI.SetActive(true);

        ClearUIFocus();
    }

    public void End()
    {
        isPlay = false;
        PauseGameYG.SetState(0, false, true);
        if (deathScreen != null) deathScreen.SetActive(true);
    }

    public void Pause()
    {
        if (pauseMenu != null) pauseMenu.SetActive(true);
        if (scoreText != null) scoreText.SetActive(false);
        PauseGameYG.SetState(0, false, true);
    }

    public void Continue()
    {
        isPlay = true;
        if (pauseMenu != null) pauseMenu.SetActive(false);
        PauseGameYG.SetState(1, false, true);
        if (scoreText != null) scoreText.SetActive(true);

        ClearUIFocus();
    }

    public void AnimationEnd()
    {
        EventBus.isPlay?.Invoke();
    }

    public void MainMenu()
    {
        isPlay = false;

        // При выходе в главное меню полностью зачищаем пул из памяти, чтобы освободить WebGL кэш
        if (chunkPool != null) chunkPool.ClearAllPools();
        ResetGenerator();

        if (deathScreen != null) deathScreen.SetActive(false);
        if (mainMenu != null) mainMenu.SetActive(true);
        if (pauseMenu != null) pauseMenu.SetActive(false);
        if (scoreText != null) scoreText.SetActive(false);
        if (scoreUI != null) scoreUI.SetActive(false);

        EventBus.isRestart?.Invoke();

        if (camAnimator != null)
        {
            camAnimator.ResetTrigger("Start");
            camAnimator.Play("Void", 0, 0f);
        }

        PauseGameYG.SetState(1, false, true);
        YG2.SetLeaderboard("Leaderboard", YG2.saves.bestRunScore);

        ClearUIFocus();
    }

    private void ClearUIFocus()
    {
        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
        }
    }

    private void OnEnable()
    {
        EventBus.isWallHit += End;
    }

    private void OnDisable()
    {
        EventBus.isWallHit -= End;
    }
    public void ContitueWithAd()
    {
        // 1. Сначала принудительно возвращаем игру из глубокой заморозки Яндекса,
        // чтобы WebGL-контекст Unity снова начал обновлять графику UI
        PauseGameYG.SetState(1, false, true);

        // 2. Включаем само окно меню паузы
        if (pauseMenu != null)
        {
            pauseMenu.SetActive(true);
        }

        // 3. Скрываем текст очков на время паузы, как это делается в обычном методе Pause()
        if (scoreText != null)
        {
            scoreText.SetActive(false);
        }

        // 4. Очищаем залипший фокус кнопок
        ClearUIFocus();
    }


    public void InfoButton() => YG2.GetLeaderboard("Leaderboard");

}
