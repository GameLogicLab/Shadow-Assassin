using UnityEngine;
using UnityEngine.UI;

public class PlayerHealth : MonoBehaviour
{
    [SerializeField] private float maxHealth = 100f;

    private float currentHealth;

    [Header("Health UI")]
    [SerializeField] private Slider healthBar;

    void Start()
    {
        currentHealth = maxHealth;

        if (healthBar != null)
        {
            healthBar.maxValue = maxHealth;
            healthBar.value = currentHealth;
        }
    }

    public void TakeDmg(float damage)
    {
        currentHealth -= damage;

        currentHealth = Mathf.Max(currentHealth, 0f);

        UpdateHealthBar();

        Debug.Log("Player Health: " + currentHealth);

        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    void UpdateHealthBar()
    {
        if (healthBar != null)
        {
            healthBar.value = currentHealth;
        }
    }

    void Die()
    {
        Debug.Log("Player Died!");
    }

    public float GetHealth()
    {
        return currentHealth;
    }
}