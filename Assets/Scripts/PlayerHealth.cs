using UnityEngine;
using InfimaGames.LowPolyShooterPack;

public class PlayerHealth : MonoBehaviour
{
    [Header("Stats")]
    [SerializeField] private float maxHealth = 100f;

    [Header("Shield")]
    [SerializeField] private float maxShield = 100f;
    [SerializeField] private float shieldHintHealthThreshold = 40f;

    private float currentHealth;
    private float currentShield;
    private bool shieldAvailable = true;
    private Character playerMovement;
    private GameHUD hud;

    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;
    public float CurrentShield => currentShield;
    public bool ShieldAvailable => shieldAvailable;

    private void Start()
    {
        currentHealth = maxHealth;
        playerMovement = GetComponent<Character>();
        hud = FindFirstObjectByType<GameHUD>();

        if (hud != null)
        {
            hud.UpdateHealth(currentHealth, maxHealth);
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.B))
        {
            TryActivateShield();
        }
    }

    public void TryActivateShield()
    {
        if (!shieldAvailable || currentShield > 0f || currentHealth <= 0f) return;

        shieldAvailable = false;
        currentShield = maxShield;

        if (hud != null)
        {
            hud.UpdateShield(currentShield, maxShield);
            hud.SetShieldHint(false);
        }
    }

    // Called by GameController at the start of each wave: one shield claim per wave.
    public void RefreshShield()
    {
        shieldAvailable = true;
        UpdateShieldHint();
    }

    public void TakeDamage(float damageAmount)
    {
        if (currentHealth <= 0) return;

        // Shield soaks damage first
        if (currentShield > 0f)
        {
            float absorbed = Mathf.Min(currentShield, damageAmount);
            currentShield -= absorbed;
            damageAmount -= absorbed;

            if (hud != null)
            {
                hud.UpdateShield(currentShield, maxShield);
            }
        }

        if (damageAmount > 0f)
        {
            currentHealth -= damageAmount;
            currentHealth = Mathf.Max(0f, currentHealth);

            if (hud != null)
            {
                hud.UpdateHealth(currentHealth, maxHealth);
            }
        }

        UpdateShieldHint();

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    public void Heal(float amount)
    {
        if (currentHealth <= 0) return;

        currentHealth = amount > 0 ? Mathf.Min(maxHealth, currentHealth + amount) : maxHealth;

        if (hud != null)
        {
            hud.UpdateHealth(currentHealth, maxHealth);
        }

        UpdateShieldHint();
    }

    private void UpdateShieldHint()
    {
        if (hud == null) return;
        bool show = currentHealth > 0f
                    && currentHealth < shieldHintHealthThreshold
                    && shieldAvailable
                    && currentShield <= 0f;
        hud.SetShieldHint(show);
    }

    private void Die()
    {
        // Disable Character movement and weapons script
        if (playerMovement != null)
        {
            playerMovement.enabled = false;
        }

        if (hud != null)
        {
            hud.SetShieldHint(false);
            hud.ShowGameOverScreen();
        }

        // Unlock cursor for UI interaction
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Pause time
        Time.timeScale = 0f;
    }
}
