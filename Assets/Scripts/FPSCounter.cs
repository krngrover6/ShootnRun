using UnityEngine;
using TMPro;

public class FPSCounter : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI fpsText;
    [SerializeField] private float updateInterval = 0.5f;

    private int frameCount;
    private float elapsed;

    private void Update()
    {
        if (fpsText == null) return;

        frameCount++;
        // unscaledDeltaTime so the meter keeps updating while the game is paused (timeScale = 0)
        elapsed += Time.unscaledDeltaTime;

        if (elapsed >= updateInterval)
        {
            float fps = frameCount / elapsed;
            fpsText.text = $"FPS: {Mathf.RoundToInt(fps)}";

            // Green when smooth, yellow when middling, red when struggling
            if (fps >= 50f) fpsText.color = new Color(0.3f, 1f, 0.4f);
            else if (fps >= 30f) fpsText.color = new Color(1f, 0.85f, 0.2f);
            else fpsText.color = new Color(1f, 0.3f, 0.25f);

            frameCount = 0;
            elapsed = 0f;
        }
    }
}
