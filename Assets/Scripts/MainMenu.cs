using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    [Header("Menu Panels")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject settingsPanel;

    [Header("Settings Controls")]
    [SerializeField] private Slider sensitivitySlider;
    [SerializeField] private TextMeshProUGUI sensitivityText;

    private void Start()
    {
        // Unlock and show cursor in Main Menu
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Make sure panels are in the correct state
        if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
        if (settingsPanel != null) settingsPanel.SetActive(false);

        // Load saved mouse sensitivity or default to 1.0
        float savedSensitivity = PlayerPrefs.GetFloat("MouseSensitivity", 1.0f);
        if (sensitivitySlider != null)
        {
            sensitivitySlider.minValue = 0.1f;
            sensitivitySlider.maxValue = 5.0f;
            sensitivitySlider.value = savedSensitivity;
        }
        UpdateSensitivityText(savedSensitivity);
    }

    public void StartGame()
    {
        // Load GameScene
        SceneManager.LoadScene("GameScene");
    }

    public void OpenSettings()
    {
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(true);
    }

    public void CloseSettings()
    {
        if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
        if (settingsPanel != null) settingsPanel.SetActive(false);
    }

    public void OnSensitivityChanged(float value)
    {
        // Save to PlayerPrefs
        PlayerPrefs.SetFloat("MouseSensitivity", value);
        PlayerPrefs.Save();
        
        UpdateSensitivityText(value);
    }

    private void UpdateSensitivityText(float value)
    {
        if (sensitivityText != null)
        {
            sensitivityText.text = $"Mouse Sensitivity: {value:F2}";
        }
    }

    public void QuitGame()
    {
        Application.Quit();
        Debug.Log("[MainMenu] Quit Game");
    }
}
