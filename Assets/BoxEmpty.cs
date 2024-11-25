using Platformer.Mechanics;
using System.Collections;
using UnityEngine;

public class BoxEmpty : MonoBehaviour
{
    public float trampolineForceMultiplier = 1.5f; // Adjustable multiplier for trampoline effect
    public float enemyKillForce = 5f; // Force threshold to kill the enemy
    public float enemyLaunchForce = 10f; // Force applied to launch the enemy upwards
    public float shrinkDuration = 1f; // Time it takes for enemies to shrink and disappear

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
        else if (collision.gameObject.CompareTag("Enemy"))
        {
            // Handle collision with enemy
            HandleEnemyCollision(collision);
        }
    }

    private void HandleEnemyCollision(Collision2D collision)
    {
        Rigidbody2D enemyRb = collision.gameObject.GetComponent<Rigidbody2D>();

        if (enemyRb != null)
        {
            // Check if the collision force is strong enough to kill the enemy
            if (collision.relativeVelocity.magnitude >= enemyKillForce)
            {
                StartCoroutine(BaseballHitEnemy(collision.gameObject));
            }
        }
    }

    private IEnumerator BaseballHitEnemy(GameObject enemy)
    {
        // Apply launch force
        Rigidbody2D enemyRb = enemy.GetComponent<Rigidbody2D>();
        if (enemyRb != null)
        {
            enemyRb.bodyType = RigidbodyType2D.Dynamic; // Temporarily make the enemy dynamic
            enemyRb.AddForce(Vector2.up * enemyLaunchForce, ForceMode2D.Impulse); // Launch upwards
        }

        // Shrink the enemy over time
        Vector3 originalScale = enemy.transform.localScale;
        float elapsed = 0f;

        while (elapsed < shrinkDuration)
        {
            float scale = Mathf.Lerp(1f, 0f, elapsed / shrinkDuration); // Gradually scale down
            enemy.transform.localScale = originalScale * scale;
            elapsed += Time.deltaTime;
            yield return null;
        }

        // Destroy the enemy
        Destroy(enemy);
    }

    private IEnumerator DestroyAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        Destroy(gameObject);
    }
}
