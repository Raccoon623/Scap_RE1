using UnityEngine;

public class ForceUp : MonoBehaviour
{
    [SerializeField]
    private float amplitude = 0.5f; // How far the enemy floats up and down (adjustable in Inspector)
    [SerializeField]
    private float frequency = 1f;   // How fast the enemy oscillates (adjustable in Inspector)

    private Vector2 startPosition;  // The original position where the enemy will float around

    void Start()
    {
        // Store the starting position of the enemy
        startPosition = transform.position;
    }

    void Update()
    {
        // Calculate the new position using a sine wave for smooth up and down movement
        float newY = startPosition.y + Mathf.Sin(Time.time * frequency) * amplitude;

        // Apply the new position while keeping the X and Z coordinates the same
        transform.position = new Vector2(startPosition.x, newY);
    }
}