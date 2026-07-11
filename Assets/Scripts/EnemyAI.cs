using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using InfimaGames.LowPolyShooterPack;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyAI : MonoBehaviour
{
    [Header("Stats")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float damage = 10f;
    [SerializeField] private float attackCooldown = 1.5f;
    [SerializeField] private float attackRange = 2f;
    
    [Header("Movement")]
    [SerializeField] private float speed = 3.5f;

    [Header("Hover Animation")]
    [SerializeField] private float bobSpeed = 3f;
    [SerializeField] private float bobAmplitude = 0.2f;
    [SerializeField] private float wobbleSpeed = 4f;
    [SerializeField] private float wobbleAngle = 8f;

    [Header("Effects")]
    [SerializeField] private GameObject deathEffectPrefab;

    private float currentHealth;
    private NavMeshAgent agent;
    private CharacterBehaviour player;
    private Transform visualsTransform;
    private float lastAttackTime;
    private float randomOffset;
    private Vector3 initialVisualsPosition;

    private void Start()
    {
        currentHealth = maxHealth;
        agent = GetComponent<NavMeshAgent>();
        agent.speed = speed;
        
        // Find player character via Infima Games Service Locator
        var gameModeService = ServiceLocator.Current.Get<IGameModeService>();
        if (gameModeService != null)
        {
            player = gameModeService.GetPlayerCharacter();
        }

        // Find the visual child container
        visualsTransform = transform.Find("Visuals");
        if (visualsTransform != null)
        {
            initialVisualsPosition = visualsTransform.localPosition;
        }

        randomOffset = Random.Range(0f, 100f);
    }

    private void Update()
    {
        if (player == null) return;

        // Follow player path using NavMeshAgent
        if (agent.isOnNavMesh)
        {
            agent.SetDestination(player.transform.position);
        }

        // Hover bobbing/wobbling animation
        AnimateHover();

        // Attack logic
        float distanceToPlayer = Vector3.Distance(transform.position, player.transform.position);
        if (distanceToPlayer <= attackRange)
        {
            AttackPlayer();
        }
    }

    private void AnimateHover()
    {
        if (visualsTransform == null) return;

        float time = Time.time * bobSpeed + randomOffset;
        
        // Bobbing up and down
        float newY = initialVisualsPosition.y + Mathf.Sin(time) * bobAmplitude;
        visualsTransform.localPosition = new Vector3(initialVisualsPosition.x, newY, initialVisualsPosition.z);

        // Wobbling side to side
        float wobbleTime = Time.time * wobbleSpeed + randomOffset;
        float wobbleRoll = Mathf.Sin(wobbleTime) * wobbleAngle;
        float wobblePitch = Mathf.Cos(wobbleTime * 0.7f) * (wobbleAngle * 0.5f);
        visualsTransform.localRotation = Quaternion.Euler(wobblePitch, 0f, wobbleRoll);
    }

    private void AttackPlayer()
    {
        if (Time.time - lastAttackTime >= attackCooldown)
        {
            var playerHealth = player.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(damage);
                lastAttackTime = Time.time;
            }
        }
    }

    public void TakeDamage(float damageAmount)
    {
        currentHealth -= damageAmount;
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        if (deathEffectPrefab != null)
        {
            Instantiate(deathEffectPrefab, transform.position, Quaternion.identity);
        }
        
        // Notify GameController if present
        var controller = FindFirstObjectByType<GameController>();
        if (controller != null)
        {
            controller.OnEnemyKilled();
        }

        Destroy(gameObject);
    }
}
