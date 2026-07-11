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

    private void Start()
    {
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }
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
}
