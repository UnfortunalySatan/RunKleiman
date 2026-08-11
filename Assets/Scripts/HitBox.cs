using System.Collections;
using UnityEngine;

public class HitBox : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private GameObject playerSharpsPrefab;
    private PlayerMovement playerMovement;
    private Rigidbody parentRb;
    private GameObject createdSharps;

    private bool isCrushed = false;
    private Coroutine spawnSharpsCoroutine;

    private void Start()
    {
        playerMovement = GetComponentInParent<PlayerMovement>();
        parentRb = GetComponentInParent<Rigidbody>();
    }

    private void OnEnable()
    {
        EventBus.isContitue += HidePlayerCrush;
        EventBus.isRestart += HidePlayerCrush;
    }

    private void OnDisable()
    {
        EventBus.isContitue -= HidePlayerCrush;
        EventBus.isRestart -= HidePlayerCrush;
    }

    private Vector3 GetPosition()
    {
        return transform.position;
    }

    private IEnumerator SpawnSharpsRoutine()
    {
        if (createdSharps != null)
        {
            Destroy(createdSharps);
        }

        // 1. Спавним осколки
        if (playerSharpsPrefab != null)
        {
            createdSharps = Instantiate(playerSharpsPrefab);
            createdSharps.transform.localScale = new Vector3(0.33f, 0.33f, 0.33f);
            createdSharps.transform.position = GetPosition();
        }

        yield return null;

        // 2. Ждем 2 секунды игрового времени, чтобы игрок насладился визуалом разлета осколков
        yield return new WaitForSeconds(2f);

        // 3. Жестко и гарантированно вызываем экран смерти через шину событий
        EventBus.isWallHit?.Invoke();

        spawnSharpsCoroutine = null;
    }

    private void PlayerCrush()
    {
        if (isCrushed) return;

        if (playerMovement != null && playerMovement.CheckInvulnerable())
            return;

        isCrushed = true;

        // Мгновенно останавливаем физическое тело
        if (parentRb != null) parentRb.linearVelocity = Vector3.zero;

        // Оповещаем PlayerMovement, чтобы он остановил бег и скрыл видимость оригинальной модельки
        EventBus.isCrush?.Invoke();

        // Запускаем безопасную цепочку: спавн -> ожидание 2 сек -> экран смерти
        spawnSharpsCoroutine = StartCoroutine(SpawnSharpsRoutine());
    }

    public void HidePlayerCrush()
    {
        if (spawnSharpsCoroutine != null)
        {
            StopCoroutine(spawnSharpsCoroutine);
            spawnSharpsCoroutine = null;
        }

        isCrushed = false;

        if (createdSharps != null)
        {
            Destroy(createdSharps);
            createdSharps = null;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (isCrushed || (playerMovement != null && playerMovement.CheckInvulnerable()))
            return;

        if (collision.gameObject.CompareTag("Colotun"))
        {
            PlayerCrush();
        }
        if (collision.gameObject.CompareTag("Wall") || collision.gameObject.CompareTag("Plush"))
        {
            if (animator != null) animator.SetTrigger("Hit");
        }
    }
}
