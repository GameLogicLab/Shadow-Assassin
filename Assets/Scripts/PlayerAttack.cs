using UnityEngine;
using System.Collections;   

public class PlayerAttack : MonoBehaviour
{
    [Header("Attack")]
    public float attackRadius = 2.5f;
    public float attackDamage = 100f;
    public float attackDelay = 1f;
    public float attackCooldown = 0.8f;
    public float StopMovementTime = 0f;

    [Header("Animation")]
    public Animator animator;

    [Header("Layers")]
    public LayerMask enemyLayer;

    private PlayerMovement playerMovement;

    private bool isAttacking = false;

    private float attackDelayTimer = 0f;
    private float attackCooldownTimer = 0f;

    private EnemyAI targetEnemy;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip attackSound;

    void Start()
    {
        playerMovement = GetComponent<PlayerMovement>();

        // Automatically find Animator if it was not assigned
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }
    }

    void Update()
    {
        // Reduce attack cooldown
        if (attackCooldownTimer > 0f)
        {
            attackCooldownTimer -= Time.deltaTime;
        }

        // Continue the current attack
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

    void LateUpdate()
    {
        // Reapply rotation after normal frame updates,
        // helping prevent the Animator from overriding it.
        if (isAttacking && targetEnemy != null)
        {
            FaceEnemy();
        }
    }

    public void TryAttack()
    {
        // Check cooldown
        if (attackCooldownTimer > 0f)
        {
            return;
        }

        // Find the closest enemy inside attack radius
        EnemyAI enemy = FindClosestEnemy();

        if (enemy == null)
        {
            Debug.Log("No enemy inside attack radius.");
            return;
        }

        // Set the attack target
        targetEnemy = enemy;
        isAttacking = true;
        attackDelayTimer = attackDelay;

        // Face the enemy immediately
        FaceEnemy();

        // Play attack animation
        if (animator != null)
        {
            animator.SetTrigger("Attack");
            PlayAttackSound();
        }

        Debug.Log("Player started attacking: " + targetEnemy.name);
    }

    void HandleAttack()
    {
        // Keep facing the enemy while attacking
        if (targetEnemy != null)
        {
            FaceEnemy();
        }

        DealDamage();

        // Reduce attack delay
        attackDelayTimer -= Time.deltaTime;

        // Attack when the delay finishes
        if (attackDelayTimer <= 0f)
        {
            DealDamage();

            // End the attack
            isAttacking = false;
            attackCooldownTimer = attackCooldown;
            targetEnemy = null;
        }
        
        StartCoroutine(StopMovement());
    }

    IEnumerator StopMovement()
    {
        // Allow player movement again
        if (playerMovement != null)
        {
            playerMovement.enabled = false;
        }

        yield return new WaitForSeconds(StopMovementTime);

        // Allow player movement again
        if (playerMovement != null)
        {
            playerMovement.enabled = true;
        }

    }

    EnemyAI FindClosestEnemy()
    {
        // Find colliders inside attack radius on the enemy layer
        Collider[] enemies = Physics.OverlapSphere(
            transform.position,
            attackRadius,
            enemyLayer
        );

        EnemyAI closestEnemy = null;
        float closestDistance = Mathf.Infinity;

        foreach (Collider enemyCollider in enemies)
        {
            // EnemyAI may be on the collider's parent
            EnemyAI enemy =
                enemyCollider.GetComponentInParent<EnemyAI>();

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

    public void PlayAttackSound()
    {
        if (audioSource != null && attackSound != null)
        {
            audioSource.PlayOneShot(attackSound);
        }
    }

    void DealDamage()
    {
        // Check whether the target still exists
        if (targetEnemy == null)
        {
            Debug.Log("Attack cancelled: target enemy no longer exists.");
            return;
        }

        // Check the distance again before applying damage
        float distance = Vector3.Distance(
            transform.position,
            targetEnemy.transform.position
        );

        if (distance > attackRadius)
        {
            Debug.Log("Attack missed - enemy moved too far away.");
            return;
        }

        // Deal damage to the enemy
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

        // Calculate direction from player to enemy
        Vector3 direction =
            targetEnemy.transform.position - transform.position;

        // Ignore height differences
        direction.y = 0f;

        // Avoid invalid rotation when positions overlap
        if (direction.sqrMagnitude < 0.001f)
        {
            return;
        }

        // Calculate the rotation facing the enemy
        Quaternion targetRotation = Quaternion.LookRotation(direction);

        // Rotate immediately toward the enemy
        transform.rotation = targetRotation;
    }

    void OnDrawGizmosSelected()
    {
        // Display attack radius in the Scene view
        Gizmos.color = Color.red;

        Gizmos.DrawWireSphere(
            transform.position,
            attackRadius
        );
    }
}