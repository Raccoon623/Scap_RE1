using UnityEngine;

public class CrazyBird : MonoBehaviour
{
    public float minSpeed = 2f; // Minimum speed of the bird
    public float maxSpeed = 7f; // Maximum speed of the bird
    private float speed; // Speed at which the bird moves to the left

    void Start()
    {
        // Set the speed to a random value between minSpeed and maxSpeed
        speed = Random.Range(minSpeed, maxSpeed);
    }

    void Update()
    {
        // Move the bird to the left by decreasing the X position over time
        transform.Translate(Vector2.left * speed * Time.deltaTime);
    }
}
