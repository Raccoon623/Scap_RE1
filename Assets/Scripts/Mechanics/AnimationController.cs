using System.Collections;
using Platformer.Core;
using Platformer.Model;
using UnityEngine;

namespace Platformer.Mechanics
{
    /// <summary>
    /// AnimationController integrates physics and animation. It is generally used for simple enemy animation.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer), typeof(Animator), typeof(Rigidbody2D))]
    public class AnimationController : KinematicObject
    {
        public float maxSpeed = 7;
        public float jumpTakeOffSpeed = 7;

        public Vector2 move;
        public bool jump;
        public bool stopJump;

        private SpriteRenderer spriteRenderer;
        private Animator animator;
        private Rigidbody2D rb;
        private PlatformerModel model = Simulation.GetModel<PlatformerModel>();

        private bool isBeingPushedBack = false;  // Flag to check if pushback is active
        private Vector2 pushbackDirection;
        private float pushbackDistance;
        private float pushbackSpeed = 10f;  // Adjust to control how quickly the enemy moves back

        protected virtual void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            animator = GetComponent<Animator>();
            rb = GetComponent<Rigidbody2D>();
        }

        protected override void ComputeVelocity()
        {
            if (isBeingPushedBack)
            {
                // Apply the pushback effect manually by adjusting position
                transform.position += (Vector3)(pushbackDirection * pushbackSpeed * Time.deltaTime);

                // Check if pushback distance has been covered, then stop pushback
                pushbackDistance -= pushbackSpeed * Time.deltaTime;
                if (pushbackDistance <= 0f)
                {
                    isBeingPushedBack = false;  // Stop pushback effect
                }

                return;  // Skip regular movement while pushback is active
            }

            if (jump && IsGrounded)
            {
                velocity.y = jumpTakeOffSpeed * model.jumpModifier;
                jump = false;
            }
            else if (stopJump)
            {
                stopJump = false;
                if (velocity.y > 0)
                {
                    velocity.y = velocity.y * model.jumpDeceleration;
                }
            }

            if (move.x > 0.01f)
                spriteRenderer.flipX = false;
            else if (move.x < -0.01f)
                spriteRenderer.flipX = true;

            animator.SetBool("grounded", IsGrounded);
            animator.SetFloat("velocityX", Mathf.Abs(velocity.x) / maxSpeed);

            targetVelocity = move * maxSpeed;
        }

        /// <summary>
        /// Triggers a pushback effect for the enemy.
        /// </summary>
        /// <param name="direction">Direction of the pushback effect.</param>
        /// <param name="distance">Distance over which the enemy will be pushed back.</param>
        public void TriggerPushback(Vector2 direction, float distance)
        {
            pushbackDirection = direction.normalized;
            pushbackDistance = distance;
            isBeingPushedBack = true;
        }
    }
}
