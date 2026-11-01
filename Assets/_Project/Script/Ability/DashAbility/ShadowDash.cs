using UnityEngine;

// Рывок ящера: короткий рывок, за героем остаётся след фиолетового пламени.
// Рекомендуемые значения в ассете: Range 2.2, Dash Time 0.16, Cooldown 0.7, Invulnerable Time 0.25.
[CreateAssetMenu(fileName = "ShadowDash", menuName = "Game/Abilities/Shadow Dash")]
public class ShadowDash : DashAbility
{
    [Header("След фиолетового пламени")]
    [Tooltip("Префаб одного язычка пламени (объект со SpriteRenderer и FlamePuff)")]
    public FlamePuff flamePrefab;
    [Tooltip("Как часто оставлять язычок, сек (меньше = гуще след)")]
    public float flameInterval = 0.025f;
    [Tooltip("Где у героя появляется пламя относительно его ног")]
    public Vector2 flameOffset = new Vector2(0f, 0.15f);
    [Tooltip("Сколько ещё секунд оставлять пламя после конца рывка")]
    public float flameAfterDash = 0.04f;

    protected override void OnDashStart(AbilityContext ctx, Vector2 dir, float time)
    {
        if (flamePrefab == null || ctx.owner == null) return;

        DashTrail trail = ctx.owner.GetComponent<DashTrail>();
        if (trail == null) trail = ctx.owner.gameObject.AddComponent<DashTrail>();   // добавится сам при первом рывке
        trail.Run(flamePrefab, flameInterval, time + flameAfterDash, flameOffset);
    }
}
