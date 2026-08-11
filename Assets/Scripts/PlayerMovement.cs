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
    [SerializeField] private float jumpForce = 6.5f;           // Сила прыжка вверх
    [SerializeField] private float groundCheckRadius = 0.25f;    // Радиус сферы проверки земли под ногами
    [SerializeField] private Vector3 groundCheckOffset = new Vector3(0, -0.05f, 0); // Смещение сферы вниз под ноги
    [SerializeField] private LayerMask groundLayer;            // Слой земли

    [Header("Ускорение со временем")]
    [SerializeField] private float accelerationRate = 0.05f;
    [SerializeField] private float maxSpeed = 15f;

    [Header("Ссылки на компоненты")]
    [SerializeField] private Transform playerPoint;
    [SerializeField] private Score score;
    [SerializeField] private GameObject playerModel; // Ссылка на модельку игрока (чтобы скрывать при смерти)

    private float currentSpeed;
    private float currentSideSpeed;
    private bool isRun = false;
    private bool isGrounded = true;

    private float startZ;
    public float DistanceTraveled { get; private set; }
    public int DT;
    public int wasDT;
    private int totalDT;

    public bool IsRecoveringFromAd { get; private set; } = false;
    private SkinnedMeshRenderer playerRenderer;

    void Start()
    {
        startZ = playerPoint.position.z;
        isRun = false;

        animator = GetComponentInChildren<Animator>();
        rb = GetComponentInChildren<Rigidbody>();

        if (playerModel != null)
            playerRenderer = playerModel.GetComponent<SkinnedMeshRenderer>();

        if (rb == null)
        {
            Debug.LogError("[PlayerMovement] Компонент Rigidbody не найден на дочерних объектах!");
            return;
        }

        // Включаем интерполяцию для идеального сглаживания движения камеры Cinemachine
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationY | RigidbodyConstraints.FreezeRotationZ;

        currentSpeed = baseSpeed;
        currentSideSpeed = baseSideSpeed;
    }

    void Update()
    {
        // Проверка земли через сферу
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
            if (rb != null) rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0);
            return;
        }

        float horizontal = Input.GetAxis("Horizontal");

        // Возвращаем плавное движение через linearVelocity (Решает проблему дерганья камеры)
        Vector3 velocity = new Vector3(horizontal * currentSideSpeed, rb.linearVelocity.y, currentSpeed);
        rb.linearVelocity = velocity;

        // Ограничение по бокам трассы
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
        EventBus.isCrush += OnPlayerCrush;   // Слушаем событие разрушения от крутилки
        EventBus.isContitue += OnPlayerAlive; // Слушаем возрождение
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

    // Логика разрушения игрока
    private void OnPlayerCrush()
    {
        isRun = false;
        if (playerRenderer != null) playerRenderer.enabled = false; // Прячем модельку
        StartCoroutine(LateEndRoutine());
    }

    // Логика восстановления игрока (при старте или рестарте)
    private void OnPlayerAlive()
    {
        if (playerRenderer != null) playerRenderer.enabled = true; // Возвращаем модельку
        StopAllCoroutines();
    }

    private System.Collections.IEnumerator LateEndRoutine()
    {
        // Даем 2 секунды посмотреть на разлетающиеся осколки
        yield return new WaitForSeconds(2f);
        EventBus.isWallHit?.Invoke(); // Вызываем появление экрана смерти в LevelGenerator
    }

    public void Restart()
    {
        if (score != null)
        {
            int yDT = score.GetScore();
            YG2.saves.playerScore += yDT;
        }

        OnPlayerAlive(); // Гарантируем, что моделька станет видимой при перезапуске
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

        yield return new WaitForSecondsRealtime(0.2f);
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
