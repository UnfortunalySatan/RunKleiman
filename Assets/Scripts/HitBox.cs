using System.Collections;
using UnityEngine;

public class HitBox : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private GameObject playerSharpsPrefab;
    private PlayerMovement playerMovement;
    private Rigidbody parentRb;
    private GameObject createdSharps;

    // Флаг-предохранитель: защищает от многократного срабатывания смерти при скольжении ловушки
    private bool isCrushed = false;

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
    }

    private void PlayerCrush()
    {
        // Если мы уже разбились в этом раунде — мгновенно игнорируем повторный удар
        if (isCrushed) return;

        // Если у игрока действует окно бессмертия после респавна — тоже выходим
        if (playerMovement != null && playerMovement.CheckInvulnerable())
            return;

        // Фиксируем смерть: теперь ловушка больше не сможет запустить взрыв повторно
        isCrushed = true;

        if (parentRb != null) parentRb.linearVelocity = Vector3.zero;
        if (playerMovement != null) playerMovement.DontRun();

        EventBus.isCrush?.Invoke();
        StartCoroutine(SpawnSharps());
    }

    public void HidePlayerCrush()
    {
        StopAllCoroutines();

        // Сбрасываем флаг при возрождении или рестарте, чтобы игрок снова мог проиграть
        isCrushed = false;

        if (createdSharps != null)
        {
            Destroy(createdSharps);
            createdSharps = null;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        // Если мы уже разбились или бессмертны — полностью игнорируем любые коллизии
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
