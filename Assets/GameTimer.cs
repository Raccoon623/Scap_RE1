using UnityEngine;

namespace Platformer.Mechanics
{
    public class GameTimer : MonoBehaviour
    {
        public static GameTimer Instance; // Singleton instance of GameTimer

        private float startTime; // Time when the game starts
        private bool isRunning = false; // Track whether the timer is running

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject); // Optional, keeps the timer between scenes
            }
            else
            {
                Destroy(gameObject); // Ensure there's only one instance
            }
        }

        private void Start()
        {
            StartTimer(); // Start the timer when the game starts
        }

        // Start or restart the timer
        public void StartTimer()
        {
            startTime = Time.time;
            isRunning = true;
        }

        // Stop the timer
        public void StopTimer()
        {
            isRunning = false;
        }

        // Get the elapsed time since the timer started
        public float GetElapsedTime()
        {
            if (isRunning)
            {
                return Time.time - startTime;
            }
            return 0f;
        }
    }
}
