
using UnityEngine;

public class Enemy : Entity
{
    [Header("Настройки врага")]
    [SerializeField] protected float moveSpeed = 3f;
    [SerializeField] protected float attackRange = 2f;
    [SerializeField] protected int damage = 1;


    protected Transform player;

    protected override void Awake()
    {
        base.Awake();

        GameObject playerObject =
            GameObject.FindGameObjectWithTag("Player");

        if (playerObject != null)
        {
            player = playerObject.transform;
        }
    }

    protected virtual void FixedUpdate()
    {
        if (player == null)
        {
            StopMoving();
            return;
        }

        float distance = Vector2.Distance(
            transform.position,
            player.position
        );

        if (distance > attackRange)
        {
            ChasePlayer();
        }
        else
        {
            StopMoving();
        }
    }

    protected virtual void ChasePlayer()
    {
        Vector2 direction =
            ((Vector2)player.position -
             rb.position).normalized;

        rb.linearVelocity = direction * moveSpeed;
    }

    protected void StopMoving()
    {
        rb.linearVelocity = Vector2.zero;
    }
}
