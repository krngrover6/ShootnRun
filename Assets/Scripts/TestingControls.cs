using UnityEngine;

public class TestingControls : MonoBehaviour
{
    [Header("Testing Sliders")]
    [Range(0.1f, 10f)]
    [SerializeField] private float mouseSensitivity = 1.0f;

    [Range(0.5f, 20f)]
    [SerializeField] private float enemyMoveSpeed = 3.5f;

    private float lastMouseSensitivity;
    private float lastEnemyMoveSpeed;

    private void Start()
    {
        lastMouseSensitivity = mouseSensitivity;
        lastEnemyMoveSpeed = enemyMoveSpeed;

        // Load saved sensitivity if it exists
        if (PlayerPrefs.HasKey("MouseSensitivity"))
        {
            mouseSensitivity = PlayerPrefs.GetFloat("MouseSensitivity");
            lastMouseSensitivity = mouseSensitivity;
        }
        else
        {
            // Auto-initialize from current player look settings if available
            var look = FindFirstObjectByType<InfimaGames.LowPolyShooterPack.CameraLook>();
            if (look != null)
            {
                mouseSensitivity = look.Sensitivity.x;
                lastMouseSensitivity = mouseSensitivity;
            }
        }
        
        // Apply sensitivity to the camera look at start
        UpdateMouseSensitivity();

        // Auto-initialize from one of the active enemies if any exist
        var enemy = FindFirstObjectByType<EnemyAI>();
        if (enemy != null)
        {
            enemyMoveSpeed = enemy.MoveSpeed;
            lastEnemyMoveSpeed = enemyMoveSpeed;
        }
    }

    private void Update()
    {
        // Detect changes in the Inspector and apply them immediately in real-time
        if (Mathf.Abs(mouseSensitivity - lastMouseSensitivity) > 0.01f)
        {
            UpdateMouseSensitivity();
            lastMouseSensitivity = mouseSensitivity;
        }

        if (Mathf.Abs(enemyMoveSpeed - lastEnemyMoveSpeed) > 0.01f)
        {
            UpdateEnemySpeeds();
            lastEnemyMoveSpeed = enemyMoveSpeed;
        }
    }

    private void UpdateMouseSensitivity()
    {
        var look = FindFirstObjectByType<InfimaGames.LowPolyShooterPack.CameraLook>();
        if (look != null)
        {
            look.Sensitivity = new Vector2(mouseSensitivity, mouseSensitivity);
            Debug.Log($"[TestingControls] Mouse Sensitivity updated to: {mouseSensitivity}");
        }
    }

    private void UpdateEnemySpeeds()
    {
        var activeEnemies = FindObjectsByType<EnemyAI>(FindObjectsSortMode.None);
        foreach (var enemy in activeEnemies)
        {
            enemy.MoveSpeed = enemyMoveSpeed;
        }
        Debug.Log($"[TestingControls] Active Enemy Speeds updated to: {enemyMoveSpeed}");
    }

    public float EnemyMoveSpeed => enemyMoveSpeed;
}
