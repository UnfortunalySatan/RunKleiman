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
    [SerializeField] private float jumpForce = 7f;         // Сила толчка вверх
    [SerializeField] private float groundCheckDistance = 0.2f; // Длина луча проверки земли под ногами
    [SerializeField] private LayerMask groundLayer;        // Слой, который считается землей (например, Default)

    [Header("Ускорение со временем")]
    [SerializeField] private float accelerationRate = 0.1f;
    [SerializeField] private float maxSpeed = 15f;

    [Header("Ссылки на компоненты")]
    [SerializeField] private Transform playerPoint;
    [SerializeField] private Score score;

    private float currentSpeed;
    private float currentSideSpeed;
    private bool isRun = false;
    private bool isGrounded = true; // Находится ли игрок на земле прямо сейчас

    private float startZ;
    public float DistanceTraveled { get; private set; }
    public int DT;
    public int wasDT;
    private int totalDT;

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

        // Включаем интерполяцию для идеального сглаживания движения камеры Cinemachine
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        currentSpeed = baseSpeed;
        currentSideSpeed = baseSideSpeed;
    }

    void Update()
    {
        // 1. Проверка нахождения на земле (пускаем луч из центра игрока чуть выше его ног строго вниз)
        isGrounded = Physics.Raycast(playerPoint.position + Vector3.up * 0.1f, Vector3.down, groundCheckDistance + 0.1f, groundLayer);

        // Рисуем этот луч в окне сцены Unity: зеленый — стоим на земле, красный — в воздухе
        Debug.DrawRay(playerPoint.position + Vector3.up * 0.1f, Vector3.down * (groundCheckDistance + 0.1f), isGrounded ? Color.green : Color.red);

        // 2. Обработка ввода прыжка (только если игра идет и персонаж касается земли)
        if (isRun && isGrounded && (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow)))
        {
            Jump();
        }

        // 3. Постепенное увеличение скорости
        if (isRun && currentSpeed < maxSpeed)
        {
            currentSpeed += accelerationRate * Time.deltaTime;
            currentSideSpeed += accelerationRate * Time.deltaTime * 0.5f;
            currentSpeed = Mathf.Min(currentSpeed, maxSpeed);
            currentSideSpeed = Mathf.Min(currentSideSpeed, maxSpeed * 0.5f);
        }

        // 4. Расчет пройденной дистанции
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
            animator.SetBool("Grounded", isGrounded); // Передаем состояние земли в аниматор для будущих переходов
        }

        if (!isRun)
        {
            if (rb != null) rb.linearVelocity = Vector3.zero;
            return;
        }

        // Управление боковым смещением (A/D или стрелочки влево/вправо)
        float horizontal = Input.GetAxis("Horizontal");

        // Сохраняем текущую скорость по Y (rb.linearVelocity.y), чтобы гравитация и прыжок работали корректно
        Vector3 velocity = new Vector3(horizontal * currentSideSpeed, rb.linearVelocity.y, currentSpeed);
        rb.linearVelocity = velocity;

        // Ограничение по краям трассы
        Vector3 pos = rb.position;
        pos.x = Mathf.Clamp(pos.x, minX, maxX);
        rb.MovePosition(pos);
    }

    private void Jump()
    {
        if (rb != null)
        {
            // Перед импульсом обнуляем вертикальную скорость, чтобы прыжок всегда был одинаковой высоты
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
            rb.AddForce(Vector3.up * jumpForce, ForceMode.VelocityChange);

            if (animator != null)
            {
                animator.SetTrigger("Jump"); // Запускаем триггер анимации прыжка
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
    }

    private void OnDisable()
    {
        EventBus.isPlay -= Run;
        EventBus.isRestart -= Restart;
    }

    public void Run() => isRun = true;
    public void DontRun() => isRun = false;

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
}
