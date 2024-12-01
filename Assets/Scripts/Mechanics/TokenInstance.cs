using Platformer.Gameplay;
using UnityEngine;
using static Platformer.Core.Simulation;

namespace Platformer.Mechanics
{
    [RequireComponent(typeof(Collider2D))]
    public class TokenInstance : MonoBehaviour
    {
        public AudioClip tokenCollectAudio;
        [Tooltip("If true, animation will start at a random position in the sequence.")]
        public bool randomAnimationStartTime = false;
        [Tooltip("List of frames that make up the animation.")]
        public Sprite[] idleAnimation, collectedAnimation;

        internal Sprite[] sprites = new Sprite[0];

        internal SpriteRenderer _renderer;

        // Unique index which is assigned by the TokenController in a scene.
        internal int tokenIndex = -1;
        internal TokenController controller;
        // Active frame in animation, updated by the controller.
        internal int frame = 0;
        internal bool collected = false;

        void Awake()
        {
            _renderer = GetComponent<SpriteRenderer>();
            sprites = idleAnimation; // Start with idle animation

            if (randomAnimationStartTime && sprites.Length > 0)
                frame = Random.Range(0, sprites.Length); // Randomize animation start frame
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            // Only execute OnPlayerEnter if the player collides with this token.
            var player = other.gameObject.GetComponent<PlayerController>();
            if (player != null) OnPlayerEnter(player);
        }

        void OnPlayerEnter(PlayerController player)
        {
            if (collected) return; // If already collected, do nothing

            collected = true; // Mark token as collected
            frame = 0; // Reset animation frame
            sprites = collectedAnimation; // Switch to collected animation

            // Notify the controller that the token is collected
            if (controller != null)
            {
                controller.IncrementTokenCount();
            }

            // Play collection sound if available
            if (tokenCollectAudio != null)
            {
                AudioSource.PlayClipAtPoint(tokenCollectAudio, transform.position);
            }

            // Send an event into the gameplay system
            var ev = Schedule<PlayerTokenCollision>();
            ev.token = this;
            ev.player = player;

            // Start the collected animation and hide the token after it finishes
            Invoke(nameof(HideToken), collectedAnimation.Length / controller.frameRate);
        }

        private void HideToken()
        {
            gameObject.SetActive(false); // Disable the token
        }

        void Update()
        {
            // Ensure collected tokens still animate properly before being hidden
            if (collected && frame < sprites.Length)
            {
                _renderer.sprite = sprites[frame];
                frame++;
            }
        }
    }
}
