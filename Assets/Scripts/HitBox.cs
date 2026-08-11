using System.Collections;
using UnityEngine;

public class HitBox : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private GameObject playerSharpsPrefab;
    private PlayerMovement playerMovement;
    private Rigidbody parentRb;
    private GameObject createdSharps;

    private void Start()
    {
        playerMovement = GetComponentInParent<PlayerMovement>();
        parentRb = GetComponentInParent<Rigidbody>();
    }

    private void OnEnable()
    {
        // Подписываемся на события продолжения и полного рестарта игры, 
        // чтобы гарантированно удалять старые осколки при возрождении
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
        // Если старые осколки почему-то еще живы — уничтожаем их перед созданием новых
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
        if (parentRb != null) parentRb.linearVelocity = Vector3.zero;
        if (playerMovement != null) playerMovement.DontRun();

        EventBus.isCrush?.Invoke();
        StartCoroutine(SpawnSharps());
    }

    // Метод очистки сцены от мусора осколков
    public void HidePlayerCrush()
    {
        StopAllCoroutines();
        if (createdSharps != null)
        {
            Destroy(createdSharps);
            createdSharps = null;
            Debug.Log("[HitBox] Старые осколки успешно удалены со сцены.");
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
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
