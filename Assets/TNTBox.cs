using Platformer.Core;
using Platformer.Gameplay;
using Platformer.Mechanics;
using System.Collections;
using UnityEngine;

public class BoxTNT : MonoBehaviour
{
    [SerializeField] private float explosionDelay = 3f; // Time in seconds before the TNT explodes
    [SerializeField] private float destroyDelay = 0.5f; // Time after turning on the animator before destroying the box
    [SerializeField] private float explosionRadius = 2f; // Radius of the explosion effect

    [SerializeField] private string enemyTag = "Enemy"; // Tag to identify enemy objects
    [SerializeField] private string crackedTag = "Cracked"; // Tag to identify cracked objects

    [SerializeField] private AudioClip warningSound; // Serialize field for the warning sound
    [SerializeField] private AudioSource audioSource; // Reference to AudioSource for playing the warning sound

    private Animator animator; // Reference to the animator
    private SpriteRenderer spriteRenderer; // Reference to the sprite renderer for color change
    private Collider2D boxCollider; // Reference to the box collider
    private bool isActivated = false; // Track if the box has already been activated

    private void Awake()
    {
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        boxCollider = GetComponent<Collider2D>(); // Initialize the box collider

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>(); // Fallback to attached AudioSource
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!isActivated && (collision.gameObject.GetComponent<PlayerController>() != null || collision.gameObject.CompareTag(enemyTag)))
        {
            // Start the explosion timer if the player or enemy collides with it
            StartCoroutine(StartExplosionTimer(collision.gameObject));

            isActivated = true;
        }
    }

    private IEnumerator StartExplosionTimer(GameObject triggeringObject)
    {
        float timeElapsed = 0f;

        // Play the warning sound at the start of the timer
        if (warningSound != null && audioSource != null)
        {
            audioSource.loop = true; // Enable looping for the warning sound
            audioSource.clip = warningSound;
            audioSource.Play();
        }

        while (timeElapsed < explosionDelay)
        {
            // Gradually change color to red over time
            float t = timeElapsed / explosionDelay;
            spriteRenderer.color = Color.Lerp(Color.white, Color.red, t);

            timeElapsed += Time.deltaTime;
            yield return null;
        }

        // Stop the warning sound when the timer ends
        if (audioSource != null && audioSource.clip == warningSound)
        {
            audioSource.Stop();
            audioSource.loop = false; // Disable looping
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

        // Trigger the explosion sound through PlayerController if triggered by the player
        var playerController = triggeringObject.GetComponent<PlayerController>();
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
