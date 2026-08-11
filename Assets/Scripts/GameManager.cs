using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using YG;

public class LevelGenerator : MonoBehaviour
{
    [Header("Префабы чанков")]
    [SerializeField] private GameObject[] emptyPrefabs;
    [SerializeField] private GameObject[] normalPrefabs;

    [Header("Ссылки на сцену")]
    [SerializeField] private GameObject playerObj;
    private Camera mainCamera;
    private Animator camAnimator;

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

    private List<GameObject> activeChunks = new List<GameObject>();
    private Vector3 nextSpawnPos;

    private GameObject lastEmptyPrefab = null;
    private GameObject lastNormalPrefab = null;

    private bool isPlay = false;

    // Кэшированные ссылки компонентов игрока для оптимизации
    private PlayerMovement playerMovementCache;
    private SkinnedMeshRenderer playerRendererCache;
    private HitBox playerHitBoxCache;

    void Start()
    {
        // Устанавливаем дефолтное состояние UI интерфейсов на старте
        if (deathScreen != null) deathScreen.SetActive(false);
        if (continueUI != null) continueUI.SetActive(false);
        if (mainMenu != null) mainMenu.SetActive(true);
        if (scoreUI != null) scoreUI.SetActive(false);
        if (pauseButton != null) pauseButton.SetActive(true); // Кнопка паузы доступна по клику мыши на ПК

        mainCamera = Camera.main;
        if (mainCamera != null)
            camAnimator = mainCamera.GetComponent<Animator>();

        // Сохраняем ссылки один раз для оптимизации процессора
        if (playerObj != null)
        {
            playerMovementCache = playerObj.GetComponent<PlayerMovement>();
            playerRendererCache = playerObj.GetComponentInChildren<SkinnedMeshRenderer>();
            playerHitBoxCache = playerObj.GetComponentInChildren<HitBox>();
        }

        if (emptyPrefabs == null || emptyPrefabs.Length == 0 || normalPrefabs == null || normalPrefabs.Length == 0)
        {
            Debug.LogError("[LevelGenerator] Пожалуйста, заполните оба массива префабов чанков в Инспекторе!");
            enabled = false;
            return;
        }

        ResetGeneratorState();
    }

    void Update()
    {
        if (!isPlay || player == null) return;

        float playerZ = player.position.z;

        // Удаление старых пройденных чанков позади игрока
        for (int i = activeChunks.Count - 1; i >= 0; i--)
        {
            GameObject chunk = activeChunks[i];
            if (chunk != null && chunk.transform.position.z < playerZ - destroyDistance)
            {
                Destroy(chunk);
                activeChunks.RemoveAt(i);
            }
        }

        // Спавн новых блоков перед игроком
        while (activeChunks.Count < preloadChunks)
        {
            bool isEmpty = Random.value < emptyChance;
            SpawnChunk(isEmpty);
        }

        // Вызов паузы на ПК по нажатию клавиши Esc
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Pause();
        }
    }

    void SpawnChunk(bool forceEmpty)
    {
        GameObject prefab = forceEmpty ? GetRandomPrefab(emptyPrefabs, ref lastEmptyPrefab) : GetRandomPrefab(normalPrefabs, ref lastNormalPrefab);
        if (prefab == null) return;

        float randomY = Random.Range(0, 2) == 0 ? 90f : -90f;
        GameObject chunk = Instantiate(prefab, nextSpawnPos, Quaternion.Euler(0, randomY, 0));
        activeChunks.Add(chunk);

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
        PlayerAlive();
    }

    void ResetGeneratorState()
    {
        foreach (var chunk in activeChunks)
            if (chunk != null) Destroy(chunk);
        activeChunks.Clear();

        nextSpawnPos = Vector3.zero;
        lastEmptyPrefab = null;
        lastNormalPrefab = null;

        for (int i = 0; i < startEmptyCount; i++) SpawnChunk(true);
        for (int i = startEmptyCount; i < preloadChunks; i++) SpawnChunk(false);
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
    }

    public void AnimationEnd()
    {
        EventBus.isPlay?.Invoke();
    }

    public void MainMenu()
    {
        isPlay = false;
        ResetGenerator();

        if (playerMovementCache != null)
            playerMovementCache.ResetSpeed();

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
    }

    private void OnEnable()
    {
        EventBus.isWallHit += End;
        EventBus.isContitue += ResetGenerator;
        EventBus.isPauseMenu += ContitueWithAd;
        EventBus.isCrush += PlayerCrushing;
    }

    private void OnDisable()
    {
        EventBus.isWallHit -= End;
        EventBus.isContitue -= ResetGenerator;
        EventBus.isPauseMenu -= ContitueWithAd;
        EventBus.isCrush -= PlayerCrushing;
    }

    public void ContitueWithAd()
    {
        if (pauseMenu != null) pauseMenu.SetActive(true);
    }

    public void InfoButton() => YG2.GetLeaderboard("Leaderboard");

    private void PlayerCrushing()
    {
        if (playerRendererCache != null) playerRendererCache.enabled = false;
        StartCoroutine(LateEnd());
    }

    private void PlayerAlive()
    {
        if (playerRendererCache != null) playerRendererCache.enabled = true;
        if (playerHitBoxCache != null) playerHitBoxCache.HidePlayerCrush();
        StopAllCoroutines();
    }

    private IEnumerator LateEnd()
    {
        yield return new WaitForSeconds(2f);
        End();
    }
}
