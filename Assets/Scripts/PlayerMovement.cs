using UnityEngine;
using YG;
using TMPro;

public class PlayerMovement : MonoBehaviour
{
    private Rigidbody rb;
    private Animator animator;

    [Header("Параметры бега (ПК)")]
    [SerializeField] private float baseSpeed = 5f;
    [SerializeField] private float baseSideSpeed = 3f;
    [SerializeField] private float minX = -5f;
    [SerializeField] private float maxX = 5f;
    [SerializeField] private float pushForce = 20f;

    [Header("Параметры прыжка")]
    [SerializeField] private float jumpForce = 6.5f;
    [SerializeField] private float groundCheckRadius = 0.25f;
    [SerializeField] private Vector3 groundCheckOffset = new Vector3(0, -0.05f, 0);
    [SerializeField] private LayerMask groundLayer;

    [Header("Ускорение со временем")]
    [SerializeField] private float accelerationRate = 0.05f;
    [SerializeField] private float maxSpeed = 15f;

    [Header("Ссылки на компоненты")]
    [SerializeField] private Transform playerPoint;
    [SerializeField] private Score score;
    [SerializeField] private GameObject playerModel;

    private float currentSpeed;
    private float currentSideSpeed;
    private bool isRun = false;
    private bool isGrounded = true;
    private bool isInvulnerable = false;

    private float startZ;
    public float DistanceTraveled { get; private set; }
    public int DT;
    public int wasDT;
    private int totalDT;

    public bool IsRecoveringFromAd { get; private set; } = false;
    private Renderer[] playerRenderers;

    void Start()
    {
        startZ = playerPoint.position.z;
        isRun = false;

        animator = GetComponentInChildren<Animator>();
        rb = GetComponentInChildren<Rigidbody>();

        if (playerModel != null)
        {
            playerRenderers = playerModel.GetComponentsInChildren<Renderer>(true);
        }

        if (rb == null)
        {
            Debug.LogError("[PlayerMovement] Компонент Rigidbody не найден на дочерних объектах!");
            return;
        }

        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationY | RigidbodyConstraints.FreezeRotationZ;

        currentSpeed = baseSpeed;
        currentSideSpeed = baseSideSpeed;
    }

    void Update()
    {
        Vector3 sphereCenter = playerPoint.position + groundCheckOffset;
        isGrounded = Physics.CheckSphere(sphereCenter, groundCheckRadius, groundLayer);

        if (isRun && isGrounded && (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow)))
        {
            Jump();
        }

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
            animator.SetBool("Grounded", isGrounded);
        }

        if (!isRun)
        {
            if (rb != null) rb.linearVelocity = Vector3.zero;
            return;
        }

        float horizontal = Input.GetAxis("Horizontal");
        float verticalVelocity = isGrounded && rb.linearVelocity.y < 0.1f ? 0f : rb.linearVelocity.y;

        Vector3 velocity = new Vector3(horizontal * currentSideSpeed, verticalVelocity, currentSpeed);
        rb.linearVelocity = velocity;

        Vector3 pos = rb.position;
        pos.x = Mathf.Clamp(pos.x, minX, maxX);
        rb.position = pos;
    }

    private void Jump()
    {
        if (rb != null)
        {
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
            rb.AddForce(Vector3.up * jumpForce, ForceMode.VelocityChange);

            if (animator != null)
            {
                animator.SetTrigger("Jump");
            }
        }
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
        EventBus.isCrush += OnPlayerCrush;
        EventBus.isContitue += OnPlayerAlive;
    }

    private void OnDisable()
    {
        EventBus.isPlay -= Run;
        EventBus.isRestart -= Restart;
        EventBus.isCrush -= OnPlayerCrush;
        EventBus.isContitue -= OnPlayerAlive;
    }

    public void Run() => isRun = true;
    public void DontRun() => isRun = false;

    public bool CheckInvulnerable() => isInvulnerable;

    private void OnPlayerCrush()
    {
        isRun = false;
        if (rb != null) rb.linearVelocity = Vector3.zero;
        SetModelVisibility(false);
        StartCoroutine(LateEndRoutine());
    }

    private void OnPlayerAlive()
    {
        SetModelVisibility(true);
        StopAllCoroutines();
        // Используем Realtime корутину для бессмертия, чтобы она работала на паузе Яндекса
        StartCoroutine(InvulnerabilityRealtimeRoutine());
    }

    private void SetModelVisibility(bool visible)
    {
        if (playerRenderers != null)
        {
            foreach (var ren in playerRenderers)
            {
                if (ren != null) ren.enabled = visible;
            }
        }
    }

    private System.Collections.IEnumerator InvulnerabilityRealtimeRoutine()
    {
        isInvulnerable = true;
        yield return new WaitForSecondsRealtime(0.5f); // Игнорирует Time.timeScale = 0
        isInvulnerable = false;
    }

    private System.Collections.IEnumerator LateEndRoutine()
    {
        yield return new WaitForSeconds(2f);
        EventBus.isWallHit?.Invoke();
    }

    public void Restart()
    {
        if (score != null)
        {
            int yDT = score.GetScore();
            YG2.saves.playerScore += yDT;
        }

        OnPlayerAlive();
        TeleportToStart();
        isRun = false;
    }

    private void TeleportToStart()
    {
        if (rb != null)
        {
            // 1. Полностью гасим любые накопленные силы и скорости Rigidbody
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;

            // 2. Временно отключаем интерполяцию, чтобы сбросить кэш физики Unity
            rb.interpolation = RigidbodyInterpolation.None;
        }

        // 3. Перемещаем физическое тело и трансформ игрока в стартовую точку
        Vector3 targetStartPos = new Vector3(0f, playerPoint.position.y, startZ);
        playerPoint.position = targetStartPos;
        if (rb != null)
        {
            rb.position = targetStartPos;
        }

        // 4. Жестко приказываем Unity синхронизировать физические координаты прямо в этом кадре
        Physics.SyncTransforms();

        // 5. Мгновенно телепортируем камеру Cinemachine за игроком, чтобы она не "летела" через всю карту
        // Метод автоматически находит активную виртуальную камеру и перемещает её без сглаживания
        var vCam = FindFirstObjectByType<Unity.Cinemachine.CinemachineCamera>();
        if (vCam != null)
        {
            // Сообщаем Cinemachine, что объект совершил пространственный скачок (Warp)
            vCam.OnTargetObjectWarped(playerPoint, targetStartPos - playerPoint.position);
        }

        // 6. Возвращаем плавную интерполяцию Rigidbody обратно для плавного бега
        if (rb != null)
        {
            rb.interpolation = RigidbodyInterpolation.Interpolate;
        }

        Debug.Log("[PlayerMovement] Безопасная телепортация игрока и Cinemachine на старт успешно выполнена.");
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
        YG2.RewardedAdvShow("revive_player", Reward);
    }

    public void Reward()
    {
        // Запускаем очистку и респавн через Realtime-корутину, чтобы Unity проснулся
        StartCoroutine(SafeRewardRoutine());
    }

    private System.Collections.IEnumerator SafeRewardRoutine()
    {
        IsRecoveringFromAd = true;

        // 1. Возвращаем игрока в начало и убираем осколки
        TeleportToStart();
        EventBus.isContitue?.Invoke();
        isRun = false; // Игрок НЕ бежит сам по себе сразу после рекламы

        // 2. Даем физическому движку 0.1 сек системного времени, чтобы пул чанков перестроился на позиции (0,0,0)
        yield return new WaitForSecondsRealtime(0.1f);

        // 3. Только ТЕПЕРЬ, когда уровень готов, сообщаем LevelGenerator открыть меню паузы
        EventBus.isPauseMenu?.Invoke();

        yield return new WaitForSecondsRealtime(0.1f);
        IsRecoveringFromAd = false;
    }

    public void SaveTotalDT() => wasDT += DT;
    public void ClearTotalDT() => wasDT = 0;
    public void StartPosition() => TeleportToStart();
    public void playerOffMove() => isRun = false;

    private void OnDrawGizmosSelected()
    {
        if (playerPoint != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(playerPoint.position + groundCheckOffset, groundCheckRadius);
        }
    }
}
