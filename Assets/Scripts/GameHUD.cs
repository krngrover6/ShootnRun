using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class GameHUD : MonoBehaviour
{
    [Header("HUD Elements")]
    [SerializeField] private TextMeshProUGUI healthText;
    [SerializeField] private TextMeshProUGUI waveText;
    [SerializeField] private TextMeshProUGUI enemiesText;

    [SerializeField] private UnityEngine.UI.Slider healthSlider;

    [Header("Game Over Screen")]
    [SerializeField] private GameObject gameOverPanel;

    [Header("Shield")]
    [SerializeField] private UnityEngine.UI.Image shieldOverlay;
    [SerializeField] private TextMeshProUGUI shieldText;
    [SerializeField] private TextMeshProUGUI shieldHintText;

    private bool hintVisible;

    private void Start()
    {
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }

        if (shieldOverlay != null) shieldOverlay.gameObject.SetActive(false);
        if (shieldText != null) shieldText.gameObject.SetActive(false);
        if (shieldHintText != null) shieldHintText.gameObject.SetActive(false);
    }

    private void Update()
    {
        // Glowing pulse on the shield hint
        if (hintVisible && shieldHintText != null)
        {
            float pulse = 0.65f + 0.35f * Mathf.Sin(Time.unscaledTime * 5f);
            var c = shieldHintText.color;
            shieldHintText.color = new Color(c.r, c.g, c.b, pulse);
            shieldHintText.transform.localScale = Vector3.one * (1f + 0.04f * Mathf.Sin(Time.unscaledTime * 5f));
        }
    }

    public void UpdateShield(float current, float max)
    {
        bool active = current > 0f;

        if (shieldOverlay != null)
        {
            shieldOverlay.gameObject.SetActive(active);
            if (active)
            {
                // Tint fades as the shield wears down, so the screen itself tells you how much is left
                float pct = current / max;
                var c = shieldOverlay.color;
                shieldOverlay.color = new Color(c.r, c.g, c.b, 0.06f + 0.16f * pct);
            }
        }

        if (shieldText != null)
        {
            shieldText.gameObject.SetActive(active);
            if (active) shieldText.text = $"SHIELD: {Mathf.CeilToInt(current)}";
        }
    }

    public void SetShieldHint(bool visible)
    {
        if (hintVisible == visible) return;
        hintVisible = visible;
        if (shieldHintText != null) shieldHintText.gameObject.SetActive(visible);
    }

    public void UpdateHealth(float current, float max)
    {
        if (healthText != null)
        {
            healthText.text = $"HP: {Mathf.CeilToInt(current)} / {max}";
        }

        if (healthSlider != null)
        {
            healthSlider.maxValue = max;
            healthSlider.value = current;

            // Change color dynamically from green to yellow to red
            var fillImage = healthSlider.fillRect.GetComponent<UnityEngine.UI.Image>();
            if (fillImage != null)
            {
                float pct = current / max;
                if (pct > 0.5f)
                    fillImage.color = Color.Lerp(Color.yellow, Color.green, (pct - 0.5f) * 2f);
                else
                    fillImage.color = Color.Lerp(Color.red, Color.yellow, pct * 2f);
            }
        }
    }

    public void UpdateWave(int waveNumber)
    {
        if (waveText != null)
        {
            waveText.text = $"WAVE: {waveNumber}";
        }
    }

    public void UpdateEnemiesRemaining(int count)
    {
        if (enemiesText != null)
        {
            enemiesText.text = $"ENEMIES: {count}";
        }
    }

    public void ShowGameOverScreen()
    {
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
        }
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void GoToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenuScene");
    }

public void QuitGame()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

}
