
using System.Collections;
using UnityEngine;

public class Rat : Enemy
{
    [Header("Атака крысы")]
    [SerializeField] private float attackDelay = 1f;
    [SerializeField] private float dashSpeed = 8f;
    [SerializeField] private float dashDuration = 0.3f;
    [SerializeField] private float attackCooldown = 0.5f;

    private bool isAttacking = false;
    private bool canDealDamage = false;
    private bool hasHitPlayer = false;

    protected override void FixedUpdate()
    {
        if (player == null)
        {
            StopMoving();
            return;
        }

        if (isAttacking)
            return;

        float distance = Vector2.Distance(
            transform.position,
            player.position
        );

        if (distance <= attackRange)
        {
            StopMoving();
            StartCoroutine(JumpAttack());
        }
        else
        {
            base.FixedUpdate();
        }
    }

    private IEnumerator JumpAttack()
    {
        isAttacking = true;

        // Останавливаемся перед атакой
        StopMoving();

        // Ждём перед прыжком
        yield return new WaitForSeconds(attackDelay);

        if (player == null)
        {
            isAttacking = false;
            yield break;
        }

        // Запоминаем направление прыжка
        Vector2 direction =
            ((Vector2)player.position -
             rb.position).normalized;

        canDealDamage = true;
        hasHitPlayer = false;

        // Рывок
        rb.linearVelocity = direction * dashSpeed;

        yield return new WaitForSeconds(dashDuration);

        // Завершаем атаку
        StopMoving();
        canDealDamage = false;

        // Пауза после атаки
        yield return new WaitForSeconds(attackCooldown);

        isAttacking = false;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!canDealDamage || hasHitPlayer)
            return;

        if (other.CompareTag("Player"))
        {
            hasHitPlayer = true;

            Entity target = other.GetComponent<Entity>();

            if (target != null)
            {
                target.TakeDamage(damage);
            }

            Debug.Log("Крыса атаковала игрока!");
        }
    }
}
