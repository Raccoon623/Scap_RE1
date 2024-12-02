using Platformer.Mechanics;
using System.Collections;
using UnityEngine;

public class PlayerAttack : MonoBehaviour
{
    public float attackRange = 1.5f;  // Range of the attack (adjustable)
    public float attackDelay = 0.5f;  // Delay before applying pushback (adjustable)
    public Transform attackPoint;     // The point from where the attack is checked (e.g., player's position)
    public string boxTag = "Box";     // Tag to identify the box objects
    public string enemyTag = "Enemy"; // Tag to identify enemies
    public float pushbackForce = 5f;  // The force applied to push back the box
    public float enemyLaunchForce = 10f; // Force applied to launch the enemy upwards
    public float shrinkDuration = 1f; // Time it takes for enemies to shrink and disappear

    [SerializeField] private AudioClip baseballHitSound; // Sound for hitting an enemy with a baseball swing
    private AudioSource audioSource; // Reference to the player's audio source

    private bool canAttack = true;    // Track whether the player can attack again
    private Animator animator;        // Reference to the player's animator
    public AudioClip attackAudio;     // Sound effect for the attack

    private PlayerController playerController; // Reference to the PlayerController
    private bool isPoweredUp = false;          // Track if the player is powered up

    private void Awake()
    {
        animator = GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>();
        playerController = GetComponent<PlayerController>(); // Get reference to PlayerController
    }

    private void Update()
    {
        // Detect the attack input using the legacy Input Manager
        if (canAttack && Input.GetButtonDown("Fire1"))
        {
            // Trigger the attack
            StartCoroutine(PerformAttack());
        }

        // Update attackPoint position based on player's facing direction
        UpdateAttackPointPosition();
    }

    private void UpdateAttackPointPosition()
    {
        if (playerController == null || attackPoint == null)
            return;

        // Flip the attack point based on the player's facing direction
        if (playerController.IsFacingRight)
        {
            attackPoint.localPosition = new Vector3(Mathf.Abs(attackPoint.localPosition.x), attackPoint.localPosition.y, attackPoint.localPosition.z);
        }
        else
        {
            attackPoint.localPosition = new Vector3(-Mathf.Abs(attackPoint.localPosition.x), attackPoint.localPosition.y, attackPoint.localPosition.z);
        }
    }

    private IEnumerator PerformAttack()
    {
        // Disable further attacks until the current one finishes
        canAttack = false;

        // Trigger the attack animation
        if (animator != null)
        {
            if (isPoweredUp)
            {
                animator.SetBool("isPoweredAttacking", true);
            }
            else
            {
                animator.SetBool("isAttacking", true);
            }
        }

        // Play attack sound if available
        if (attackAudio != null && audioSource != null)
        {
            audioSource.PlayOneShot(attackAudio);
        }

        // Add a delay before the attack is executed
        yield return new WaitForSeconds(attackDelay);

        // Detect objects in range using an overlap circle
        Collider2D[] collidersHit = Physics2D.OverlapCircleAll(attackPoint.position, attackRange);

        foreach (Collider2D collider in collidersHit)
        {
            // Handle boxes (pushback)
            if (collider.CompareTag(boxTag))
            {
                Rigidbody2D rb = collider.GetComponent<Rigidbody2D>();
                if (rb != null && rb.bodyType == RigidbodyType2D.Dynamic) // Only apply force to dynamic bodies
                {
                    Vector2 pushbackDirection = (collider.transform.position - transform.position).normalized;
                    rb.AddForce(pushbackDirection * pushbackForce, ForceMode2D.Impulse);
                }
            }

            // Handle enemies (baseball hit effect)
            if (isPoweredUp && collider.CompareTag(enemyTag))
            {
                StartCoroutine(BaseballHitEnemy(collider.gameObject));
            }
        }

        // End the attack animation
        yield return new WaitForSeconds(0.3f); // Adjust duration as per animation timing
        if (animator != null)
        {
            animator.SetBool("isAttacking", false);
            animator.SetBool("isPoweredAttacking", false);
        }

        // Re-enable attacking after the attack is finished
        canAttack = true;
    }

    private IEnumerator BaseballHitEnemy(GameObject enemy)
    {
        // Play the baseball hit sound
        if (baseballHitSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(baseballHitSound);
        }

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

    // Enable the power-up
    public void ActivatePowerUp()
    {
        isPoweredUp = true;
    }

    // Disable the power-up
    public void DeactivatePowerUp()
    {
        isPoweredUp = false;
    }

    // Optional: Draw a gizmo to visualize the attack range in the editor
    private void OnDrawGizmosSelected()
    {
        if (attackPoint == null)
            return;

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(attackPoint.position, attackRange);
    }
}
