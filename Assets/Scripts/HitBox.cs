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
    private bool blockCollisionsImmediately = false; // Потоковый фикс ложных коллизий
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

        if (playerSharpsPrefab != null)
        {
            createdSharps = Instantiate(playerSharpsPrefab);
            createdSharps.transform.localScale = new Vector3(0.33f, 0.33f, 0.33f);
            createdSharps.transform.position = GetPosition();
        }

        yield return null;

        yield return new WaitForSeconds(2f);

        EventBus.isWallHit?.Invoke();
        spawnSharpsCoroutine = null;
    }

    private void PlayerCrush()
    {
        if (isCrushed || blockCollisionsImmediately) return;

        if (playerMovement != null && playerMovement.CheckInvulnerable())
            return;

        isCrushed = true;

        if (parentRb != null) parentRb.linearVelocity = Vector3.zero;

        EventBus.isCrush?.Invoke();
        spawnSharpsCoroutine = StartCoroutine(SpawnSharpsRoutine());
    }

    public void HidePlayerCrush()
    {
        isCrushed = false;

        if (spawnSharpsCoroutine != null)
        {
            StopCoroutine(spawnSharpsCoroutine);
            spawnSharpsCoroutine = null;
        }

        if (createdSharps != null)
        {
            Destroy(createdSharps);
            createdSharps = null;
        }

        // Фикс: запускаем короткую аппаратную блокировку триггеров на время телепортации
        StartCoroutine(TemporaryInvulnerabilityRoutine());
    }

    private IEnumerator TemporaryInvulnerabilityRoutine()
    {
        blockCollisionsImmediately = true;
        // Ждем пару физических кадров, пока сцена полностью перестроится
        yield return new WaitForSecondsRealtime(0.15f);
        blockCollisionsImmediately = false;
    }

    private void OnCollisionEnter(Collision collision)
    {
        // Если включен блок коллизий из-за недавнего рестарта — полностью игнорируем удар
        if (isCrushed || blockCollisionsImmediately)
            return;

        if (playerMovement != null && playerMovement.CheckInvulnerable())
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
