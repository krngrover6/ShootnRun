using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class GameController : MonoBehaviour
{
    [Header("Enemy Prefabs")]
    [SerializeField] private GameObject[] enemyPrefabs;

    [Header("Wave Configuration")]
    [SerializeField] private float timeBetweenWaves = 3f;
    [SerializeField] private int baseEnemyCount = 4;
    [SerializeField] private int enemiesPerWaveIncrease = 3;

    private Transform[] spawnPoints;
    private int currentWave = 0;
    private int enemiesRemainingToSpawn;
    private int activeEnemiesCount = 0;
    private GameHUD hud;

    private void Start()
    {
        // Resume time at startup
        Time.timeScale = 1f;

        // Find HUD
        hud = FindFirstObjectByType<GameHUD>();

        // Find spawn points
        var spawnPointsParent = GameObject.Find("EnemySpawnPoints");
        if (spawnPointsParent != null)
        {
            var list = new List<Transform>();
            foreach (Transform child in spawnPointsParent.transform)
            {
                list.Add(child);
            }
            spawnPoints = list.ToArray();
        }
        else
        {
            Debug.LogError("EnemySpawnPoints parent object not found in scene!");
        }

        // Start first wave
        StartCoroutine(StartNextWaveCo());
    }

    private IEnumerator StartNextWaveCo()
    {
        currentWave++;
        if (hud != null)
        {
            hud.UpdateWave(currentWave);
            hud.UpdateEnemiesRemaining(0);
        }

        // Breather delay
        yield return new WaitForSeconds(timeBetweenWaves);

        enemiesRemainingToSpawn = baseEnemyCount + (currentWave - 1) * enemiesPerWaveIncrease;
        activeEnemiesCount = 0;

        if (hud != null)
        {
            hud.UpdateEnemiesRemaining(enemiesRemainingToSpawn);
        }

        // Spawn loop
        while (enemiesRemainingToSpawn > 0)
        {
            SpawnEnemy();
            enemiesRemainingToSpawn--;
            
            // Update UI count
            if (hud != null)
            {
                hud.UpdateEnemiesRemaining(activeEnemiesCount + enemiesRemainingToSpawn);
            }

            // Spawn one enemy every 1 to 2 seconds
            yield return new WaitForSeconds(Random.Range(1f, 2f));
        }
    }

    private void SpawnEnemy()
    {
        if (spawnPoints == null || spawnPoints.Length == 0 || enemyPrefabs == null || enemyPrefabs.Length == 0) return;

        // Choose random spawn point and random prefab
        Transform spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];
        GameObject enemyPrefab = enemyPrefabs[Random.Range(0, enemyPrefabs.Length)];

        // Sample position on NavMesh to ensure it spawns exactly on the mesh
        Vector3 spawnPosition = spawnPoint.position;
        if (UnityEngine.AI.NavMesh.SamplePosition(spawnPoint.position, out UnityEngine.AI.NavMeshHit hit, 15f, UnityEngine.AI.NavMesh.AllAreas))
        {
            spawnPosition = hit.position;
        }

        // Instantiate enemy
        GameObject enemyInstance = Instantiate(enemyPrefab, spawnPosition, spawnPoint.rotation);
        
        // Ensure it is named correctly so it doesn't have "(Clone)" appended (optional, but clean)
        enemyInstance.name = enemyPrefab.name;

        // Parent under the Enemies folder to keep the hierarchy clean during gameplay
        var enemiesGroup = GameObject.Find("Enemies");
        if (enemiesGroup != null)
        {
            enemyInstance.transform.SetParent(enemiesGroup.transform);
        }

        activeEnemiesCount++;
    }

    public void OnEnemyKilled()
    {
        activeEnemiesCount--;
        
        int totalRemaining = activeEnemiesCount + enemiesRemainingToSpawn;
        if (hud != null)
        {
            hud.UpdateEnemiesRemaining(totalRemaining);
        }

        if (totalRemaining <= 0)
        {
            StartCoroutine(StartNextWaveCo());
        }
    }
}
