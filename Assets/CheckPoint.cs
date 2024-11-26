using UnityEngine;

public class CheckPoint : MonoBehaviour
{
    [Header("Checkpoint Settings")]
    [SerializeField] private Animator checkpointAnimator; // Animator for the checkpoint (plays constantly)
    [SerializeField] private string playerTag = "Player"; // Tag to identify the player object
    [SerializeField] private GameObject objectToMove; // The object to move to the checkpoint's location
    [SerializeField] private float yOffset = 2f; // Y offset value for moving the object

    [Header("Audio Settings")]
    [SerializeField] private AudioClip checkpointSound; // Sound when the player reaches the checkpoint
    [SerializeField] private AudioClip nearCheckpointSound; // Sound when the player is near the checkpoint
    [SerializeField] private AudioSource audioSource; // AudioSource for playing sounds

    private void Awake()
    {
        // Ensure the Animator is assigned; if not, get it from the same GameObject
        if (checkpointAnimator == null)
        {
            checkpointAnimator = GetComponent<Animator>();
        }

        // Ensure that the animator plays continuously
        if (checkpointAnimator != null)
        {
            checkpointAnimator.enabled = true; // This line ensures the animator is constantly running
        }

        // Ensure we have an AudioSource assigned
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Check if the colliding object is the player
        if (collision.CompareTag(playerTag))
        {
            // Play checkpoint reached sound
            if (checkpointSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(checkpointSound);
            }

            // Mark the checkpoint as reached by disabling its sprite renderer (making it disappear)
            SpriteRenderer checkpointSprite = GetComponent<SpriteRenderer>();
            if (checkpointSprite != null)
            {
                checkpointSprite.enabled = false;
            }

            // Move the specified object to the checkpoint's position with the Y offset
            if (objectToMove != null)
            {
                Vector3 newPosition = transform.position;
                newPosition.y += yOffset; // Apply the Y offset
                objectToMove.transform.position = newPosition;
            }
        }
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        // Check if the colliding object is the player and if the player is still close
        if (collision.CompareTag(playerTag))
        {
            // Play near checkpoint sound if it's not already playing
            if (nearCheckpointSound != null && audioSource != null && !audioSource.isPlaying)
            {
                audioSource.PlayOneShot(nearCheckpointSound);
            }
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        // Optionally stop near checkpoint sound when the player leaves the area
        if (collision.CompareTag(playerTag))
        {
            if (audioSource != null)
            {
                audioSource.Stop();
            }
        }
    }
}
