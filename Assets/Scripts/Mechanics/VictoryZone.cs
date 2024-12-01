using UnityEngine;
using UnityEngine.SceneManagement; // For scene management
using TMPro; // For TextMeshPro elements
using System.Collections;

namespace Platformer.Mechanics
{
    public class VictoryZone : MonoBehaviour
    {
        [SerializeField] private int nextSceneIndex = 0; // Index of the next scene to load, set in the Inspector
        [SerializeField] private GameObject victoryCanvas; // Reference to the Victory Canvas
        [SerializeField] private TMP_Text tokensCollectedText; // Reference to the TextMeshPro UI for collected tokens
        [SerializeField] private TMP_Text timeTakenText; // Reference to the TextMeshPro UI for time taken

        private bool victoryActivated = false; // To track if the victory screen is active

        private void OnTriggerEnter2D(Collider2D collider)
        {
            var player = collider.gameObject.GetComponent<PlayerController>();
            if (player != null && !victoryActivated)
            {
                victoryActivated = true;

                // Activate the Victory Canvas
                if (victoryCanvas != null)
                {
                    victoryCanvas.SetActive(true);

                    // Update the UI elements
                    UpdateVictoryUI();
                }

                // Pause the game
                Time.timeScale = 0f;

                // Start listening for any key press
                StartCoroutine(WaitForAnyKey(player));
            }
        }

        private void UpdateVictoryUI()
        {
            // Ensure TokenController.Instance is valid
            if (TokenController.Instance == null)
            {
                Debug.LogWarning("TokenController.Instance is null. Attempting to find TokenController in the scene...");
                var foundTokenController = FindObjectOfType<TokenController>();
                if (foundTokenController != null)
                {
                    Debug.Log("TokenController found in the scene.");
                }
                else
                {
                    Debug.LogError("No TokenController found in the scene!");
                    return; // Exit the method early if no TokenController is found
                }
            }

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



        private IEnumerator WaitForAnyKey(PlayerController player)
        {
            // Wait for any key to be pressed
            while (!Input.anyKeyDown)
            {
                yield return null;
            }

            // Play the player's victory animation
            if (player != null && player.animator != null)
            {
                player.animator.SetTrigger("victory"); // Trigger sthe victory animation
            }

            // Unpause the game
            Time.timeScale = 1f;

            // Disable the Victory Canvas
            if (victoryCanvas != null)
            {
                victoryCanvas.SetActive(false);
            }

            // Wait for 2 seconds to allow the animation to play
            yield return new WaitForSeconds(2f);

            // Load the next scene
            SceneManager.LoadScene(nextSceneIndex);
        }
    }
}
