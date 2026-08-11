using UnityEngine;
using YG;
using TMPro;
using System;
using YG.Utils.LB;
public class PlayerMovement : MonoBehaviour
{
    private Rigidbody rb;
    private Animator animator;

    [Header("Параметры игрока")]
    [SerializeField] private float baseSpeed = 5f;
    [SerializeField] private float baseSideSpeed = 3f;
    [SerializeField] private float minX = -5f;
    [SerializeField] private float maxX = 5f;
    [SerializeField] private float pushForce = 20f;
    [Header("Ускорение со временем")]
    [SerializeField] private float accelerationRate = 0.1f;
    [SerializeField] private float maxSpeed = 15f;

    [Header("Вставки")]
    [SerializeField] private TMP_Text textScore;
    [SerializeField] private Transform playerPoint;
    [SerializeField] private Score score;
    private float currentSpeed;
    private float currentSideSpeed;
    private string device;
    private float mobileHor = 0f;
    private bool isRun = false;

    private float startZ;
    public float DistanceTraveled { get; private set; }
    public int DT;
    public int wasDT;
    private int totalDT;
    void Start()
    {
        startZ = playerPoint.position.z;
        isRun = false;
        animator = GetComponentInChildren<Animator>();
        rb = GetComponentInChildren<Rigidbody>();
        if (rb == null)
        {
            Debug.LogError("Rigidbody не найден!");
            return;
        }

        rb.interpolation = RigidbodyInterpolation.Interpolate;
        //rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

        device = YG2.envir.deviceType;
        Debug.Log($"Устройство: {device}");

        currentSpeed = baseSpeed;
        currentSideSpeed = baseSideSpeed;
    }

    void Update()
    {
        // Ускорение работает только если игра запущена (isRun)
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
            score.ReturnScore(totalDT);
        }
    }

    void FixedUpdate()
    {
        // ВСЕГДА читаем ввод, даже если isRun == false
        float horizontal = 0f;
        switch (device)
        {
            case "desktop":
                horizontal = Input.GetAxis("Horizontal");
                break;
            case "mobile":
                horizontal = mobileHor;
                break;
            default:
                horizontal = mobileHor;
                break;
        }

        // Отладка (проверяем, что ввод приходит)
        // Debug.Log($"horizontal: {horizontal}, isRun: {isRun}");

        // Анимация бега (только если isRun)
        if (animator != null)
        {
            bool shouldRun = isRun;
            animator.SetBool("Run", shouldRun);
        }

        // Если игра не запущена - не двигаемся
        if (!isRun)
        {
            // Можно оставить персонажа неподвижным, зануляя скорость
            rb.linearVelocity = Vector3.zero;
            return;
        }

        // Движение (только если isRun == true)
        Vector3 velocity = new Vector3(horizontal * currentSideSpeed, rb.linearVelocity.y, currentSpeed);
        rb.linearVelocity = velocity;
        // Ограничение по X
        Vector3 pos = rb.position;
        pos.x = Mathf.Clamp(pos.x, minX, maxX);
        rb.MovePosition(pos);
    }

    // Методы для UI-кнопок (мобильное управление)
    public void LeftButton() => mobileHor = -1;
    public void RightButton() => mobileHor = 1;
    public void NoClick() => mobileHor = 0;

    // Сброс скорости при возрождении
    public void ResetSpeed()
    {
        currentSpeed = baseSpeed;
        currentSideSpeed = baseSideSpeed;
    }

    public float GetCurrentSpeed() => currentSpeed;
    public float GetMaxSpeed() => maxSpeed;

    // Подписка на событие старта
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

    // Метод, который вызывается по событию
    public void Run()
    {
        isRun = true;
        Debug.Log("Игра запущена! Движение разрешено.");
    }
    public void DontRun()
    {
        isRun = false;
    }
    public void Restart()
    {
        int yDT = score.GetScore();
        YG2.saves.playerScore += yDT;
        YG2.SaveProgress();
        playerPoint.position = new Vector3(playerPoint.position.x, playerPoint.position.y, startZ);
        isRun = false;
    }

    public void Push(Vector3 direction)
    {
        rb.AddForce(new Vector3(-direction.x * pushForce, 0, -direction.z * pushForce), ForceMode.Impulse);
    }
    public void ContunueWithAd()
    {
        string id = "contitue";
        YG2.RewardedAdvShow(id, Reward);
    }

    public void Reward()
    {
        EventBus.isContitue?.Invoke();
        EventBus.isPauseMenu?.Invoke();
        playerPoint.position = new Vector3(playerPoint.position.x, playerPoint.position.y, startZ);
        isRun = true;
    }

    public void SaveTotalDT()
    {
        wasDT += DT;
    }
    public void ClearTotalDT()
    {
        wasDT = 0;
    }

    public void StartPosition()
    {
        playerPoint.position = new Vector3(0, playerPoint.position.y, startZ);
    }

    public void playerOffMove()
    {
        isRun = false;
    }
}