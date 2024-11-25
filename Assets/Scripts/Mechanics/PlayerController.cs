using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Platformer.Gameplay;
using static Platformer.Core.Simulation;
using Platformer.Model;
using Platformer.Core;

namespace Platformer.Mechanics
{
    public class PlayerController : KinematicObject
    {
        public AudioClip jumpAudio;
        public AudioClip respawnAudio;
        public AudioClip ouchAudio;

        // Updated BoxEmpty and BoxTNT audio clips
        public AudioClip BoxEmptySound; // Sound for BoxEmpty interaction
        public AudioClip BoxTNTSound;   // Sound for BoxTNT explosion

        public float maxSpeed = 7;
        public float jumpTakeOffSpeed = 7;

        public JumpState jumpState = JumpState.Grounded;
        private bool stopJump;
        public Collider2D collider2d;
        public AudioSource audioSource;
        public Health health;
        public bool controlEnabled = true;

        bool jump;
        Vector2 move;
        SpriteRenderer spriteRenderer;
        internal Animator animator;
        readonly PlatformerModel model = Simulation.GetModel<PlatformerModel>();

        public Bounds Bounds => collider2d.bounds;
        public bool IsFacingRight => !spriteRenderer.flipX;

        void Awake()
        {
            health = GetComponent<Health>();
            audioSource = GetComponent<AudioSource>();
            collider2d = GetComponent<Collider2D>();
            spriteRenderer = GetComponent<SpriteRenderer>();
            animator = GetComponent<Animator>();
        }

        protected override void Update()
        {
            if (controlEnabled)
            {
                move.x = Input.GetAxis("Horizontal");
                if (jumpState == JumpState.Grounded && Input.GetButtonDown("Jump"))
                    jumpState = JumpState.PrepareToJump;
                else if (Input.GetButtonUp("Jump"))
                {
                    stopJump = true;
                    Schedule<PlayerStopJump>().player = this;
                }
            }
            else
            {
                move.x = 0;
            }
            UpdateJumpState();
            base.Update();
        }

        void UpdateJumpState()
        {
            jump = false;
            switch (jumpState)
            {
                case JumpState.PrepareToJump:
                    jumpState = JumpState.Jumping;
                    jump = true;
                    stopJump = false;
                    break;
                case JumpState.Jumping:
                    if (!IsGrounded)
                    {
                        Schedule<PlayerJumped>().player = this;
                        jumpState = JumpState.InFlight;
                    }
                    break;
                case JumpState.InFlight:
                    if (IsGrounded)
                    {
                        Schedule<PlayerLanded>().player = this;
                        jumpState = JumpState.Landed;
                    }
                    break;
                case JumpState.Landed:
                    jumpState = JumpState.Grounded;
                    break;
            }
        }

        protected override void ComputeVelocity()
        {
            // Handle jump velocity
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

            // Handle collision with walls to prevent sticking
            RaycastHit2D wallHit = Physics2D.Raycast(transform.position, Vector2.right * Mathf.Sign(move.x), 0.1f);
            if (wallHit.collider != null && !IsGrounded)
            {
                // Check if the collision is with a wall (not the ground)
                if (wallHit.collider.gameObject.CompareTag("Wall"))
                {
                    // Maintain horizontal velocity to prevent sticking
                    targetVelocity.x = move.x * maxSpeed;
                }
            }

            // Flip the sprite based on movement direction
            if (move.x > 0.01f)
                spriteRenderer.flipX = false; // Facing right
            else if (move.x < -0.01f)
                spriteRenderer.flipX = true; // Facing left

            animator.SetBool("grounded", IsGrounded);
            animator.SetFloat("velocityX", Mathf.Abs(velocity.x) / maxSpeed);

            targetVelocity = move * maxSpeed;
        }

        // Method to apply a trampoline jump effect with a specific multiplier
        public void ApplyTrampolineJump(float trampolineForceMultiplier)
        {
            velocity.y = jumpTakeOffSpeed * trampolineForceMultiplier; // Apply trampoline jump force

            // Play the BoxEmpty sound if it exists
            if (BoxEmptySound != null && audioSource != null)
            {
                audioSource.PlayOneShot(BoxEmptySound);
            }
        }

        // Method to check if the player is currently attacking
        public bool IsAttacking()
        {
            // Check if the animator has "isAttacking" or "isPoweredAttacking" set to true
            if (animator != null)
            {
                return animator.GetBool("isAttacking") || animator.GetBool("isPoweredAttacking");
            }
            return false;
        }

        // Method to play TNT explosion sound
        public void PlayBoxTNTSound()
        {
            if (BoxTNTSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(BoxTNTSound);
            }
        }



        public enum JumpState
        {
            Grounded,
            PrepareToJump,
            Jumping,
            InFlight,
            Landed
        }
    }
}
