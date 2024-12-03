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
        // Audio clips
        public AudioClip jumpAudio;
        public AudioClip respawnAudio;
        public AudioClip ouchAudio;
        public AudioClip respawnGlass;
        public AudioClip BoxEmptySound; // Sound for BoxEmpty interaction
        public AudioClip BoxTNTSound;   // Sound for BoxTNT explosion
        public AudioClip PowerUpSound; // Sound for PowerUp interaction

        // Movement and jump variables
        public float maxSpeed = 7;
        public float jumpTakeOffSpeed = 7;

        public JumpState jumpState = JumpState.Grounded;
        private bool stopJump;

        // Component references
        public Collider2D collider2d;
        public AudioSource audioSource;
        public Health health;
        public bool controlEnabled = true;

        // Effects and additional gameplay variables
        [SerializeField] private ParticleSystem dustEffect;
        [SerializeField] private Transform dustSpawnPoint;
        [SerializeField] private float wallDetectionRadius = 1f;
        [SerializeField] private LayerMask wallLayer;
        [SerializeField] private float raycastAngle = -55f;

        private bool jump;
        private Vector2 move;
        private SpriteRenderer spriteRenderer;
        internal Animator animator;
        private readonly PlatformerModel model = Simulation.GetModel<PlatformerModel>();

        private bool respawnAudioPlayed = false; // To track if the respawn audio has been played
        private bool isPlayingRespawnAudio = false; // Guard to ensure coroutine doesn't overlap

        public Bounds Bounds => collider2d.bounds;
        public bool IsFacingRight => !spriteRenderer.flipX;

        void Awake()
        {
            // Initialize component references
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
                {
                    jumpState = JumpState.PrepareToJump;
                }
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

            // Check the "dead" condition and handle respawn audio
            if (animator.GetBool("dead"))
            {
                if (!respawnAudioPlayed && !isPlayingRespawnAudio)
                {
                    respawnAudioPlayed = true; // Mark respawn audio as played
                    StartCoroutine(PlayRespawnAudioWithDelay());
                }
            }
            else
            {
                respawnAudioPlayed = false; // Reset flag when "dead" is false
            }

            UpdateJumpState();
            base.Update();
        }

        private void UpdateJumpState()
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
                        TriggerDustEffect();
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
                    velocity.y *= model.jumpDeceleration;
                }
            }

            // Handle wall collision detection
            if (CheckWallCollision(out RaycastHit2D wallHit))
            {
                Debug.Log($"Wall detected at {wallHit.point}. Preventing movement toward the wall.");
                float moveDirection = move.x;
                float wallDirection = IsFacingRight ? 1 : -1;

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

        // Method to check for wall collision
        private bool CheckWallCollision(out RaycastHit2D wallHit)
        {
            Vector2 direction = IsFacingRight ? Vector2.right : Vector2.left;
            float adjustedAngle = IsFacingRight ? raycastAngle : -raycastAngle;
            direction = Quaternion.Euler(0, 0, adjustedAngle) * direction;
            wallHit = Physics2D.Raycast(transform.position, direction, wallDetectionRadius, wallLayer);

            Debug.DrawRay(transform.position, direction * wallDetectionRadius, Color.red);

            return wallHit.collider != null;
        }

        // Method to trigger the dust effect
        private void TriggerDustEffect()
        {
            if (dustEffect != null && dustSpawnPoint != null)
            {
                ParticleSystem dust = Instantiate(dustEffect, dustSpawnPoint.position, Quaternion.identity);
                dust.Play();
                Destroy(dust.gameObject, dust.main.duration);
            }
        }

        // Method to play the respawn audio after a delay
        private IEnumerator PlayRespawnAudioWithDelay()
        {
            isPlayingRespawnAudio = true; // Set guard
            yield return new WaitForSeconds(2f); // Delay duration

            if (respawnAudio != null && audioSource != null)
            {
                audioSource.PlayOneShot(respawnAudio);
                Debug.Log("Respawn audio played.");
            }

            isPlayingRespawnAudio = false; // Reset guard
        }

        // Method to play the PowerUp sound
        private void PlayPowerUpSound()
        {
            if (PowerUpSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(PowerUpSound);
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.CompareTag("PowerUp"))
            {
                PlayPowerUpSound();
                Debug.Log("Player touched a PowerUp!");
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

        // Method to apply trampoline jump effect
        public void ApplyTrampolineJump(float trampolineForceMultiplier)
        {
            velocity.y = jumpTakeOffSpeed * trampolineForceMultiplier;

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
