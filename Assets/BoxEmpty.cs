using Platformer.Mechanics;
using System.Collections;
using UnityEngine;

public class BoxEmpty : MonoBehaviour
{
    public float trampolineForceMultiplier = 1.5f; // Adjustable multiplier for trampoline effect

    private Animator animator;
    private Collider2D boxCollider;
    private bool hasExploded = false;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        boxCollider = GetComponent<Collider2D>();

        if (animator != null)
        {
            animator.enabled = false;
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player") && !hasExploded)
        {
            foreach (ContactPoint2D contact in collision.contacts)
            {
                if (contact.normal.y < -0.5f)
                {
                    hasExploded = true;

                    if (animator != null)
                    {
                        animator.enabled = true;
                        animator.SetTrigger("Explode");
                    }

                    PlayerController player = collision.gameObject.GetComponent<PlayerController>();
                    if (player != null)
                    {
                        player.ApplyTrampolineJump(trampolineForceMultiplier); // Call trampoline jump
                    }

                    if (boxCollider != null)
                    {
                        boxCollider.enabled = false;
                    }

                    StartCoroutine(DestroyAfterDelay(2f));
                    break;
                }
            }
        }
    }

    private IEnumerator DestroyAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        Destroy(gameObject);
    }
}
