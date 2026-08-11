using System.Collections;
using UnityEngine;

public class HitBox : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private GameObject playerSharpsPrefab;
    private PlayerMovement playerMovement;
    private GameObject createdSharps;

    private void Start()
    {
        playerMovement = GetComponentInParent<PlayerMovement>();
    }

    private Vector3 GetPosition()
    {
        return transform.position;
    }

    private IEnumerator SpawnSharps()
    {
        // Защита от дублирования: если старый объект почему-то не удалился, удаляем его
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
        StartCoroutine(SpawnSharps());
        EventBus.isCrush?.Invoke();
        if (playerMovement != null) playerMovement.DontRun();
    }

    public void HidePlayerCrush()
    {
        StopAllCoroutines();
        if (createdSharps != null)
        {
            Destroy(createdSharps);
            createdSharps = null;
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
