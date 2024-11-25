using Platformer.Core;
using Platformer.Gameplay;
using Platformer.Mechanics;
using System.Collections;
using UnityEngine;

public class BoxTNT : MonoBehaviour
{
    public float explosionDelay = 3f; // Time in seconds before the TNT explodes
    public float destroyDelay = 0.5f; // Time after turning on the animator before destroying the box
    public float explosionRadius = 2f; // Radius of the explosion effect

    public string enemyTag = "Enemy"; // Tag to identify enemy objects
    public string crackedTag = "Cracked"; // Tag to identify cracked objects

    private Animator animator; // Reference to the animator
    private SpriteRenderer spriteRenderer; // Reference to the sprite renderer for color change
    private Collider2D boxCollider; // Reference to the box collider
    private bool isActivated = false; // Track if the box has already been activated

    private void Awake()
    {
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        boxCollider = GetComponent<Collider2D>(); // Initialize the box collider
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!isActivated && collision.gameObject.GetComponent<PlayerController>() != null)
        {
            // Start the explosion timer if the player collides with it
            StartCoroutine(StartExplosionTimer(collision.gameObject));

            isActivated = true;
        }
    }

    private IEnumerator StartExplosionTimer(GameObject player)
    {
        float timeElapsed = 0f;

        while (timeElapsed < explosionDelay)
        {
            // Gradually change color to red over time
            float t = timeElapsed / explosionDelay;
            spriteRenderer.color = Color.Lerp(Color.white, Color.red, t);

            timeElapsed += Time.deltaTime;
            yield return null;
        }

        // Ensure the box color is fully red when the timer ends
        spriteRenderer.color = Color.red;

        // Disable collider and Rigidbody2D during animation
        var rigidbody = GetComponent<Rigidbody2D>();
        if (rigidbody != null)
        {
            rigidbody.simulated = false; // Disable physics
        }
        if (boxCollider != null)
        {
            boxCollider.enabled = false; // Disable collisions
        }

        // Trigger the explosion sound through PlayerController
        PlayerController playerController = player.GetComponent<PlayerController>();
        if (playerController != null)
        {
            playerController.PlayBoxTNTSound();
        }

        // Turn on the explosion animation
        if (animator != null)
        {
            animator.enabled = true;
            animator.SetTrigger("Explode");
        }

        // Wait for the destroy delay before destroying the box and applying effects
        yield return new WaitForSeconds(destroyDelay);

        // Apply explosion effects
        Explode();

        // Destroy the TNT box itself
        Destroy(gameObject);
    }

    private void Explode()
    {
        // Find all objects within the explosion radius
        Collider2D[] objectsToAffect = Physics2D.OverlapCircleAll(transform.position, explosionRadius);

        foreach (Collider2D obj in objectsToAffect)
        {
            // Check if it's the player (Scap)
            var playerController = obj.GetComponent<PlayerController>();
            if (playerController != null)
            {
                // Trigger the same event as DeathZone
                var ev = Simulation.Schedule<PlayerEnteredDeathZone>();
                ev.deathzone = null; // Set this to null or customize if needed
                continue;
            }

            // Destroy objects tagged as "Cracked"
            if (obj.CompareTag(crackedTag))
            {
                Destroy(obj.gameObject);
            }

            // Destroy objects tagged as "Enemy"
            if (obj.CompareTag(enemyTag))
            {
                Destroy(obj.gameObject);
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        // Optional: Draw a gizmo to visualize the explosion radius in the editor
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}
