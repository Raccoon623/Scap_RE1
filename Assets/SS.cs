using UnityEngine;
using UnityEngine.SceneManagement; // For scene management

public class SS : MonoBehaviour
{
    [SerializeField] private string sceneName; // The name of the scene to load
    [SerializeField] private bool useIndex = false; // Use scene index instead of name
    [SerializeField] private int sceneIndex; // The index of the scene to load (optional)

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Check if the player has entered the trigger
        if (collision.CompareTag("Player"))
        {
            Debug.Log("Player touched the scene change trigger!");

            // Load the next scene based on name or index
            if (useIndex)
            {
                SceneManager.LoadScene(sceneIndex);
            }
            else
            {
                SceneManager.LoadScene(sceneName);
            }
        }
    }
}
