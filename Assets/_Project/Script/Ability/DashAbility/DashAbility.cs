using UnityEngine;

// Куда летит рывок
public enum DashDirection
{
    Movement,   // куда герой идёт (если стоит - куда смотрит), как в Cult of the Lamb
    Cursor      // к курсору мыши
}

// ОБЩИЙ ШАБЛОН ЛЮБОГО РЫВКА (средний уровень дерева: AbilityData -> DashAbility -> конкретный рывок).
// Одинаково у всех рывков: полёт на расстояние Range за Dash Time, неуязвимость, усиления от предметов.
// Потомки решают, что происходит при рывке (OnDashStart): след из пламени, огонь на старте и т.п.
public abstract class DashAbility : AbilityData
{
    [Header("Рывок (длина = Range, в клетках)")]
    [Tooltip("Сколько длится рывок, сек")]
    public float dashTime = 0.16f;
    public DashDirection direction = DashDirection.Movement;

    [Header("Неуязвимость")]
    [Tooltip("Сколько секунд герой неуязвим с начала рывка. Обычно чуть больше Dash Time")]
    public float invulnerableTime = 0.25f;

    public override void Execute(AbilityContext ctx)
    {
        PlayerAbilities player = ctx.owner != null ? ctx.owner.GetComponent<PlayerAbilities>() : null;

        Vector2 dir = direction == DashDirection.Cursor ? ctx.aim : ctx.moveDirection;
        if (dir.sqrMagnitude < 0.0001f) dir = ctx.direction;
        dir.Normalize();

        // усиления от предметов: длиннее рывок -> дольше летим (скорость та же) и дольше неуязвимы
        float mult = player != null ? Mathf.Max(0.1f, player.dashDistanceMultiplier) : 1f;
        float distance = ctx.range * mult;
        float time = dashTime * mult;
        float invulnerable = invulnerableTime + (time - dashTime)
                           + (player != null ? player.dashInvulnerabilityBonus : 0f);

        if (player != null)
        {
            player.Dash(dir, distance, time);
            player.GrantInvulnerability(invulnerable);
        }

        OnDashStart(ctx, dir, time);
        Debug.DrawLine(ctx.origin, ctx.origin + dir * distance, Color.cyan, 0.3f);
    }

    // Что происходит в начале рывка (каждый потомок решает сам). time - сколько продлится рывок
    protected abstract void OnDashStart(AbilityContext ctx, Vector2 dir, float time);
}
