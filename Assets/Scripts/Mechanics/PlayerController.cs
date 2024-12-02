using System.Collections;
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
        public AudioClip respawnGlass;

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

        [SerializeField] private ParticleSystem dustEffect; // Reference to dust particle effect
        [SerializeField] private Transform dustSpawnPoint;  // Spawn point for the particle effect

        [SerializeField] private float wallDetectionRadius = 1f; // Radius for wall detection
        [SerializeField] private LayerMask wallLayer; // LayerMask for walls
        [SerializeField] private float raycastAngle = -55f; // Angle of the raycast (adjustable in the Inspector)

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
                        TriggerDustEffect(); // Trigger the dust effect upon landing
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

            // Handle wall collision detection and prevent movement toward walls
            if (CheckWallCollision(out RaycastHit2D wallHit))
            {
                Debug.Log($"Wall detected at {wallHit.point}. Preventing movement toward the wall.");

                // Check the direction the player is moving
                float moveDirection = move.x;
                float wallDirection = IsFacingRight ? 1 : -1;

                // Prevent movement toward the wall, but allow movement away
                if (Mathf.Sign(moveDirection) == Mathf.Sign(wallDirection))
                {
                    move.x = 0; // Stop movement toward the wall
                }
            }

            targetVelocity = move * maxSpeed;

            // Flip the sprite based on movement direction
            if (move.x > 0.01f)
                spriteRenderer.flipX = false; // Facing right
            else if (move.x < -0.01f)
                spriteRenderer.flipX = true; // Facing left

            animator.SetBool("grounded", IsGrounded);
            animator.SetFloat("velocityX", Mathf.Abs(velocity.x) / maxSpeed);
        }

        // Method to check if the player is colliding with a wall in front
        private bool CheckWallCollision(out RaycastHit2D wallHit)
        {
            // Determine the direction based on the player's facing direction
            Vector2 direction = IsFacingRight ? Vector2.right : Vector2.left;

            // Adjust the angle based on the facing direction
            float adjustedAngle = IsFacingRight ? raycastAngle : -raycastAngle;

            // Rotate the ray direction based on the adjusted angle
            direction = Quaternion.Euler(0, 0, adjustedAngle) * direction;

            // Perform a raycast in the direction the player is facing
            wallHit = Physics2D.Raycast(transform.position, direction, wallDetectionRadius, wallLayer);

            // Visualize the ray in the scene view for debugging
            Debug.DrawRay(transform.position, direction * wallDetectionRadius, Color.red);

            // Return true if the ray hits a wall
            return wallHit.collider != null;
        }

        // Method to trigger the dust particle effect
        private void TriggerDustEffect()
        {
            if (dustEffect != null && dustSpawnPoint != null)
            {
                // Instantiate the dust effect at the spawn point
                ParticleSystem dust = Instantiate(dustEffect, dustSpawnPoint.position, Quaternion.identity);
                dust.Play();
                Destroy(dust.gameObject, dust.main.duration); // Destroy the effect after it finishes
            }
        }

        // Method to play TNT explosion sound
        public void PlayBoxTNTSound()
        {
            if (BoxTNTSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(BoxTNTSound);
            }
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
