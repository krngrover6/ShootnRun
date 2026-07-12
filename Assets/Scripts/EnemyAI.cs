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

    [Header("Ranged Attack Settings")]
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private float rangedRange = 16f;
    [SerializeField] private float rangedCooldown = 2.0f;

    [Header("Hover Animation")]
    [SerializeField] private float bobSpeed = 3f;
    [SerializeField] private float bobAmplitude = 0.2f;
    [SerializeField] private float wobbleSpeed = 4f;
    [SerializeField] private float wobbleAngle = 8f;

    [Header("Effects")]
    [SerializeField] private GameObject deathEffectPrefab;

    private float currentHealth;
    private NavMeshAgent agent;
    private Rigidbody rb;
    private CharacterBehaviour player;
    private Transform visualsTransform;
    private float lastAttackTime;
    private float randomOffset;
    private Vector3 initialVisualsPosition;

    // Hit feedback fields
    private Vector3 hitRecoilOffset;
    private float hitStunTimer;

    // Jumpy / Leap attack settings
    private bool isHopper;
    private bool isLeaping;
    private float leapTimeLimit;
    private float leapMaxDuration = 0.8f;
    private float leapForwardSpeed = 10f;
    private float leapUpwardSpeed = 5.5f;
    private float leapRange = 5.5f; // Jump towards player when this close

    // Ranged state
    private bool isRanged;
    private float lastRangedAttackTime;

    // Stuck rescue state
    private float stuckTimer;
    private Vector3 lastPosition;
    private int rescueStage;

    // Path repathing is throttled - recalculating every frame per enemy is wasted
    // CPU once several are alive at once, and the player doesn't move fast enough
    // for a short delay to matter.
    private float pathUpdateTimer;
    private const float pathUpdateInterval = 0.2f;

    public float MoveSpeed
    {
        get => speed;
        set
        {
            speed = value;
            if (agent != null)
            {
                agent.speed = value;
            }
        }
    }

    private void Start()
    {
        currentHealth = maxHealth;
        agent = GetComponent<NavMeshAgent>();
        rb = GetComponent<Rigidbody>();
        
        // Ensure Rigidbody starts kinematic so NavMeshAgent has control
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        // Randomly make some enemies "hoppers"
        isHopper = (Random.value > 0.4f);

        // Randomly make some enemies ranged (60% chance)
        isRanged = (Random.value > 0.4f);

        // Load default projectile if none specified
        if (projectilePrefab == null)
        {
            projectilePrefab = Resources.Load<GameObject>("EnemyProjectile");
        }

        var testingControls = FindFirstObjectByType<TestingControls>();
        if (testingControls != null)
        {
            speed = testingControls.EnemyMoveSpeed;
        }
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
        lastPosition = transform.position;
    }

private void Update()
    {
        if (player == null) return;

        // If currently executing a leap attack, handle physics updates
        if (isLeaping)
        {
            UpdateLeapAttack();
            return;
        }

        // Handle hit flinch/stun countdown
        if (hitStunTimer > 0)
        {
            hitStunTimer -= Time.deltaTime;
            if (hitStunTimer <= 0)
            {
                // Restore agent speed after flinch stun finishes
                agent.speed = speed;
            }
        }

        float distanceToPlayer = Vector3.Distance(transform.position, player.transform.position);

        // Follow player path using NavMeshAgent with SamplePosition fallback
        if (agent.enabled && agent.isOnNavMesh)
        {
            // Throttle repathing - recalculating a destination every single frame per
            // enemy is unnecessary CPU cost that adds up once several are alive at once.
            pathUpdateTimer -= Time.deltaTime;
            if (pathUpdateTimer <= 0f)
            {
                pathUpdateTimer = pathUpdateInterval;

                NavMeshHit nmHit;
                // Search inside a wide radius (30 meters) to find the closest valid NavMesh spot near player
                if (NavMesh.SamplePosition(player.transform.position, out nmHit, 30.0f, NavMesh.AllAreas))
                {
                    agent.SetDestination(nmHit.position);
                }
                else
                {
                    agent.SetDestination(player.transform.position);
                }
            }
        }
        else
        {
            // Off the NavMesh entirely: walk directly towards the player while the
            // rescue logic below works on getting us back onto the mesh.
            Vector3 directDir = (player.transform.position - transform.position);
            directDir.y = 0;
            if (directDir.sqrMagnitude > 0.01f)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(directDir), Time.deltaTime * 6f);
                transform.position = Vector3.MoveTowards(transform.position, new Vector3(player.transform.position.x, transform.position.y, player.transform.position.z), speed * Time.deltaTime);
            }
        }

        // --- Stuck detection with escalating rescue ---
        // Stage 1: snap to the nearest NavMesh point / leap over the obstruction.
        // Stage 2+: hard-relocate to a valid NavMesh spot near the player. An enemy the
        // player has to walk over to is worse than one that visibly repositions.
        if (distanceToPlayer > attackRange)
        {
            float moved = Vector3.Distance(transform.position, lastPosition);
            if (moved < 0.05f * Time.deltaTime * 60f)
            {
                stuckTimer += Time.deltaTime;
            }
            else
            {
                stuckTimer = 0f;
                rescueStage = 0;
            }

            if (stuckTimer >= 1.5f)
            {
                stuckTimer = 0f;
                rescueStage++;
                if (rescueStage == 1) TrySoftRescue();
                else RelocateNearPlayer();
            }
        }
        else
        {
            stuckTimer = 0f;
            rescueStage = 0;
        }
        lastPosition = transform.position;

        // Hover / Hop animation
        AnimateVisuals();

        // Attack selection logic
        if (distanceToPlayer <= attackRange)
        {
            AttackPlayer();
        }
        else if (isRanged && distanceToPlayer <= rangedRange && distanceToPlayer > attackRange * 2.5f && Time.time - lastRangedAttackTime >= rangedCooldown)
        {
            // Ranged projectile attack!
            ShootProjectile();
        }
        else if (!isRanged && distanceToPlayer <= leapRange && Time.time - lastAttackTime >= attackCooldown + 1f)
        {
            // Leap attack towards player when close and off cooldown!
            StartLeapAttack();
        }
    }

private void TrySoftRescue()
    {
        // First choice: snap to the nearest valid NavMesh point.
        if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 4f, NavMesh.AllAreas))
        {
            if (!agent.enabled) agent.enabled = true;
            agent.Warp(hit.position);
            if (agent.isOnNavMesh) agent.SetDestination(player.transform.position);
        }
        else
        {
            // No mesh nearby - try to physically leap out towards the player.
            StartLeapAttack();
        }
    }

    private void RelocateNearPlayer()
    {
        rescueStage = 0;

        // Try a ring of positions around the player (never right on top of them).
        for (int i = 0; i < 12; i++)
        {
            float angle = Random.Range(0f, Mathf.PI * 2f);
            float radius = Random.Range(6f, 14f);
            Vector3 candidate = player.transform.position + new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
            if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, 4f, NavMesh.AllAreas))
            {
                if (!agent.enabled) agent.enabled = true;
                agent.Warp(hit.position);
                if (agent.isOnNavMesh) agent.SetDestination(player.transform.position);
                return;
            }
        }

        // Last resort: closest mesh point to the player, whatever the distance.
        if (NavMesh.SamplePosition(player.transform.position, out NavMeshHit fallback, 30f, NavMesh.AllAreas))
        {
            if (!agent.enabled) agent.enabled = true;
            agent.Warp(fallback.position);
            if (agent.isOnNavMesh) agent.SetDestination(player.transform.position);
        }
    }


    private void AnimateVisuals()
    {
        if (visualsTransform == null) return;

        // Visual squash & stretch recovery
        visualsTransform.localScale = Vector3.Lerp(visualsTransform.localScale, Vector3.one, Time.deltaTime * 8f);

        // Visual hit recoil recovery
        hitRecoilOffset = Vector3.Lerp(hitRecoilOffset, Vector3.zero, Time.deltaTime * 10f);

        float time = Time.time * bobSpeed + randomOffset;
        float newY = initialVisualsPosition.y;

        if (isHopper)
        {
            // Make Hopper enemies bounce along the ground
            newY += Mathf.Abs(Mathf.Sin(time * 1.5f)) * (bobAmplitude * 2.2f);
        }
        else
        {
            // Standard smooth hovering
            newY += Mathf.Sin(time) * bobAmplitude;
        }
        
        visualsTransform.localPosition = new Vector3(initialVisualsPosition.x, newY, initialVisualsPosition.z) + hitRecoilOffset;

        // Wobbling side to side
        float wobbleTime = Time.time * wobbleSpeed + randomOffset;
        float wobbleRoll = Mathf.Sin(wobbleTime) * wobbleAngle;
        float wobblePitch = Mathf.Cos(wobbleTime * 0.7f) * (wobbleAngle * 0.5f);
        
        if (isHopper)
        {
            // Dynamic hopping tilt
            wobblePitch += Mathf.Sin(wobbleTime) * wobbleAngle * 0.5f;
        }

        visualsTransform.localRotation = Quaternion.Euler(wobblePitch, 0f, wobbleRoll);
    }

private void StartLeapAttack()
    {
        // Leap vector calculation
        Vector3 direction = (player.transform.position - transform.position);
        direction.y = 0f;
        direction.Normalize();

        // Guard against leaping into a wall or low ceiling - skip this leap rather than
        // launching the enemy through solid geometry, where it can end up wedged/stuck.
        float forwardCheckDistance = leapForwardSpeed * leapMaxDuration * 0.5f;
        if (Physics.Raycast(transform.position + Vector3.up * 0.5f, direction, forwardCheckDistance, ~0, QueryTriggerInteraction.Ignore))
        {
            return;
        }
        if (Physics.Raycast(transform.position + Vector3.up * 0.5f, Vector3.up, 2f, ~0, QueryTriggerInteraction.Ignore))
        {
            return;
        }

        isLeaping = true;
        leapTimeLimit = Time.time + leapMaxDuration;

        // Disable NavMeshAgent so we can apply custom physics velocity
        agent.enabled = false;

        // Configure Rigidbody for active physics movement
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = true;

            // Set velocity
            rb.linearVelocity = new Vector3(direction.x * leapForwardSpeed, leapUpwardSpeed, direction.z * leapForwardSpeed);
        }

        // Visual squash on launch!
        if (visualsTransform != null)
        {
            visualsTransform.localScale = new Vector3(0.7f, 1.4f, 0.7f);
        }
    }

    private bool IsGrounded()
    {
        // Simple raycast down from slightly above pivot to detect any floor or platform
        return Physics.Raycast(transform.position + Vector3.up * 0.15f, Vector3.down, 0.35f, ~0, QueryTriggerInteraction.Ignore);
    }

    private void UpdateLeapAttack()
    {
        if (player == null)
        {
            EndLeapAttack();
            return;
        }

        // Direct look rotation towards player during leap
        Vector3 targetDir = (player.transform.position - transform.position);
        targetDir.y = 0;
        if (targetDir.sqrMagnitude > 0.01f)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(targetDir), Time.deltaTime * 8f);
        }

        float distance = Vector3.Distance(transform.position, player.transform.position);

        // Check if we hit the player
        if (distance <= attackRange)
        {
            AttackPlayer();
            EndLeapAttack();
            return;
        }

        bool grounded = IsGrounded();

        // If the leap time limit is reached, zero out horizontal velocity and let gravity bring the enemy down
        if (Time.time >= leapTimeLimit)
        {
            if (rb != null)
            {
                rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
            }

            // Only transition back to NavMesh pathfinding once the enemy has touched the ground/walkway
            if (grounded)
            {
                EndLeapAttack();
            }
        }
        else if (rb != null && grounded && rb.linearVelocity.y <= 0.1f)
        {
            // Landing on the ground/walkway ends the leap attack immediately
            EndLeapAttack();
        }
    }

private void EndLeapAttack()
    {
        isLeaping = false;
        lastAttackTime = Time.time;

        if (rb != null)
        {
            // Zero linear velocity BEFORE setting kinematic to avoid Unity console warnings
            rb.linearVelocity = Vector3.zero;
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        // Re-enable NavMeshAgent
        agent.enabled = true;

        // Safety net: if the landing spot isn't on the NavMesh (e.g. wedged against geometry
        // from the leap), snap back to the closest valid point so it doesn't stay stuck.
        if (!agent.isOnNavMesh && NavMesh.SamplePosition(transform.position, out NavMeshHit landHit, 10f, NavMesh.AllAreas))
        {
            agent.Warp(landHit.position);
        }

        if (agent.isOnNavMesh)
        {
            agent.SetDestination(player.transform.position);
        }

        // Visual squash on landing!
        if (visualsTransform != null)
        {
            visualsTransform.localScale = new Vector3(1.4f, 0.6f, 1.4f);
        }
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

    private void ShootProjectile()
    {
        if (projectilePrefab == null) return;

        // Face player direction
        Vector3 shootDir = (player.transform.position - transform.position);
        shootDir.y = 0;
        if (shootDir.sqrMagnitude > 0.01f)
        {
            transform.rotation = Quaternion.LookRotation(shootDir);
        }

        // Spawn projectile slightly above the enemy center and aiming at player chest
        Vector3 spawnPos = transform.position + Vector3.up * 1f + transform.forward * 0.5f;
        Vector3 targetPos = player.transform.position + Vector3.up * 1.1f;
        
        Vector3 projDirection = (targetPos - spawnPos).normalized;
        Quaternion projRotation = Quaternion.LookRotation(projDirection);

        var projGo = Instantiate(projectilePrefab, spawnPos, projRotation);
        
        lastRangedAttackTime = Time.time;

        // Play visual squash on visuals when firing
        if (visualsTransform != null)
        {
            visualsTransform.localScale = new Vector3(1.15f, 0.8f, 1.15f);
        }
    }

    public void TakeDamage(float damageAmount)
    {
        currentHealth -= damageAmount;

        // Play Hit Reaction
        if (visualsTransform != null)
        {
            // Visual knockback shake (push visuals opposite to forward direction)
            hitRecoilOffset = -transform.forward * 0.45f;
            // Visual squash and stretch
            visualsTransform.localScale = new Vector3(1.3f, 0.6f, 1.3f);
        }

        // Temporarily slow down enemy (recoil flinch)
        hitStunTimer = 0.25f;
        if (agent != null && agent.enabled)
        {
            agent.speed = speed * 0.15f;
        }

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
