using UnityEngine;

public class CheckPoint : MonoBehaviour
{
    [SerializeField] private Animator checkpointAnimator; // Animator for the checkpoint
    [SerializeField] private string playerTag = "Player"; // Tag to identify the player object
    [SerializeField] private GameObject objectToMove; // The object to move to the checkpoint's location

    private void Awake()
    {
        // Ensure the Animator is assigned; if not, get it from the same GameObject
        if (checkpointAnimator == null)
        {
            checkpointAnimator = GetComponent<Animator>();
        }

        // Disable the animator initially
        if (checkpointAnimator != null)
        {
            checkpointAnimator.enabled = false;
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Check if the colliding object is the player
        if (collision.CompareTag(playerTag))
        {
            // Enable the animator
            if (checkpointAnimator != null)
            {
                checkpointAnimator.enabled = true;
            }

            // Move the specified object to the checkpoint's position
            if (objectToMove != null)
            {
                objectToMove.transform.position = transform.position;
            }
        }
    }
}
