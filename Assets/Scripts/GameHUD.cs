using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class GameHUD : MonoBehaviour
{
    [Header("HUD Elements")]
    [SerializeField] private TextMeshProUGUI healthText;
    [SerializeField] private TextMeshProUGUI waveText;
    [SerializeField] private TextMeshProUGUI enemiesText;

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
        // Resume time
        Time.timeScale = 1f;
        // Reload active scene
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
