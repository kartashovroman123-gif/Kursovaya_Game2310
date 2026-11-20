
using UnityEngine;

public class Entity : MonoBehaviour
{
    [Header("Характеристики сущности")]
    [SerializeField] protected int maxHealth = 100;

    protected int currentHealth;
    protected Rigidbody2D rb;

    protected virtual void Awake()
    {
        currentHealth = maxHealth;
        rb = GetComponent<Rigidbody2D>();
    }

    public virtual void TakeDamage(int damage)
    {
        currentHealth -= damage;

        Debug.Log(
            gameObject.name + " получил урон: " + damage
        );

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    protected virtual void Die()
    {
        Destroy(gameObject);
    }
}
