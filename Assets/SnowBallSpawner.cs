using System.Collections;
using UnityEngine;

public class SnowBallSpawner : MonoBehaviour
{
    public GameObject[] spawnablePrefabs; // Array of prefabs to spawn, drag and drop multiple objects here
    public float spawnInterval = 2f; // Time interval between spawns
    public float objectLifetime = 5f; // How long each spawned object lasts before being destroyed
    public float spawnForce = 5f; // Force applied to each object when spawned
    public Vector2 spawnDirection = new Vector2(1, 0); // Direction of the force

    private void Start()
    {
        // Start spawning objects at regular intervals
        StartCoroutine(SpawnObjects());
    }

    private IEnumerator SpawnObjects()
    {
        while (true)
        {
            // Choose a random prefab from the array
            GameObject prefabToSpawn = spawnablePrefabs[Random.Range(0, spawnablePrefabs.Length)];

            // Spawn the chosen prefab at the position of the spawner
            GameObject spawnedObject = Instantiate(prefabToSpawn, transform.position, Quaternion.identity);

            // Apply force to the spawned object to make it move
            Rigidbody2D rb = spawnedObject.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.AddForce(spawnDirection.normalized * spawnForce, ForceMode2D.Impulse);
            }

            // Destroy the spawned object after the specified lifetime
            Destroy(spawnedObject, objectLifetime);

            // Wait for the next spawn interval
            yield return new WaitForSeconds(spawnInterval);
        }
    }
}
