using UnityEngine;

public class EnemyProjectile : MonoBehaviour
{
    [Header("Settings")]
    public float speed = 16f;
    public float damage = 12f;
    public float lifetime = 5f;

    private void Start()
    {
        Destroy(gameObject, lifetime);
    }

    private void Update()
    {
        // Move forward in world space
        transform.Translate(Vector3.forward * speed * Time.deltaTime);
    }

    private void OnTriggerEnter(Collider other)
    {
        // Ignore enemy collision
        if (other.GetComponentInParent<EnemyAI>() != null || other.GetComponent<EnemyAI>() != null)
            return;

        // If it hits the player character
        var playerHealth = other.GetComponent<PlayerHealth>() ?? other.GetComponentInParent<PlayerHealth>();
        if (playerHealth != null)
        {
            playerHealth.TakeDamage(damage);
            SpawnHitEffect();
            Destroy(gameObject);
            return;
        }

        // Ignore triggers (like radar colliders or spawn zones)
        if (other.isTrigger) return;

        // Hit a wall, floor or obstacle
        SpawnHitEffect();
        Destroy(gameObject);
    }

    private void SpawnHitEffect()
    {
        // Optional: Spawn a tiny hit explosion or spark
        var spark = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        spark.transform.position = transform.position;
        spark.transform.localScale = Vector3.one * 0.15f;
        
        var renderer = spark.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material.color = Color.red;
        }
        
        Destroy(spark, 0.3f);
    }
}
