using System.Collections;
using UnityEngine;
using Platformer.Gameplay;
using static Platformer.Core.Simulation;

namespace Platformer.Mechanics
{
    /// <summary>
    /// A simple controller for enemies. Provides movement control over a patrol path.
    /// </summary>
    [RequireComponent(typeof(AnimationController), typeof(Collider2D), typeof(Rigidbody2D))]
    public class EnemyController : MonoBehaviour
    {
        public PatrolPath path;
        public AudioClip ouch;

        internal PatrolPath.Mover mover;
        internal AnimationController control;
        internal Collider2D _collider;
        internal AudioSource _audio;
        internal Rigidbody2D rb; // Rigidbody for applying force

        public float pushbackResistance = 1f; // Resistance to the pushback force
        SpriteRenderer spriteRenderer;

        public Bounds Bounds => _collider.bounds;

        void Awake()
        {
            control = GetComponent<AnimationController>();
            _collider = GetComponent<Collider2D>();
            _audio = GetComponent<AudioSource>();
            rb = GetComponent<Rigidbody2D>();
            spriteRenderer = GetComponent<SpriteRenderer>();

            // Ensure Rigidbody2D settings are appropriate
            rb.gravityScale = 0; // Prevent the enemy from falling if that's intended
            rb.freezeRotation = true; // Prevent rotation for a stable pushback effect
        }

        void OnCollisionEnter2D(Collision2D collision)
        {
            var player = collision.gameObject.GetComponent<PlayerController>();
            if (player != null)
            {
                var ev = Schedule<PlayerEnemyCollision>();
                ev.player = player;
                ev.enemy = this;
            }
        }

        void Update()
        {
            if (path != null)
            {
                if (mover == null) mover = path.CreateMover(control.maxSpeed * 0.5f);
                control.move.x = Mathf.Clamp(mover.Position.x - transform.position.x, -1, 1);
            }
        }

        /// <summary>
        /// This method is called by the player's attack to apply a pushback force.
        /// </summary>
        /// <param name="pushbackDirection">The direction of the pushback force.</param>
        /// <param name="pushbackForce">The magnitude of the pushback force.</param>
        public void TakeHit(Vector2 pushbackDirection, float pushbackForce)
        {
            // Apply pushback to the enemy based on resistance
            if (rb != null)
            {
                Vector2 force = pushbackDirection * (pushbackForce / pushbackResistance);
                rb.AddForce(force, ForceMode2D.Impulse); // Immediate pushback effect
            }

            // Play the "ouch" sound if it exists
            if (ouch != null && _audio != null)
            {
                _audio.PlayOneShot(ouch);
            }
        }
    }
}
