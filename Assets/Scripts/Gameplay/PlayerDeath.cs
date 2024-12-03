using System.Collections;
using System.Collections.Generic;
using Platformer.Core;
using Platformer.Model;
using UnityEngine;

namespace Platformer.Gameplay
{
    /// <summary>
    /// Fired when the player has died.
    /// </summary>
    /// <typeparam name="PlayerDeath"></typeparam>
    public class PlayerDeath : Simulation.Event<PlayerDeath>
    {
        PlatformerModel model = Simulation.GetModel<PlatformerModel>();

        public override void Execute()
        {
            var player = model.player;
            if (player.health.IsAlive)
            {
                // Deactivate the player's power-up
                var playerAttack = player.GetComponent<PlayerAttack>();
                if (playerAttack != null)
                {
                    playerAttack.DeactivatePowerUp();
                }

                // Handle player death logic
                player.health.Die();
                model.virtualCamera.m_Follow = null;
                model.virtualCamera.m_LookAt = null;
                player.controlEnabled = false;

                // Play death audio and trigger animations
                if (player.audioSource && player.ouchAudio)
                    player.audioSource.PlayOneShot(player.ouchAudio);
                player.animator.SetTrigger("hurt");
                player.animator.SetBool("dead", true);

                // Schedule player respawn
                Simulation.Schedule<PlayerSpawn>(2);
            }
        }
    }
}
