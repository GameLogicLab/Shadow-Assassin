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

    public float attackDelay = 0.5f;

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

    enum EnemyState
    {
        Patrol,
        Chase,
        LostPlayer,
        Investigate
    }

    EnemyState currentState = EnemyState.Patrol;

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


    void Start()
    {
        agent = GetComponent<NavMeshAgent>();

        agent.updateRotation = false;

        agent.speed = patrolSpeed;

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
        Vector3 origin =
            transform.position +
            Vector3.up * rayHeight;

        for (int i = 0; i < rayCount; i++)
        {
            float angle =
                -detectionAngle / 2f +
                (detectionAngle /
                (rayCount - 1)) * i;

            Vector3 direction =
                Quaternion.Euler(
                    0f,
                    angle,
                    0f
                ) * transform.forward;

            if (Physics.Raycast(
                origin,
                direction,
                out RaycastHit hit,
                rayDistance))
            {
                if (IsLayer(
                    hit.collider.gameObject,
                    playerLayer))
                {
                    return true;
                }

                if (IsLayer(
                    hit.collider.gameObject,
                    obstacleLayer))
                {
                    continue;
                }
            }
        }

        return false;
    }


    void Patrol()
    {
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


        if (!agent.pathPending &&
            agent.remainingDistance <= agent.stoppingDistance)
        {
            isWaiting = true;

            waitTimer = waitAtRoamPoint;

            SetAnimation("Idle");
        }
    }


    void Chase()
    {
        agent.speed = chaseSpeed;

        if (player == null)
            return;


        if (IsPlayerInAttackRange())
        {
            Attack();

            return;
        }


        if (DetectPlayer())
        {
            lastKnownPlayerPosition = player.position;

            playerMemoryTimer = playerMemoryTime;

            agent.isStopped = false;

            agent.SetDestination(player.position);

            // NEW:
            // Smoothly face the player while chasing
            FacePlayer();

            SetAnimation("Run");

            return;
        }


        playerMemoryTimer = playerMemoryTime;

        currentState = EnemyState.LostPlayer;
    }


    // =========================================================
    // NEW: FACE PLAYER
    // =========================================================

    void FacePlayer()
    {
        if (player == null)
            return;

        Vector3 direction =
            player.position -
            transform.position;

        direction.y = 0f;

        if (direction == Vector3.zero)
            return;

        Quaternion targetRotation =
            Quaternion.LookRotation(direction);

        transform.rotation =
            Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                rotationSpeed * Time.deltaTime
            );
    }


    bool IsPlayerInAttackRange()
    {
        Vector3 origin =
            transform.position +
            Vector3.up * attackRayHeight;

        for (int i = 0; i < attackRayCount; i++)
        {
            float angle =
                -attackAngle / 2f +
                (attackAngle /
                (attackRayCount - 1)) * i;

            Vector3 direction =
                Quaternion.Euler(
                    0f,
                    angle,
                    0f
                ) * transform.forward;

            if (Physics.Raycast(
                origin,
                direction,
                out RaycastHit hit,
                attackDistance))
            {
                if (IsLayer(
                    hit.collider.gameObject,
                    playerLayer))
                {
                    return true;
                }

                if (IsLayer(
                    hit.collider.gameObject,
                    obstacleLayer))
                {
                    continue;
                }
            }
        }

        return false;
    }


    void Attack()
    {
        agent.isStopped = true;

        SetAnimation("Idle");

        if (player != null)
        {
            Vector3 direction =
                player.position -
                transform.position;

            direction.y = 0f;

            if (direction != Vector3.zero)
            {
                Quaternion targetRotation =
                    Quaternion.LookRotation(direction);

                transform.rotation =
                    Quaternion.Slerp(
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

                agent.isStopped = false;

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
                animator.SetTrigger("Attack");
            }

            Debug.Log("Enemy started attack!");
        }
    }


    void DealDamage()
    {
        if (player == null)
            return;

        if (!IsPlayerInAttackRange())
        {
            Debug.Log("Attack missed!");

            return;
        }


        PlayerHealth playerHealth =
            player.GetComponent<PlayerHealth>();


        if (playerHealth != null)
        {
            playerHealth.TakeDmg(attackDamage);

            Debug.Log(
                "Enemy dealt " +
                attackDamage +
                " damage!"
            );
        }
    }


    public void TakeDamage(float damage)
    {
        if (isDead)
            return;

        currentHealth -= damage;

        currentHealth =
            Mathf.Max(
                currentHealth,
                0f
            );

        Debug.Log(
            "Enemy Health: " +
            currentHealth
        );


        if (currentHealth <= 0f)
        {
            Die();
        }
    }


    void Die()
    {
        if (isDead)
            return;

        isDead = true;


        if (agent != null)
        {
            agent.isStopped = true;
            agent.enabled = false;
        }


        isAttacking = false;


        Collider enemyCollider =
            GetComponent<Collider>();

        if (enemyCollider != null)
        {
            enemyCollider.enabled = false;
        }


        if (animator != null)
        {
            animator.SetTrigger("Death");
        }


        Debug.Log("Enemy Died!");


        Invoke(
            nameof(Despawn),
            deathDespawnDelay
        );
    }


    void Despawn()
    {
        Destroy(gameObject);
    }


    void LostPlayer()
    {
        agent.speed = chaseSpeed;

        agent.isStopped = false;

        SetAnimation("Walk");


        if (DetectPlayer())
        {
            currentState = EnemyState.Chase;

            playerMemoryTimer = playerMemoryTime;

            return;
        }


        playerMemoryTimer -= Time.deltaTime;


        if (hasLastKnownPlayerPosition)
        {
            agent.SetDestination(
                lastKnownPlayerPosition
            );

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


        agent.SetDestination(
            investigationPoint
        );

        SetAnimation("Walk");


        if (!agent.pathPending &&
            agent.remainingDistance <= agent.stoppingDistance)
        {
            waitingAtInvestigationPoint = true;

            investigationWaitTimer =
                waitAtInvestigationPoint;

            SetAnimation("Idle");
        }
    }


    void PickRandomRoamPoint()
    {
        Vector3 randomDirection =
            Random.insideUnitSphere *
            roamRadius;

        randomDirection +=
            transform.position;


        if (NavMesh.SamplePosition(
            randomDirection,
            out NavMeshHit hit,
            roamRadius,
            NavMesh.AllAreas))
        {
            roamPoint =
                hit.position;

            hasRoamPoint = true;

            agent.SetDestination(
                roamPoint
            );
        }
        else
        {
            hasRoamPoint = false;
        }
    }


    void PickInvestigationPoint()
    {
        Vector3 randomDirection =
            Random.insideUnitSphere *
            investigationRadius;

        randomDirection +=
            lastKnownPlayerPosition;


        if (NavMesh.SamplePosition(
            randomDirection,
            out NavMeshHit hit,
            investigationRadius,
            NavMesh.AllAreas))
        {
            investigationPoint =
                hit.position;

            hasInvestigationPoint = true;

            agent.SetDestination(
                investigationPoint
            );
        }
        else
        {
            hasInvestigationPoint = false;
        }
    }


    bool IsLayer(
        GameObject obj,
        LayerMask layer)
    {
        return
            (layer.value &
            (1 << obj.layer)) != 0;
    }


    void SetAnimation(
        string animationName)
    {
        if (animator == null)
            return;


        animator.SetBool(
            "Idle",
            animationName == "Idle"
        );


        animator.SetBool(
            "Walk",
            animationName == "Walk"
        );


        animator.SetBool(
            "Run",
            animationName == "Run"
        );
    }


    void SmoothRotation()
    {
        if (agent == null)
            return;


        Vector3 direction =
            agent.steeringTarget -
            transform.position;

        direction.y = 0f;


        if (direction != Vector3.zero)
        {
            Quaternion targetRotation =
                Quaternion.LookRotation(direction);


            transform.rotation =
                Quaternion.Slerp(
                    transform.rotation,
                    targetRotation,
                    rotationSpeed *
                    Time.deltaTime
                );
        }
    }


    void OnDrawGizmos()
    {
        Vector3 origin =
            transform.position +
            Vector3.up * rayHeight;


        for (int i = 0; i < rayCount; i++)
        {
            float angle =
                -detectionAngle / 2f +
                (detectionAngle /
                (rayCount - 1)) * i;


            Vector3 direction =
                Quaternion.Euler(
                    0f,
                    angle,
                    0f
                ) * transform.forward;


            Gizmos.color = Color.red;


            if (Physics.Raycast(
                origin,
                direction,
                out RaycastHit hit,
                rayDistance))
            {
                if (IsLayer(
                    hit.collider.gameObject,
                    playerLayer))
                {
                    Gizmos.color = Color.green;
                }
                else if (IsLayer(
                    hit.collider.gameObject,
                    obstacleLayer))
                {
                    Gizmos.color = Color.yellow;
                }


                Gizmos.DrawRay(
                    origin,
                    direction.normalized *
                    hit.distance
                );


                Gizmos.DrawSphere(
                    hit.point,
                    0.08f
                );
            }
            else
            {
                Gizmos.DrawRay(
                    origin,
                    direction.normalized *
                    rayDistance
                );
            }
        }


        Gizmos.color = Color.cyan;

        Gizmos.DrawWireSphere(
            transform.position,
            roamRadius
        );


        if (hasLastKnownPlayerPosition)
        {
            Gizmos.color = Color.magenta;

            Gizmos.DrawWireSphere(
                lastKnownPlayerPosition,
                0.3f
            );

            Gizmos.DrawWireSphere(
                lastKnownPlayerPosition,
                investigationRadius
            );
        }


        if (hasInvestigationPoint)
        {
            Gizmos.color = Color.white;

            Gizmos.DrawSphere(
                investigationPoint,
                0.2f
            );
        }


        Vector3 attackOrigin =
            transform.position +
            Vector3.up * attackRayHeight;


        for (int i = 0; i < attackRayCount; i++)
        {
            float angle =
                -attackAngle / 2f +
                (attackAngle /
                (attackRayCount - 1)) * i;


            Vector3 direction =
                Quaternion.Euler(
                    0f,
                    angle,
                    0f
                ) * transform.forward;


            Gizmos.color = Color.blue;


            if (Physics.Raycast(
                attackOrigin,
                direction,
                out RaycastHit hit,
                attackDistance))
            {
                if (IsLayer(
                    hit.collider.gameObject,
                    playerLayer))
                {
                    Gizmos.color = Color.green;
                }
                else if (IsLayer(
                    hit.collider.gameObject,
                    obstacleLayer))
                {
                    Gizmos.color = Color.yellow;
                }


                Gizmos.DrawRay(
                    attackOrigin,
                    direction.normalized *
                    hit.distance
                );


                Gizmos.DrawSphere(
                    hit.point,
                    0.06f
                );
            }
            else
            {
                Gizmos.DrawRay(
                    attackOrigin,
                    direction.normalized *
                    attackDistance
                );
            }
        }


        Gizmos.color = Color.red;

        Gizmos.DrawWireSphere(
            transform.position,
            attackDistance
        );
    }
}