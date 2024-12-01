using UnityEngine;
using UnityEngine.SceneManagement;

namespace Platformer.Mechanics
{
    public class TokenController : MonoBehaviour
    {
        public float frameRate = 12; // Animation frame rate
        public TokenInstance[] tokens;

        public static TokenController Instance { get; private set; } // Singleton for easy access

        private int collectedTokenCount;

        public int CollectedTokenCount => collectedTokenCount; // Public getter for collected tokens

        float nextFrameTime = 0;

        void Awake()
        {
            if (Instance == null)
            {
                Instance = this; // Set up singleton
                DontDestroyOnLoad(gameObject); // Make persistent across scenes
            }
            else
            {
                Destroy(gameObject); // Ensure there's only one instance
            }

            // If tokens are empty, find all instances
            if (tokens.Length == 0)
                FindAllTokensInScene();

            // Register all tokens so they can work with this controller
            for (var i = 0; i < tokens.Length; i++)
            {
                if (tokens[i] != null)
                {
                    tokens[i].tokenIndex = i;
                    tokens[i].controller = this;
                }
            }
        }

        [ContextMenu("Find All Tokens")]
        void FindAllTokensInScene()
        {
            tokens = FindObjectsOfType<TokenInstance>();
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            FindAllTokensInScene(); // Refresh token references when a new scene is loaded
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded; // Subscribe to the sceneLoaded event
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded; // Unsubscribe from the event
        }

        public void IncrementTokenCount()
        {
            collectedTokenCount++; // Increment the collected token count
        }

        void Update()
        {
            if (Time.time >= nextFrameTime)
            {
                nextFrameTime = Time.time + (1f / frameRate);

                for (var i = 0; i < tokens.Length; i++)
                {
                    var token = tokens[i];
                    if (token != null && !token.collected)
                    {
                        token._renderer.sprite = token.sprites[token.frame];
                        token.frame = (token.frame + 1) % token.sprites.Length;
                    }
                }
            }
        }
    }
}
