using UnityEngine;

namespace Platformer.Mechanics
{
    /// <summary>
    /// This class animates all token instances in a scene.
    /// This allows a single update call to animate hundreds of sprite 
    /// animations.
    /// If the tokens property is empty, it will automatically find and load 
    /// all token instances in the scene at runtime.
    /// </summary>
    public class TokenController : MonoBehaviour
    {
        [Tooltip("Frames per second at which tokens are animated.")]
        public float frameRate = 12;
        [Tooltip("Instances of tokens which are animated. If empty, token instances are found and loaded at runtime.")]
        public TokenInstance[] tokens;

        public static TokenController Instance { get; private set; } // Singleton for easy access

        private int collectedTokenCount;

        public int CollectedTokenCount => collectedTokenCount; // Public getter for collected tokens

        float nextFrameTime = 0;

        [ContextMenu("Find All Tokens")]
        void FindAllTokensInScene()
        {
            tokens = UnityEngine.Object.FindObjectsOfType<TokenInstance>();
        }

        void Awake()
        {
            if (Instance == null)
                Instance = this; // Set up singleton
            else
                Destroy(gameObject);

            // If tokens are empty, find all instances.
            if (tokens.Length == 0)
                FindAllTokensInScene();

            // Register all tokens so they can work with this controller.
            for (var i = 0; i < tokens.Length; i++)
            {
                tokens[i].tokenIndex = i;
                tokens[i].controller = this;
            }
        }

        public void IncrementTokenCount()
        {
            collectedTokenCount++; // Increment the token count
        }

        void Update()
        {
            // If it's time for the next frame...
            if (Time.time - nextFrameTime > (1f / frameRate))
            {
                // Update all tokens with the next animation frame.
                for (var i = 0; i < tokens.Length; i++)
                {
                    var token = tokens[i];
                    // If token is null, it has been disabled and is no longer animated.
                    if (token != null)
                    {
                        token._renderer.sprite = token.sprites[token.frame];
                        if (token.collected && token.frame == token.sprites.Length - 1)
                        {
                            token.gameObject.SetActive(false);
                            tokens[i] = null;
                        }
                        else
                        {
                            token.frame = (token.frame + 1) % token.sprites.Length;
                        }
                    }
                }
                // Calculate the time of the next frame.
                nextFrameTime += 1f / frameRate;
            }
        }
    }
}
