using UnityEngine;

public class Powerup : MonoBehaviour
{
    public float duration = 10f; // Duration of the power-up effect

    private void OnTriggerEnter2D(Collider2D other)
    {
        PlayerAttack playerAttack = other.GetComponent<PlayerAttack>();

        if (playerAttack != null)
        {
            // Activate the power-up
            playerAttack.ActivatePowerUp();

            // Destroy the power-up object
            Destroy(gameObject);
        }
    }
}