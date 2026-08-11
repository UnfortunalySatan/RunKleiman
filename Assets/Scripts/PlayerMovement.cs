using UnityEngine;
using YG;
using TMPro;

public class PlayerMovement : MonoBehaviour
{
    private Rigidbody rb;
    private Animator animator;

    [Header("Параметры игрока (ПК)")]
    [SerializeField] private float baseSpeed = 5f;
    [SerializeField] private float baseSideSpeed = 3f;
    [SerializeField] private float minX = -5f;
    [SerializeField] private float maxX = 5f;
    [SerializeField] private float pushForce = 20f;

    [Header("Ускорение со временем")]
    [SerializeField] private float accelerationRate = 0.1f;
    [SerializeField] private float maxSpeed = 15f;

    [Header("Ссылки на компоненты")]
    [SerializeField] private Transform playerPoint;
    [SerializeField] private Score score;

    private float currentSpeed;
    private float currentSideSpeed;
    private bool isRun = false;

    private float startZ;
    public float DistanceTraveled { get; private set; }
    public int DT;
    public int wasDT;
    private int totalDT;

    // Свойство для защиты других менеджеров от ложных кликов после рекламы
    public bool IsRecoveringFromAd { get; private set; } = false;

    void Start()
    {
        startZ = playerPoint.position.z;
        isRun = false;

        animator = GetComponentInChildren<Animator>();
        rb = GetComponentInChildren<Rigidbody>();

        if (rb == null)
        {
            Debug.LogError("[PlayerMovement] Компонент Rigidbody не найден на дочерних объектах!");
            return;
        }

        rb.interpolation = RigidbodyInterpolation.Interpolate;

        currentSpeed = baseSpeed;
        currentSideSpeed = baseSideSpeed;
    }

    void Update()
    {
        if (isRun && currentSpeed < maxSpeed)
        {
            currentSpeed += accelerationRate * Time.deltaTime;
            currentSideSpeed += accelerationRate * Time.deltaTime * 0.5f;
            currentSpeed = Mathf.Min(currentSpeed, maxSpeed);
            currentSideSpeed = Mathf.Min(currentSideSpeed, maxSpeed * 0.5f);
        }

        if (isRun)
        {
            DistanceTraveled = playerPoint.position.z - startZ;
            DT = Mathf.RoundToInt(DistanceTraveled);
            totalDT = DT + wasDT;

            if (score != null)
                score.ReturnScore(totalDT);
        }
    }

    void FixedUpdate()
    {
        if (animator != null)
        {
            animator.SetBool("Run", isRun);
        }

        if (!isRun)
        {
            if (rb != null) rb.linearVelocity = Vector3.zero;
            return;
        }

        float horizontal = Input.GetAxis("Horizontal");

        Vector3 velocity = new Vector3(horizontal * currentSideSpeed, rb.linearVelocity.y, currentSpeed);
        rb.linearVelocity = velocity;

        Vector3 pos = rb.position;
        pos.x = Mathf.Clamp(pos.x, minX, maxX);
        rb.MovePosition(pos);
    }

    public void ResetSpeed()
    {
        currentSpeed = baseSpeed;
        currentSideSpeed = baseSideSpeed;
    }

    private void OnEnable()
    {
        EventBus.isPlay += Run;
        EventBus.isRestart += Restart;
    }

    private void OnDisable()
    {
        EventBus.isPlay -= Run;
        EventBus.isRestart -= Restart;
    }

    public void Run()
    {
        isRun = true;
    }

    public void DontRun()
    {
        isRun = false;
    }

    public void Restart()
    {
        if (score != null)
        {
            int yDT = score.GetScore();
            YG2.saves.playerScore += yDT;
        }

        TeleportToStart();
        isRun = false;
    }

    private void TeleportToStart()
    {
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
        playerPoint.position = new Vector3(0f, playerPoint.position.y, startZ);
    }

    public void Push(Vector3 direction)
    {
        if (rb != null)
        {
            rb.AddForce(new Vector3(-direction.x * pushForce, 0, -direction.z * pushForce), ForceMode.Impulse);
        }
    }

    public void ContunueWithAd()
    {
        // Использование уникального ID для исключения конфликтов с магазином
        YG2.RewardedAdvShow("revive_player", Reward);
    }

    public void Reward()
    {
        StartCoroutine(SafeRewardRoutine());
    }

    private System.Collections.IEnumerator SafeRewardRoutine()
    {
        IsRecoveringFromAd = true;

        EventBus.isContitue?.Invoke();
        EventBus.isPauseMenu?.Invoke();
        TeleportToStart();
        isRun = true;

        // Пауза 0.2 секунды реального времени для игнорирования кликов EventSystem браузера
        yield return new WaitForSecondsRealtime(0.2f);
        IsRecoveringFromAd = false;
    }

    public void SaveTotalDT() => wasDT += DT;
    public void ClearTotalDT() => wasDT = 0;
    public void StartPosition() => TeleportToStart();
    public void playerOffMove() => isRun = false;
}
