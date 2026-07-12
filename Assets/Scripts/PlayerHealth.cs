using UnityEngine;
using InfimaGames.LowPolyShooterPack;

public class PlayerHealth : MonoBehaviour
{
    [Header("Stats")]
    [SerializeField] private float maxHealth = 100f;
    
    private float currentHealth;
    private Character playerMovement;

    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;

    private void Start()
    {
        currentHealth = maxHealth;
        playerMovement = GetComponent<Character>();

        // Update UI HUD at start
        var hud = FindFirstObjectByType<GameHUD>();
        if (hud != null)
        {
            hud.UpdateHealth(currentHealth, maxHealth);
        }
    }

    public void TakeDamage(float damageAmount)
    {
        if (currentHealth <= 0) return;

        currentHealth -= damageAmount;
        currentHealth = Mathf.Max(0f, currentHealth);

        // Update UI HUD if available
        var hud = FindFirstObjectByType<GameHUD>();
        if (hud != null)
        {
            hud.UpdateHealth(currentHealth, maxHealth);
        }

        if (currentHealth <= 0)
        {
            Die();
        }
    }

public void Heal(float amount)
    {
        if (currentHealth <= 0) return;

        currentHealth = amount > 0 ? Mathf.Min(maxHealth, currentHealth + amount) : maxHealth;

        var hud = FindFirstObjectByType<GameHUD>();
        if (hud != null)
        {
            hud.UpdateHealth(currentHealth, maxHealth);
        }
    }


    private void Die()
    {
        // Disable Character movement and weapons script
        if (playerMovement != null)
        {
            playerMovement.enabled = false;
        }

        // Show Game Over UI
        var hud = FindFirstObjectByType<GameHUD>();
        if (hud != null)
        {
            hud.ShowGameOverScreen();
        }

        // Unlock cursor for UI interaction
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Pause time
        Time.timeScale = 0f;
    }
}
