using System.Collections;
using Platformer.Gameplay;
using UnityEngine;
using static Platformer.Core.Simulation;

namespace Platformer.Mechanics
{
    public class DeathZone : MonoBehaviour
    {
        void OnTriggerEnter2D(Collider2D collider)
        {
            // Check if the object is the player
            var p = collider.gameObject.GetComponent<PlayerController>();
            if (p != null)
            {
                // Turn off the power-up when the player respawns
                var playerAttack = p.GetComponent<PlayerAttack>();
                if (playerAttack != null)
                {
                    playerAttack.DeactivatePowerUp();
                }

                // Schedule the PlayerEnteredDeathZone event
                var ev = Schedule<PlayerEnteredDeathZone>();
                ev.deathzone = this;
            }

            // Check if the object is an enemy and destroy it
            if (collider.CompareTag("Enemy"))
            {
                Destroy(collider.gameObject);
            }

            // Check if the object is a box and destroy it
            if (collider.CompareTag("Box"))
            {
                Destroy(collider.gameObject);
            }
        }
    }
}
