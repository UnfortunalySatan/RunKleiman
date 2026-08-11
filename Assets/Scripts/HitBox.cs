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

    // Ссылка на конкретную корутину спавна осколков, чтобы не использовать глобальный StopAllCoroutines()
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

    private IEnumerator SpawnSharps()
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
        spawnSharpsCoroutine = null; // Очищаем ссылку после завершения
    }

    private void PlayerCrush()
    {
        if (isCrushed) return;

        if (playerMovement != null && playerMovement.CheckInvulnerable())
            return;

        isCrushed = true;

        if (parentRb != null) parentRb.linearVelocity = Vector3.zero;
        if (playerMovement != null) playerMovement.DontRun();

        EventBus.isCrush?.Invoke();

        // Безопасно запускаем именно эту корутину
        spawnSharpsCoroutine = StartCoroutine(SpawnSharps());
    }

    public void HidePlayerCrush()
    {
        // Вместо StopAllCoroutines() аккуратно убираем только спавн осколков, не ломая чужие скрипты и рекламу
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
