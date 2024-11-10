using Platformer.Mechanics;
using System.Collections;
using UnityEngine;

public class PlayerAttack : MonoBehaviour
{
    public float attackRange = 1.5f;  // Range of the attack (adjustable)
    public float attackDelay = 0.5f;  // Delay before applying pushback (adjustable)
    public Transform attackPoint;     // The point from where the attack is checked (e.g., player's position)
    public string enemyTag = "Enemy"; // Tag to identify enemies
    public float pushbackForce = 5f;  // The force applied to push back the enemy

    private bool canAttack = true;    // Track whether the player can attack again
    private Animator animator;        // Reference to the player's animator
    private AudioSource audioSource;  // Reference to the player's audio source
    public AudioClip attackAudio;     // Sound effect for the attack

    private void Awake()
    {
        animator = GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>();
    }

    private void Update()
    {
        // Detect the attack input using the legacy Input Manager
        if (canAttack && (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.F) || Input.GetButtonDown("Fire1")))
        {
            // Trigger the attack
            StartCoroutine(PerformAttack());
        }
    }

    private IEnumerator PerformAttack()
    {
        // Disable further attacks until the current one finishes
        canAttack = false;

        // Trigger the attack animation
        if (animator != null)
        {
            animator.SetBool("isAttacking", true);
        }

        // Play attack sound if available
        if (attackAudio != null && audioSource != null)
        {
            audioSource.PlayOneShot(attackAudio);
        }

        // Add a delay before the attack is executed
        yield return new WaitForSeconds(attackDelay);

        // Detect enemies in range using an overlap circle
        Collider2D[] collidersHit = Physics2D.OverlapCircleAll(attackPoint.position, attackRange);

        // Apply pushback to enemies with the specified tag within range
        foreach (Collider2D collider in collidersHit)
        {
            if (collider.CompareTag(enemyTag))  // Check if the collider has the enemy tag
            {
                // Determine the pushback direction and apply pushback to the enemy
                Vector2 pushbackDirection = (collider.transform.position - transform.position).normalized;
                AnimationController enemyController = collider.GetComponent<AnimationController>();

                if (enemyController != null)
                {
                    float pushbackDistance = 2f;  // Adjust the distance for the pushback effect
                    enemyController.TriggerPushback(pushbackDirection, pushbackDistance);
                }
            }
        }



        // End the attack animation
        yield return new WaitForSeconds(0.3f); // Adjust duration as per animation timing
        if (animator != null)
        {
            animator.SetBool("isAttacking", false);
        }

        // Re-enable attacking after the attack is finished
        canAttack = true;
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
