using UnityEngine;

public class Snowball : MonoBehaviour
{
    public float downhillForce = 1f; // Force applied to simulate downhill movement (adjustable in inspector)
    private Rigidbody2D rb; // Reference to the Rigidbody2D component

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>(); // Get the Rigidbody2D component on the snowball
    }

    private void FixedUpdate()
    {
        // Check if the snowball is moving to the right (positive X velocity)
        if (rb.velocity.x > 0)
        {
            // Apply a small force to the right to simulate downhill movement
            rb.AddForce(Vector2.right * downhillForce, ForceMode2D.Force);
        }
    }
}
