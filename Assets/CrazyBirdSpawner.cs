using UnityEngine;

public class CrazyBirdSpawner : MonoBehaviour
{
    public GameObject creaturePrefab; // The creature prefab to spawn
    public float spawnInterval = 2f; // Interval between spawns (adjustable in inspector)
    public float creatureLifetime = 5f; // Time after which the creature will be automatically destroyed (adjustable in inspector)
    private Collider2D spawnArea; // Reference to the Collider2D component on the spawner

    private void Start()
    {
        // Get the Collider2D component attached to this GameObject
        spawnArea = GetComponent<Collider2D>();
        if (spawnArea == null)
        {
            Debug.LogError("No Collider2D found on CrazyBirdSpawner. Please add a collider to define the spawn area.");
            return;
        }

        // Start the spawning process
        InvokeRepeating(nameof(SpawnCreature), spawnInterval, spawnInterval);
    }

    private void SpawnCreature()
    {
        if (creaturePrefab == null)
        {
            Debug.LogError("Creature prefab not assigned to CrazyBirdSpawner.");
            return;
        }

        // Generate a random position within the spawn area bounds
        Vector2 spawnPosition = GetRandomPositionInBounds(spawnArea.bounds);

        // Instantiate the creature at the random position
        GameObject creature = Instantiate(creaturePrefab, spawnPosition, Quaternion.identity);

        // Schedule the creature to be destroyed after 'creatureLifetime' seconds
        Destroy(creature, creatureLifetime);
    }

    private Vector2 GetRandomPositionInBounds(Bounds bounds)
    {
        // Calculate a random position within the bounds of the collider
        float x = Random.Range(bounds.min.x, bounds.max.x);
        float y = Random.Range(bounds.min.y, bounds.max.y);
        return new Vector2(x, y);
    }

    private void OnDrawGizmosSelected()
    {
        // Visualize the spawn area in the editor (only when the spawner is selected)
        if (spawnArea == null)
            spawnArea = GetComponent<Collider2D>();

        if (spawnArea != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(spawnArea.bounds.center, spawnArea.bounds.size);
        }
    }
}
