
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

        if (((1 << other.gameObject.layer) & enemyLayer.value) != 0)
        {
            EnemyAI enemy = other.GetComponentInParent<EnemyAI>();

            if (enemy != null)
            {
                hasHit = true;
                enemy.TakeDamage(damage, true);
                Destroy(gameObject);
            }
        }
    }
}
