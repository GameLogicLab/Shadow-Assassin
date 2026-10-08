using UnityEngine;

public class PlayerAttack : MonoBehaviour
{
    [Header("Attack")]
    public float attackRadius = 2.5f;
    public float attackDamage = 100f;
    public float attackDelay = 0.3f;
    public float attackCooldown = 0.8f;

    [Header("Animation")]
    public Animator animator;

    [Header("Layers")]
    public LayerMask enemyLayer;

    private PlayerMovement playerMovement;

    private bool isAttacking = false;

    private float attackDelayTimer = 0f;
    private float attackCooldownTimer = 0f;

    private EnemyAI targetEnemy;

    void Start()
    {
        playerMovement = GetComponent<PlayerMovement>();
    }

    void Update()
    {
        if (attackCooldownTimer > 0f)
        {
            attackCooldownTimer -= Time.deltaTime;
        }


        if (isAttacking)
        {
            HandleAttack();
            return;
        }


        // Press E to attack
        if (Input.GetKeyDown(KeyCode.E))
        {
            TryAttack();
        }
    }

    public void TryAttack()
    {
        // Still on cooldown
        if (attackCooldownTimer > 0f)
        {
            return;
        }

        EnemyAI enemy = FindClosestEnemy();


        // No enemy found
        if (enemy == null)
        {
            Debug.Log("No enemy inside attack radius.");
            return;
        }


        targetEnemy = enemy;


        isAttacking = true;

        attackDelayTimer = attackDelay;


        if (playerMovement != null)
        {
            playerMovement.enabled = false;
        }


        FaceEnemy();


        // Play attack animation
        if (animator != null)
        {
            animator.SetTrigger("Attack");
        }


        Debug.Log(
            "Player started attacking: " +
            targetEnemy.name
        );
    }

    void HandleAttack()
    {
        // If enemy still exists,
        // keep facing the enemy
        if (targetEnemy != null)
        {
            FaceEnemy();
        }


        // Reduce attack delay
        attackDelayTimer -= Time.deltaTime;


        // Attack time reached
        if (attackDelayTimer <= 0f)
        {
            DealDamage();

            isAttacking = false;

            attackCooldownTimer = attackCooldown;

            targetEnemy = null;

            if (playerMovement != null)
            {
                playerMovement.enabled = true;
            }
        }
    }

    EnemyAI FindClosestEnemy()
    {
        Debug.Log("Find closedt enemy");
        // Find all colliders inside attack radius
        Collider[] enemies = Physics.OverlapSphere(
            transform.position,
            attackRadius,
            enemyLayer
        );


        EnemyAI closestEnemy = null;

        float closestDistance = Mathf.Infinity;


        foreach (Collider collider in enemies)
        {
            EnemyAI enemy =
                collider.GetComponentInParent<EnemyAI>();


            if (enemy == null)
            {
                continue;
            }


            float distance = Vector3.Distance(
                transform.position,
                enemy.transform.position
            );


            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestEnemy = enemy;
            }
        }


        return closestEnemy;
    }

    void DealDamage()
    {
        // No target
        if (targetEnemy == null)
        {
            return;
        }


        // Check distance again
        float distance = Vector3.Distance(
            transform.position,
            targetEnemy.transform.position
        );


        // Enemy moved outside attack radius
        if (distance > attackRadius)
        {
            Debug.Log("Attack missed - enemy moved too far away.");
            return;
        }


        targetEnemy.TakeDamage(attackDamage);


        Debug.Log(
            "Player dealt " +
            attackDamage +
            " damage to " +
            targetEnemy.name
        );
    }

    void FaceEnemy()
    {
        if (targetEnemy == null)
        {
            return;
        }


        Vector3 direction =
            targetEnemy.transform.position -
            transform.position;


        direction.y = 0f;


        if (direction == Vector3.zero)
        {
            return;
        }


        Quaternion targetRotation =
            Quaternion.LookRotation(direction);


        transform.rotation =
            Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                15f * Time.deltaTime
            );
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;


        Gizmos.DrawWireSphere(
            transform.position,
            attackRadius
        );
    }
}