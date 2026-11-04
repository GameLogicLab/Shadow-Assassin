using UnityEngine;
using UnityEngine.AI;

public class EnemyAI : MonoBehaviour
{
    public Transform player;

    [Header("Movement")]
    public float patrolSpeed = 3f;
    public float chaseSpeed = 4f;
    public float rotationSpeed = 8f;

    [Header("Patrol")]
    public float roamRadius = 10f;
    public float waitAtRoamPoint = 3f;

    [Header("Player Detection")]
    public float rayDistance = 5f;
    public float rayHeight = 2f;

    [Range(3, 30)]
    public int rayCount = 15;

    public float detectionAngle = 90f;

    [Header("Player Memory")]
    public float playerMemoryTime = 3f;

    [Header("Investigation")]
    public float investigationRadius = 5f;

    [Range(1, 10)]
    public int investigationPoints = 5;

    public float waitAtInvestigationPoint = 1f;

    [Header("Combat")]
    public float attackDistance = 1.5f;
    public float attackDamage = 20f;
    public float attackCooldown = 1.5f;
    public float attackDelay = 1.2f;

    [Header("Chase Stopping Distance")]
    public float stopDistance = 1.5f;

    [Header("Attack Detection")]
    public float attackRayHeight = 1f;

    [Range(3, 15)]
    public int attackRayCount = 7;

    public float attackAngle = 90f;

    [Header("Enemy Health")]
    public float maxHealth = 100f;

    [Header("Death")]
    public float deathDespawnDelay = 2f;

    [Header("Animations")]
    public Animator animator;

    [Header("Layers")]
    public LayerMask playerLayer;
    public LayerMask obstacleLayer;

    private NavMeshAgent agent;

    private float currentHealth;
    private bool isDead = false;
    private bool killedByKunai = false;

    public bool IsDead => isDead;

    public GameManager gameManager;

    enum EnemyState
    {
        Patrol,
        Chase,
        LostPlayer,
        Investigate
    }

    private EnemyState currentState = EnemyState.Patrol;

    private Vector3 roamPoint;
    private bool hasRoamPoint = false;

    private bool isWaiting = false;
    private float waitTimer = 0f;

    private Vector3 lastKnownPlayerPosition;
    private bool hasLastKnownPlayerPosition = false;

    private float playerMemoryTimer = 0f;

    private Vector3 investigationPoint;
    private bool hasInvestigationPoint = false;

    private int investigationCount = 0;

    private bool waitingAtInvestigationPoint = false;
    private float investigationWaitTimer = 0f;

    private float attackTimer = 0f;
    private bool isAttacking = false;
    private float attackDelayTimer = 0f;

    [Header("Audio")]
    public AudioSource enemyAudioSource;
    public AudioClip walkingAudio;
    public AudioClip runningAudio;
    public AudioClip attackingAudio;

    private AudioClip currentMovementClip;

    [Header("Blood Effect")]
    public GameObject bloodEffectPrefab;
    public Vector3 bloodEffectOffset = new Vector3(0f, 1f, 0f);

    [Header("Death Sounds")]
    public AudioClip knifeDeathSound;
    public AudioClip kunaiDeathSound;

    [Range(0f, 1f)]
    public float deathSoundVolume = 1f;

    // Cached Animator hashes for high performance (avoids string lookup every frame)
    private static readonly int IdleHash = Animator.StringToHash("Idle");
    private static readonly int WalkHash = Animator.StringToHash("Walk");
    private static readonly int RunHash = Animator.StringToHash("Run");
    private static readonly int AttackTriggerHash = Animator.StringToHash("Attack");
    private static readonly int KnifeDeathTriggerHash = Animator.StringToHash("KnifeDeath");
    private static readonly int KunaiDeathTriggerHash = Animator.StringToHash("KunaiDeath");

    private string currentAnimState = string.Empty;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        if (agent != null)
        {
            agent.updateRotation = false;
            agent.speed = patrolSpeed;
            agent.stoppingDistance = stopDistance;
        }

        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }

        if (player == null)
        {
            PlayerMovement pm = FindAnyObjectByType<PlayerMovement>();
            if (pm != null)
            {
                player = pm.transform;
            }
        }

        if (gameManager == null)
        {
            gameManager = GameManager.Instance;
        }

        currentHealth = maxHealth;
        PickRandomRoamPoint();
        SetAnimation("Walk");
    }

    void Update()
    {
        if (isDead)
            return;

        if (player == null)
            return;

        bool playerSeen = DetectPlayer();

        if (playerSeen)
        {
            lastKnownPlayerPosition = player.position;
            hasLastKnownPlayerPosition = true;
            playerMemoryTimer = playerMemoryTime;
            currentState = EnemyState.Chase;
            hasInvestigationPoint = false;
            investigationCount = 0;
            waitingAtInvestigationPoint = false;
        }

        switch (currentState)
        {
            case EnemyState.Patrol:
                Patrol();
                break;

            case EnemyState.Chase:
                Chase();
                break;

            case EnemyState.LostPlayer:
                LostPlayer();
                break;

            case EnemyState.Investigate:
                Investigate();
                break;
        }

        if (currentState != EnemyState.Chase)
        {
            SmoothRotation();
        }
    }

    bool DetectPlayer()
    {
        if (player == null) return false;

        Vector3 toPlayer = player.position - transform.position;
        float sqrDist = toPlayer.sqrMagnitude;

        // Fast distance pre-check: skip all raycasts if player is out of range
        if (sqrDist > rayDistance * rayDistance)
        {
            return false;
        }

        // Fast angle pre-check: skip if player is behind or outside the vision cone
        Vector3 flatToPlayer = new Vector3(toPlayer.x, 0f, toPlayer.z);
        float angleToPlayer = Vector3.Angle(transform.forward, flatToPlayer);
        if (angleToPlayer > detectionAngle * 0.5f)
        {
            return false;
        }

        Vector3 origin = transform.position + Vector3.up * rayHeight;
        Vector3 targetPos = player.position + Vector3.up * rayHeight;
        Vector3 directDir = targetPos - origin;
        float directDist = directDir.magnitude;

        // Direct line of sight raycast
        if (Physics.Raycast(origin, directDir.normalized, out RaycastHit directHit, directDist, playerLayer | obstacleLayer))
        {
            if (IsLayer(directHit.collider.gameObject, playerLayer))
            {
                return true;
            }
        }

        // Peripheral raycasts fallback if direct center ray was clipped by a close edge
        for (int i = 0; i < rayCount; i++)
        {
            float angle = -detectionAngle / 2f + (detectionAngle / (rayCount - 1)) * i;
            Vector3 direction = Quaternion.Euler(0f, angle, 0f) * transform.forward;

            if (Physics.Raycast(origin, direction, out RaycastHit hit, rayDistance, playerLayer | obstacleLayer))
            {
                if (IsLayer(hit.collider.gameObject, playerLayer))
                {
                    return true;
                }
            }
        }

        return false;
    }

    void Patrol()
    {
        if (agent == null) return;

        agent.speed = patrolSpeed;
        agent.isStopped = false;

        if (isWaiting)
        {
            SetAnimation("Idle");
            waitTimer -= Time.deltaTime;

            if (waitTimer <= 0f)
            {
                isWaiting = false;
                PickRandomRoamPoint();
            }

            return;
        }

        if (!hasRoamPoint)
        {
            PickRandomRoamPoint();
            return;
        }

        agent.SetDestination(roamPoint);
        SetAnimation("Walk");

        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            isWaiting = true;
            waitTimer = waitAtRoamPoint;
            SetAnimation("Idle");
        }
    }

    void Chase()
    {
        if (agent == null || player == null) return;

        agent.speed = chaseSpeed;

        Vector3 direction = player.position - transform.position;
        direction.y = 0f;
        float distanceToPlayer = direction.magnitude;

        // Stop moving when close enough to attack.
        if (distanceToPlayer <= stopDistance)
        {
            agent.isStopped = true;
            FacePlayer();

            if (IsPlayerInAttackRange())
            {
                Attack();
            }
            else
            {
                SetAnimation("Idle");
            }

            return;
        }

        // Continue chasing the player.
        if (DetectPlayer())
        {
            lastKnownPlayerPosition = player.position;
            playerMemoryTimer = playerMemoryTime;

            agent.isStopped = false;
            agent.stoppingDistance = stopDistance;
            agent.SetDestination(player.position);

            FacePlayer();
            SetAnimation("Run");
            return;
        }

        playerMemoryTimer = playerMemoryTime;
        currentState = EnemyState.LostPlayer;
    }

    void FacePlayer()
    {
        if (player == null) return;

        Vector3 direction = player.position - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.001f) return;

        Quaternion targetRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            rotationSpeed * Time.deltaTime
        );
    }

    bool IsPlayerInAttackRange()
    {
        if (player == null) return false;

        Vector3 toPlayer = player.position - transform.position;
        if (toPlayer.sqrMagnitude > attackDistance * attackDistance)
        {
            return false;
        }

        Vector3 origin = transform.position + Vector3.up * attackRayHeight;
        Vector3 targetPos = player.position + Vector3.up * attackRayHeight;
        Vector3 directDir = targetPos - origin;
        float directDist = directDir.magnitude;

        if (Physics.Raycast(origin, directDir.normalized, out RaycastHit directHit, directDist, playerLayer | obstacleLayer))
        {
            if (IsLayer(directHit.collider.gameObject, playerLayer))
            {
                return true;
            }
        }

        for (int i = 0; i < attackRayCount; i++)
        {
            float angle = -attackAngle / 2f + (attackAngle / (attackRayCount - 1)) * i;
            Vector3 direction = Quaternion.Euler(0f, angle, 0f) * transform.forward;

            if (Physics.Raycast(origin, direction, out RaycastHit hit, attackDistance, playerLayer | obstacleLayer))
            {
                if (IsLayer(hit.collider.gameObject, playerLayer))
                {
                    return true;
                }
            }
        }

        return false;
    }

    void Attack()
    {
        if (agent != null) agent.isStopped = true;

        SetAnimation("Idle");

        if (player != null)
        {
            Vector3 direction = player.position - transform.position;
            direction.y = 0f;

            if (direction.sqrMagnitude > 0.001f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    targetRotation,
                    rotationSpeed * Time.deltaTime
                );
            }
        }

        if (isAttacking)
        {
            attackDelayTimer -= Time.deltaTime;

            if (attackDelayTimer <= 0f)
            {
                DealDamage();
                isAttacking = false;
                attackTimer = attackCooldown;
            }

            if (!IsPlayerInAttackRange())
            {
                isAttacking = false;
                if (agent != null) agent.isStopped = false;
                currentState = EnemyState.Chase;
            }

            return;
        }

        attackTimer -= Time.deltaTime;

        if (attackTimer <= 0f)
        {
            isAttacking = true;
            attackDelayTimer = attackDelay;

            if (animator != null)
            {
                animator.SetTrigger(AttackTriggerHash);
            }

            PlayAttackAudio();
        }
    }

    void DealDamage()
    {
        if (player == null) return;

        if (!IsPlayerInAttackRange())
        {
            return;
        }

        PlayerHealth playerHealth = player.GetComponent<PlayerHealth>();
        if (playerHealth != null)
        {
            playerHealth.TakeDmg(attackDamage);
        }
    }

    public void TakeDamage(float damage)
    {
        TakeDamage(damage, false);
    }

    public void TakeDamage(float damage, bool isKunai)
    {
        if (isDead) return;

        killedByKunai = isKunai;

        if (bloodEffectPrefab != null)
        {
            Vector3 effectPosition = transform.position + bloodEffectOffset;
            GameObject effect = Instantiate(bloodEffectPrefab, effectPosition, Quaternion.identity);

            ParticleSystem[] particleSystems = effect.GetComponentsInChildren<ParticleSystem>(true);
            float longestEffectTime = 0f;

            foreach (ParticleSystem particles in particleSystems)
            {
                particles.gameObject.SetActive(true);
                particles.Play(true);

                var main = particles.main;
                float lifetime = main.startLifetime.constantMax;
                float effectTime = main.duration + lifetime;

                if (main.startDelay.constantMax > 0f)
                    effectTime += main.startDelay.constantMax;

                longestEffectTime = Mathf.Max(longestEffectTime, effectTime);
            }

            Destroy(effect, particleSystems.Length > 0 ? longestEffectTime + 0.2f : 2f);
        }

        currentHealth -= damage;
        currentHealth = Mathf.Max(currentHealth, 0f);

        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    void Die()
    {
        if (isDead) return;
        isDead = true;

        StopMovementAudio();

        if (agent != null)
        {
            agent.isStopped = true;
            agent.enabled = false;
        }

        isAttacking = false;

        Collider enemyCollider = GetComponent<Collider>();
        if (enemyCollider != null)
        {
            enemyCollider.enabled = false;
        }

        if (animator != null)
        {
            animator.SetTrigger(killedByKunai ? KunaiDeathTriggerHash : KnifeDeathTriggerHash);
        }

        AudioClip deathSound = killedByKunai ? kunaiDeathSound : knifeDeathSound;
        PlayDeathSound(deathSound);

        Invoke(nameof(Despawn), deathDespawnDelay);

        if (gameManager == null)
        {
            gameManager = GameManager.Instance;
        }

        if (gameManager != null)
        {
            gameManager.ShowNotification(1);
        }
    }

    private void PlayDeathSound(AudioClip clip)
    {
        if (clip == null) return;
        AudioSource.PlayClipAtPoint(clip, transform.position, deathSoundVolume);
    }

    void Despawn()
    {
        Destroy(gameObject);
    }

    void LostPlayer()
    {
        if (agent == null) return;

        agent.speed = chaseSpeed;
        agent.isStopped = false;

        SetAnimation("Idle");

        if (DetectPlayer())
        {
            currentState = EnemyState.Chase;
            playerMemoryTimer = playerMemoryTime;
            return;
        }

        playerMemoryTimer -= Time.deltaTime;

        if (hasLastKnownPlayerPosition)
        {
            agent.SetDestination(lastKnownPlayerPosition);
            SetAnimation("Run");
        }

        if (playerMemoryTimer <= 0f)
        {
            playerMemoryTimer = 0f;
            investigationCount = 0;
            hasInvestigationPoint = false;
            waitingAtInvestigationPoint = false;
            currentState = EnemyState.Investigate;
        }
    }

    void Investigate()
    {
        if (agent == null) return;

        agent.speed = patrolSpeed;
        agent.isStopped = false;

        if (DetectPlayer())
        {
            currentState = EnemyState.Chase;
            investigationCount = 0;
            hasInvestigationPoint = false;
            waitingAtInvestigationPoint = false;
            return;
        }

        if (waitingAtInvestigationPoint)
        {
            SetAnimation("Idle");
            investigationWaitTimer -= Time.deltaTime;

            if (investigationWaitTimer <= 0f)
            {
                waitingAtInvestigationPoint = false;
                investigationCount++;
                hasInvestigationPoint = false;
            }

            return;
        }

        if (investigationCount >= investigationPoints)
        {
            hasLastKnownPlayerPosition = false;
            hasInvestigationPoint = false;
            investigationCount = 0;
            currentState = EnemyState.Patrol;
            hasRoamPoint = false;
            return;
        }

        if (!hasInvestigationPoint)
        {
            PickInvestigationPoint();
            return;
        }

        agent.SetDestination(investigationPoint);
        SetAnimation("Walk");

        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            waitingAtInvestigationPoint = true;
            investigationWaitTimer = waitAtInvestigationPoint;
            SetAnimation("Idle");
        }
    }

    void PickRandomRoamPoint()
    {
        if (agent == null) return;

        Vector3 randomDirection = Random.insideUnitSphere * roamRadius;
        randomDirection += transform.position;

        if (NavMesh.SamplePosition(randomDirection, out NavMeshHit hit, roamRadius, NavMesh.AllAreas))
        {
            roamPoint = hit.position;
            hasRoamPoint = true;
            agent.SetDestination(roamPoint);
        }
        else
        {
            hasRoamPoint = false;
        }
    }

    void PickInvestigationPoint()
    {
        if (agent == null) return;

        Vector3 randomDirection = Random.insideUnitSphere * investigationRadius;
        randomDirection += lastKnownPlayerPosition;

        if (NavMesh.SamplePosition(randomDirection, out NavMeshHit hit, investigationRadius, NavMesh.AllAreas))
        {
            investigationPoint = hit.position;
            hasInvestigationPoint = true;
            agent.SetDestination(investigationPoint);
        }
        else
        {
            hasInvestigationPoint = false;
        }
    }

    bool IsLayer(GameObject obj, LayerMask layer)
    {
        return (layer.value & (1 << obj.layer)) != 0;
    }

    void SetAnimation(string animationName)
    {
        if (animator == null) return;

        // Skip if already in this animation state
        if (currentAnimState == animationName) return;
        currentAnimState = animationName;

        animator.SetBool(IdleHash, animationName == "Idle");
        animator.SetBool(WalkHash, animationName == "Walk");
        animator.SetBool(RunHash, animationName == "Run");

        if (animationName == "Walk")
        {
            PlayMovementAudio(walkingAudio);
        }
        else if (animationName == "Run")
        {
            PlayMovementAudio(runningAudio);
        }
        else
        {
            StopMovementAudio();
        }
    }

    void SmoothRotation()
    {
        if (agent == null) return;

        Vector3 direction = agent.steeringTarget - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                rotationSpeed * Time.deltaTime
            );
        }
    }

    void PlayMovementAudio(AudioClip clip)
    {
        if (enemyAudioSource == null) return;

        if (currentMovementClip == clip && enemyAudioSource.isPlaying)
            return;

        currentMovementClip = clip;
        enemyAudioSource.Stop();

        if (clip != null)
        {
            enemyAudioSource.clip = clip;
            enemyAudioSource.loop = true;
            enemyAudioSource.Play();
        }
    }

    void StopMovementAudio()
    {
        currentMovementClip = null;

        if (enemyAudioSource != null)
        {
            enemyAudioSource.Stop();
            enemyAudioSource.loop = false;
        }
    }

    void PlayAttackAudio()
    {
        if (attackingAudio == null) return;
        AudioSource.PlayClipAtPoint(attackingAudio, transform.position, 1f);
    }

    void OnDrawGizmos()
    {
        Vector3 origin = transform.position + Vector3.up * rayHeight;

        for (int i = 0; i < rayCount; i++)
        {
            float angle = -detectionAngle / 2f + (detectionAngle / (rayCount - 1)) * i;
            Vector3 direction = Quaternion.Euler(0f, angle, 0f) * transform.forward;
            Gizmos.color = Color.red;

            if (Physics.Raycast(origin, direction, out RaycastHit hit, rayDistance))
            {
                if (IsLayer(hit.collider.gameObject, playerLayer))
                {
                    Gizmos.color = Color.green;
                }
                else if (IsLayer(hit.collider.gameObject, obstacleLayer))
                {
                    Gizmos.color = Color.yellow;
                }

                Gizmos.DrawRay(origin, direction.normalized * hit.distance);
                Gizmos.DrawSphere(hit.point, 0.08f);
            }
            else
            {
                Gizmos.DrawRay(origin, direction.normalized * rayDistance);
            }
        }

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, roamRadius);

        if (hasLastKnownPlayerPosition)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(lastKnownPlayerPosition, 0.3f);
            Gizmos.DrawWireSphere(lastKnownPlayerPosition, investigationRadius);
        }

        if (hasInvestigationPoint)
        {
            Gizmos.color = Color.white;
            Gizmos.DrawSphere(investigationPoint, 0.2f);
        }

        Vector3 attackOrigin = transform.position + Vector3.up * attackRayHeight;

        for (int i = 0; i < attackRayCount; i++)
        {
            float angle = -attackAngle / 2f + (attackAngle / (attackRayCount - 1)) * i;
            Vector3 direction = Quaternion.Euler(0f, angle, 0f) * transform.forward;
            Gizmos.color = Color.blue;

            if (Physics.Raycast(attackOrigin, direction, out RaycastHit hit, attackDistance))
            {
                if (IsLayer(hit.collider.gameObject, playerLayer))
                {
                    Gizmos.color = Color.green;
                }
                else if (IsLayer(hit.collider.gameObject, obstacleLayer))
                {
                    Gizmos.color = Color.yellow;
                }

                Gizmos.DrawRay(attackOrigin, direction.normalized * hit.distance);
                Gizmos.DrawSphere(hit.point, 0.06f);
            }
            else
            {
                Gizmos.DrawRay(attackOrigin, direction.normalized * attackDistance);
            }
        }

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackDistance);
    }
}
