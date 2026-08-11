using Unity.VisualScripting;
using UnityEngine;
using System.Collections.Generic;
using System.Collections;
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
        Vector3 position = gameObject.GetComponent<Transform>().position;
        return position;
    }

    private IEnumerator SpawnSharps()
    {
        createdSharps = Instantiate(playerSharpsPrefab);
        createdSharps.transform.localScale = new Vector3(0.33f, 0.33f, 0.33f);
        yield return null;
    }
    private void PlayerCrush()
    {
        StartCoroutine(SpawnSharps());
        createdSharps.transform.position = GetPosition();
        EventBus.isCrush?.Invoke();
        playerMovement.DontRun();
    }

    public void HidePlayerCrush()
    {
        StopCoroutine(SpawnSharps());
        Destroy(createdSharps);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.tag == "Colotun")
        {
            //Vector3 distance = collision.gameObject.transform.position - transform.position;
            //playerMovement.Push(distance.normalized);
            //Debug.Log("Был удар! " + distance.normalized);
            PlayerCrush();
        }
        if (collision.gameObject.tag == "Wall")
        {
            animator.SetTrigger("Hit");
        }
        if (collision.gameObject.tag == "Plush")
        {
            
            animator.SetTrigger("Hit");
        }
    }

    
}
