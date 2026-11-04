using UnityEngine;
using System.Collections;

public class PlayerAttack : MonoBehaviour
{
    public enum WeaponType
    {
        Knife = 0,
        Kunai = 1
    }

    [Header("Weapon Settings")]
    public WeaponType defaultWeapon = WeaponType.Knife;

    [Header("Knife Settings")]
    public float attackRadius = 2.5f;
    public float knifeDamage = 100f;
    public float attackDelay = 0.3f;

    [Header("Kunai Aiming")]
    public float kunaiAimRadius = 8f;

    [Header("Kunai Settings")]
    public GameObject kunaiProjectilePrefab;
    public Transform kunaiSpawnPoint;
    public float kunaiSpeed = 12f;
    public float kunaiDamage = 50f;
    public float kunaiLifetime = 3f;

    [Header("Attack Settings")]
    public float attackCooldown = 0.8f;
    public LayerMask enemyLayer;

    [Header("Animation")]
    public Animator animator;

    [Header("Facing Enemy")]
    public float rotationSpeed = 15f;

    private WeaponType selectedWeapon;
    private float nextAttackTime;
    private bool isAttacking;
    private PlayerMovement playerMovement;
    private PlayerWeaponVisuals weaponVisuals;

    private static readonly int AttackTriggerHash = Animator.StringToHash("Attack");
    private static readonly int KunaiThrowTriggerHash = Animator.StringToHash("KunaiThrow");

    private readonly Collider[] hitBuffer = new Collider[16];

    public WeaponType CurrentWeapon => selectedWeapon;

    private void Start()
    {
        playerMovement = GetComponent<PlayerMovement>();
        weaponVisuals = GetComponent<PlayerWeaponVisuals>();

        selectedWeapon = (WeaponType)PlayerPrefs.GetInt(
            "SelectedWeapon",
            (int)defaultWeapon
        );

        if (animator == null)
            animator = GetComponentInChildren<Animator>();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            TryAttack();
        }

        if (Input.GetKeyDown(KeyCode.Q))
        {
            SwitchWeapon();
        }
    }

    public void SwitchWeapon()
    {
        selectedWeapon = (selectedWeapon == WeaponType.Knife) ? WeaponType.Kunai : WeaponType.Knife;
        PlayerPrefs.SetInt("SelectedWeapon", (int)selectedWeapon);
        PlayerPrefs.Save();

        if (weaponVisuals != null)
        {
            weaponVisuals.RefreshVisuals((int)selectedWeapon);
        }
    }

    public void SetWeapon(WeaponType weapon)
    {
        selectedWeapon = weapon;
        PlayerPrefs.SetInt("SelectedWeapon", (int)selectedWeapon);
        PlayerPrefs.Save();

        if (weaponVisuals != null)
        {
            weaponVisuals.RefreshVisuals((int)selectedWeapon);
        }
    }

    public void TryAttack()
    {
        if (isAttacking || Time.time < nextAttackTime)
            return;

        if (animator == null)
        {
            Debug.LogError("PlayerAttack: Animator is not assigned.");
            return;
        }

        nextAttackTime = Time.time + attackCooldown;
        isAttacking = true;

        if (playerMovement != null)
            playerMovement.enabled = false;

        float currentRadius = selectedWeapon == WeaponType.Kunai ? kunaiAimRadius : attackRadius;
        EnemyAI target = FindClosestEnemy(currentRadius);

        if (target != null)
            FaceTarget(target.transform);

        if (selectedWeapon == WeaponType.Knife)
        {
            animator.SetTrigger(AttackTriggerHash);
            StartCoroutine(KnifeAttackRoutine());
        }
        else
        {
            animator.SetTrigger(KunaiThrowTriggerHash);
            StartCoroutine(AttackLockRoutine());
        }
    }

    private IEnumerator KnifeAttackRoutine()
    {
        yield return new WaitForSeconds(attackDelay);

        EnemyAI closestEnemy = FindClosestEnemy(attackRadius);

        if (closestEnemy != null)
            closestEnemy.TakeDamage(knifeDamage, false);

        yield return new WaitForSeconds(
            Mathf.Max(0f, attackCooldown - attackDelay)
        );

        FinishAttack();
    }

    private IEnumerator AttackLockRoutine()
    {
        yield return new WaitForSeconds(attackCooldown);
        FinishAttack();
    }

    private void FinishAttack()
    {
        isAttacking = false;

        if (playerMovement != null)
            playerMovement.enabled = true;
    }

    // Called by animation event
    public void ThrowKunai()
    {
        if (kunaiProjectilePrefab == null || kunaiSpawnPoint == null)
        {
            Debug.LogError("Assign the kunai prefab and spawn point in PlayerAttack.");
            return;
        }

        // Find the closest alive enemy within the kunai aiming radius.
        EnemyAI target = FindClosestEnemy(kunaiAimRadius);

        Vector3 throwDirection;

        if (target != null)
        {
            // Aim toward the enemy, including their height.
            throwDirection = target.transform.position - kunaiSpawnPoint.position;
            FaceTarget(target.transform);
        }
        else
        {
            throwDirection = transform.forward;
        }

        if (throwDirection.sqrMagnitude < 0.001f)
            throwDirection = transform.forward;

        Quaternion throwRotation = Quaternion.LookRotation(throwDirection.normalized);

        GameObject projectile = Instantiate(
            kunaiProjectilePrefab,
            kunaiSpawnPoint.position,
            throwRotation
        );

        KunaiProjectile kunai = projectile.GetComponent<KunaiProjectile>();

        if (kunai != null)
        {
            kunai.Launch(
                kunaiSpeed,
                kunaiDamage,
                enemyLayer,
                kunaiLifetime
            );
        }
        else
        {
            Debug.LogError("Kunai prefab is missing KunaiProjectile.cs.");
            Destroy(projectile);
        }
    }

    private void FaceTarget(Transform target)
    {
        Vector3 direction = target.position - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.001f)
            return;

        transform.rotation = Quaternion.LookRotation(direction);
    }

    private EnemyAI FindClosestEnemy(float searchRadius)
    {
        int hitCount = Physics.OverlapSphereNonAlloc(
            transform.position,
            searchRadius,
            hitBuffer,
            enemyLayer
        );

        EnemyAI closestEnemy = null;
        float closestDistSqr = Mathf.Infinity;
        Vector3 playerPos = transform.position;

        for (int i = 0; i < hitCount; i++)
        {
            Collider hit = hitBuffer[i];
            if (hit == null) continue;

            EnemyAI enemy = hit.GetComponentInParent<EnemyAI>();
            if (enemy == null || enemy.IsDead)
                continue;

            Vector3 diff = enemy.transform.position - playerPos;
            float distSqr = diff.sqrMagnitude;

            if (distSqr < closestDistSqr)
            {
                closestDistSqr = distSqr;
                closestEnemy = enemy;
            }
        }

        return closestEnemy;
    }

    private void OnDrawGizmosSelected()
    {
        // Knife attack radius.
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRadius);

        // Kunai targeting radius.
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, kunaiAimRadius);
    }
}
