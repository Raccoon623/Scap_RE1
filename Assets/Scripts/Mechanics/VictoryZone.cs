using Platformer.Gameplay;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement; // Required for scene management
using static Platformer.Core.Simulation;

namespace Platformer.Mechanics
{
    /// <summary>
    /// Marks a trigger as a VictoryZone, usually used to end the current game level.
    /// </summary>
    public class VictoryZone : MonoBehaviour
    {
        [SerializeField]
        private int sceneIndex = 0; // Index of the scene to load, set in the Inspector

        void OnTriggerEnter2D(Collider2D collider)
        {
            var p = collider.gameObject.GetComponent<PlayerController>();
            if (p != null)
            {
                var ev = Schedule<PlayerEnteredVictoryZone>();
                ev.victoryZone = this;

                // Start the coroutine to change the scene after a delay
                StartCoroutine(LoadNextSceneWithDelay());
            }
        }

        // Coroutine to wait for 2 seconds and then change the scene
        private IEnumerator LoadNextSceneWithDelay()
        {
            yield return new WaitForSeconds(3f); // Wait for 2 seconds

            // Load the scene by index
            SceneManager.LoadScene(sceneIndex);
        }
    }
}
