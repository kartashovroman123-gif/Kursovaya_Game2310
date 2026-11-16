using UnityEngine;

// Всё, что может получать урон от приёмов и снарядов героя (враги, разрушаемые бочки...).
// ДЛЯ ДРУЗЕЙ: добавьте к скрипту здоровья моба  ": MonoBehaviour, IDamageable"  и метод TakeDamage.
// Снаряды (и позже удары) сами найдут этот скрипт на враге или его родителе.
public interface IDamageable
{
    // amount - урон, direction - откуда прилетело (для отбрасывания и эффектов), source - кто нанёс
    void TakeDamage(float amount, Vector2 direction, GameObject source);
}
