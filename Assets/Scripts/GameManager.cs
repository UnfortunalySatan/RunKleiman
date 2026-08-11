using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;
using YG;
public class LevelGenerator : MonoBehaviour
{
    [Header("Префабы чанков")]
    [SerializeField] private GameObject[] emptyPrefabs;   // пустые (без ловушек)
    [SerializeField] private GameObject[] normalPrefabs;  // с ловушками
    [Header("Вставки")]
    private Camera mainCamera;
    private Animator camAnimator;
    [SerializeField] private GameObject playerObj;
    [Header("Настройки")]
    [SerializeField] private int startEmptyCount = 4;     // сколько пустых в начале
    [SerializeField] private int preloadChunks = 10;      // сколько всего чанков держать активными
    [SerializeField] private float segmentLength = 10f;   // длина одного чанка
    [SerializeField] private Transform player;            // ссылка на игрока
    [SerializeField] private float destroyDistance = 30f; // дистанция позади игрока для удаления
    [SerializeField] private float emptyChance = 0.2f;    // шанс (0-1) что следующий чанк будет пустым (после стартовых)

    [Header("UI")]
    [SerializeField] private GameObject pauseMenu;
    [SerializeField] private GameObject settingsMenu;
    [SerializeField] private GameObject pauseButton;
    [SerializeField] private GameObject scoreText;
    [SerializeField] private GameObject mobileButtons;
    [SerializeField] private GameObject mainMenu;
    [SerializeField] private GameObject continueUI;
    [SerializeField] private GameObject deathScreen;
    [SerializeField] private GameObject scoreUI;
    [SerializeField] private GameObject mobileButtonsRoot;
    private List<GameObject> activeChunks = new List<GameObject>();
    private Vector3 nextSpawnPos;

    // Для исключения повторов
    private GameObject lastEmptyPrefab = null;
    private GameObject lastNormalPrefab = null;

    private bool isPlay = false;

    private string device;

    

    void Start()
    {
        mobileButtons.SetActive(false);
        deathScreen.SetActive(false);
        continueUI.SetActive(false);
        mainMenu.SetActive(true);
        scoreUI.SetActive(false);
        mainCamera = Camera.main;
        camAnimator = mainCamera.GetComponent<Animator>();
        if (emptyPrefabs == null || emptyPrefabs.Length == 0 ||
            normalPrefabs == null || normalPrefabs.Length == 0)
        {
            Debug.LogError("Заполните оба массива префабов!");
            enabled = false;
            return;
        }

        nextSpawnPos = Vector3.zero;

        // Спавним пустые стартовые
        for (int i = 0; i < startEmptyCount; i++)
            SpawnChunk(true);

        // Спавним остальные до preloadChunks
        for (int i = startEmptyCount; i < preloadChunks; i++)
            SpawnChunk(false);

        Debug.Log($"Сгенерировано {activeChunks.Count} чанков (из них {startEmptyCount} пустых)");

        device = YG2.envir.deviceType;

        switch (device)
        {
            case "desktop":
                pauseButton.SetActive(false);
                mobileButtonsRoot.SetActive(false);
                break;
            case "mobile":
                pauseButton.SetActive(true);
                mobileButtonsRoot.SetActive(true);
                break;
            default:
                break;
        }
    }

    void Update()
    {
        if (isPlay)
        {
            if (player == null) return;

            float playerZ = player.position.z;

            // Удаляем чанки, которые далеко позади
            for (int i = activeChunks.Count - 1; i >= 0; i--)
            {
                GameObject chunk = activeChunks[i];
                if (chunk != null && chunk.transform.position.z < playerZ - destroyDistance)
                {
                    Destroy(chunk);
                    activeChunks.RemoveAt(i);
                }
            }

            // Дополняем активные чанки до preloadChunks
            while (activeChunks.Count < preloadChunks)
            {
                // После стартовых, следующие чанки могут быть пустыми с вероятностью emptyChance
                bool forceEmpty = false;
                // Если мы всё ещё в стартовом диапазоне, но уже создали все пустые, то больше не форсируем
                // Но чтобы не было бесконечных пустых, мы используем шанс только после startEmptyCount.
                // Однако мы уже не можем отличить, какие чанки стартовые, т.к. они уже созданы.
                // Поэтому просто используем шанс для всех новых чанков.
                bool isEmpty = Random.value < emptyChance;
                SpawnChunk(isEmpty);
            }
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Pause();
        }
    }

    void SpawnChunk(bool forceEmpty)
    {
        GameObject prefab;
        if (forceEmpty)
        {
            prefab = GetRandomPrefab(emptyPrefabs, ref lastEmptyPrefab);
        }
        else
        {
            // Если не forceEmpty, то используем normalPrefabs
            prefab = GetRandomPrefab(normalPrefabs, ref lastNormalPrefab);
        }

        if (prefab == null) return;

        // Случайный поворот: 90 или -90
        float randomY = Random.Range(0, 2) == 0 ? 90f : -90f;

        GameObject chunk = Instantiate(prefab, nextSpawnPos, Quaternion.Euler(0, randomY, 0));
        activeChunks.Add(chunk);

        nextSpawnPos.z += segmentLength;
    }

    GameObject GetRandomPrefab(GameObject[] array, ref GameObject lastUsed)
    {
        List<GameObject> available = new List<GameObject>();
        foreach (var prefab in array)
            if (prefab != null && prefab != lastUsed)
                available.Add(prefab);

        if (available.Count == 0)
            available = new List<GameObject>(array);

        available.RemoveAll(p => p == null);

        if (available.Count == 0) return null;

        GameObject chosen = available[Random.Range(0, available.Count)];
        lastUsed = chosen;
        return chosen;
    }

    // Метод для перезапуска (возрождение)
    public void ResetGenerator()
    {
        foreach (var chunk in activeChunks)
            if (chunk != null) Destroy(chunk);
        activeChunks.Clear();

        nextSpawnPos = Vector3.zero;
        lastEmptyPrefab = null;
        lastNormalPrefab = null;

        for (int i = 0; i < startEmptyCount; i++)
            SpawnChunk(true);
        for (int i = startEmptyCount; i < preloadChunks; i++)
            SpawnChunk(false);

        Debug.Log("Генератор перезапущен");
    }

    public void Play()
    {
        isPlay = true;
        camAnimator.SetTrigger("Start");
        mainMenu.SetActive(false);
        PauseGameYG.SetState(1, false, true);
        scoreText.SetActive(true);
        scoreUI.SetActive(true);
    }
    public void End()
    {
        isPlay = false;
        PauseGameYG.SetState(0, false, true);
        deathScreen.SetActive(true);
    }

    public void Pause()
    {
        pauseMenu.SetActive(true);
        scoreText.SetActive(false);
        PauseGameYG.SetState(0, false, true);
    }
    public void Continue()
    {
        isPlay = true;
        pauseMenu.SetActive(false);
        PauseGameYG.SetState(1, false, true);
        scoreText.SetActive(true);

    }

    public void AnimationEnd()
    {
        EventBus.isPlay?.Invoke();
    }

    public void MainMenu()
    {
        isPlay = false;
        ResetGenerator();
        playerObj.GetComponent<PlayerMovement>().ResetSpeed();
        deathScreen.SetActive(false);
        mainMenu.SetActive(true);
        pauseMenu.SetActive(false);
        scoreText.SetActive(false);
        scoreUI.SetActive(false);
        EventBus.isRestart?.Invoke();
        camAnimator.SetTrigger("Void");
        PauseGameYG.SetState(1, false, true);
        YG2.SetLeaderboard("Leaderboard", YG2.saves.bestRunScore);
    }

    private void OnEnable()
    {
        EventBus.isWallHit += End;
        EventBus.isContitue += ResetGenerator;
        EventBus.isPauseMenu += ContitueWithAd;
    }
    private void OnDisable()
    {
        EventBus.isWallHit -= End;
        EventBus.isContitue -= ResetGenerator;
        EventBus.isPauseMenu -= ContitueWithAd;
    }

    public void ContitueWithAd()
    {
        pauseMenu.SetActive(true);
    }

    public void InfoButton()
    {
        YG2.GetLeaderboard("Leaderboard");
    }

    
}