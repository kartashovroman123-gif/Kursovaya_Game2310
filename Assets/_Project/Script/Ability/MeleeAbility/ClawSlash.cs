using UnityEngine;

// Слабая атака когтями (ящер): быстрый удар по широкой дуге в один из 4 углов.
// Вся механика удара живёт в MeleeAbility, здесь только то, чем отличаются когти:
// последний удар серии бьёт сильнее.
[CreateAssetMenu(fileName = "ClawSlash", menuName = "Game/Abilities/Claw Slash")]
public class ClawSlash : MeleeAbility
{
    [Header("Особенность когтей")]
    [Tooltip("Во сколько раз больше урона у последнего удара серии (1 = без бонуса)")]
    public float finisherDamageMultiplier = 1.5f;

    protected override float ComputeDamage(AbilityContext ctx)
    {
        return ctx.comboFinisher ? ctx.damage * finisherDamageMultiplier : ctx.damage;
    }
}
