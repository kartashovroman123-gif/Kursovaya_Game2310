using UnityEngine;

public class PlayerStats : MonoBehaviour
{
    public CharacterData data;
    public int maxHealth;
    public int currentHealth;
    public float moveSpeed;
    public int damage;

    public void Init(CharacterData d)
    {
        data = d;
        maxHealth = d.maxHealth;
        currentHealth = d.maxHealth;
        moveSpeed = d.moveSpeed;
        damage = d.damage;
    }
}