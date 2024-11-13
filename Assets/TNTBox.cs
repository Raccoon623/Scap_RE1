using Platformer.Mechanics;
using System.Collections;
using UnityEngine;

public class BoxTNT : MonoBehaviour
{
    public float explosionDelay = 3f; // Time in seconds before the TNT explodes
    public float destroyDelay = 0.5f; // Time after turning on the animator before destroying the box
    public string playerTag = "Player"; // Tag to identify player objects
    public string crackedTag = "Cracked"; // Tag to identify cracked objects

    private Animator animator; // Reference to the animator
    private SpriteRenderer spriteRenderer; // Reference to the sprite renderer for color change
    private bool isActivated = false; // Track if the box has already been activated

    private void Awake()
    {
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!isActivated && collision.gameObject.CompareTag(playerTag))
        {
            // Start the explosion timer if the player jumps on it
            StartCoroutine(StartExplosionTimer());

            // Access PlayerController to play TNT sound
            PlayerController playerController = collision.gameObject.GetComponent<PlayerController>();
            if (playerController != null)
            {
                playerController.PlayBoxTNTSound();
            }

            isActivated = true;
        }
    }

    private IEnumerator StartExplosionTimer()
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

        // Turn on the explosion animation
        if (animator != null)
        {
            animator.enabled = true;
            animator.SetTrigger("Explode");
        }

        // Wait for the destroy delay before destroying the box and nearby objects
        yield return new WaitForSeconds(destroyDelay);

        // Destroy all objects tagged as Player and Cracked within a certain radius
        Explode();

        // Destroy the TNT box itself
        Destroy(gameObject);
    }

    private void Explode()
    {
        // Find all objects within a certain radius (customize as needed)
        Collider2D[] objectsToDestroy = Physics2D.OverlapCircleAll(transform.position, 2f);

        foreach (Collider2D obj in objectsToDestroy)
        {
            if (obj.CompareTag(playerTag) || obj.CompareTag(crackedTag))
            {
                Destroy(obj.gameObject); // Destroy the object if it's tagged as Player or Cracked
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        // Optional: Draw a gizmo to visualize the explosion radius in the editor
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, 2f); // Customize radius as needed
    }
}
