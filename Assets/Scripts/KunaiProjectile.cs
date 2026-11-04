
using UnityEngine;

public class KunaiProjectile : MonoBehaviour
{
    private float speed;
    private float damage;
    private float lifetime;
    private LayerMask enemyLayer;
    private bool hasHit;

    public void Launch(float projectileSpeed, float projectileDamage,
        LayerMask targetLayer, float projectileLifetime)
    {
        speed = projectileSpeed;
        damage = projectileDamage;
        enemyLayer = targetLayer;
        lifetime = projectileLifetime;

        Destroy(gameObject, lifetime);
    }

    private void Update()
    {
        transform.position += transform.forward * speed * Time.deltaTime;
    }

private void OnTriggerEnter(Collider other)
{

    if (hasHit)
        return;

    if (other.name == "Bounds")
        return;

    hasHit = true;

    bool isEnemy = (enemyLayer.value & (1 << other.gameObject.layer)) != 0;

    if (isEnemy)
    {
        EnemyAI enemy = other.GetComponentInParent<EnemyAI>();

        if (enemy != null)
        {
            enemy.TakeDamage(damage, true);
            Debug.Log("Kunai damaged: " + enemy.name);
        }
    }

    Destroy(gameObject);
}
}
