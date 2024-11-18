using Platformer.Gameplay;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement; // Required for scene management
using TMPro; // Required for TextMeshPro
using static Platformer.Core.Simulation;

namespace Platformer.Mechanics
{
    /// <summary>
    /// Marks a trigger as a VictoryZone, usually used to end the current game level.
    /// </summary>
    public class VictoryZone : MonoBehaviour
    {
        [SerializeField]
        private int nextSceneIndex = 0; // Index of the next scene to load, set in the Inspector

        [SerializeField]
        private GameObject victoryCanvas; // Reference to the Victory Canvas

        [SerializeField]
        private TextMeshProUGUI tokensCollectedText; // Reference to the TextMeshPro UI for collected tokens

        [SerializeField]
        private TextMeshProUGUI timeTakenText; // Reference to the TextMeshPro UI for time taken

        private void OnTriggerEnter2D(Collider2D collider)
        {
            var player = collider.gameObject.GetComponent<PlayerController>();
            if (player != null)
            {
                var ev = Schedule<PlayerEnteredVictoryZone>();
                ev.victoryZone = this;

                // Activate the Victory Canvas
                if (victoryCanvas != null)
                {
                    victoryCanvas.SetActive(true);

                    // Update the UI elements
                    UpdateVictoryUI();
                }

                // Pause the game
                Time.timeScale = 0f;

                // Stop the timer
                if (GameTimer.Instance != null)
                {
                    GameTimer.Instance.StopTimer();
                }
            }
        }

        private void UpdateVictoryUI()
        {
            // Update tokens collected text
            if (tokensCollectedText != null && TokenController.Instance != null)
            {
                int collectedTokens = TokenController.Instance.CollectedTokenCount;
                tokensCollectedText.text = $"Tokens Collected: {collectedTokens}";
            }

            // Update time taken text
            if (timeTakenText != null && GameTimer.Instance != null)
            {
                float timeTaken = GameTimer.Instance.GetElapsedTime();
                timeTakenText.text = $"Time Taken: {timeTaken:F2} seconds";
            }
        }

        public void OnContinueButtonPressed()
        {
            // Unpause the game
            Time.timeScale = 1f;

            // Load the next scene
            SceneManager.LoadScene(nextSceneIndex);
        }
    }
}
