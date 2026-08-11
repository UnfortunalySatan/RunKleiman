using UnityEngine;

public class HitBox : MonoBehaviour
{
    [SerializeField] private Animator animator;
    private PlayerMovement playerMovement;
    private void Start()
    {
        playerMovement = GetComponentInParent<PlayerMovement>();
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.tag == "Colotun")
        {
            Vector3 distance = collision.gameObject.transform.position - transform.position;
            playerMovement.Push(distance.normalized);
            Debug.Log("Был удар! " + distance.normalized);
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
