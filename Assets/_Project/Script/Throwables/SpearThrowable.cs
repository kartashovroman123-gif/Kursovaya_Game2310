using UnityEngine;

// МЕТАТЕЛЬНОЕ КОПЬЁ (нижний уровень: GameItemData -> ThrowableData -> ProjectileThrowable -> SpearThrowable).
// Одно тяжёлое копьё строго по прямой: больше урона, быстрее, отбрасывает врага, короткий стоп-кадр при попадании.
// В стену втыкается и через мгновение исчезает.
// Прицел - длинная штриховая линия, наконечник и отметка в точке, где копьё во что-то упрётся.
[CreateAssetMenu(fileName = "Spear", menuName = "Game/Throwables/Spear")]
public class SpearThrowable : ProjectileThrowable
{
    [Header("Копьё")]
    [Tooltip("Сила отбрасывания врага (если у врага Dynamic Rigidbody2D)")]
    public float knockback = 6f;
    [Tooltip("Стоп-кадр при попадании во врага, сек (0 = без него)")]
    public float hitStop = 0.05f;
    [Tooltip("Сколько секунд копьё торчит в стене")]
    public float stuckTime = 0.4f;
    [Tooltip("Отметка в точке, где копьё упрётся (белая, необязательно)")]
    public Sprite aimImpactSprite;

    static readonly float[] straight = { 0f };
    protected override float[] Angles => straight;

    protected override ThrownProjectile CreateProjectile(GameObject go) => go.AddComponent<SpearProjectile>();

    protected override void DrawAimLine(ThrowAim aim, Vector2 origin, Vector2 dir, float length, bool blocked)
    {
        base.DrawAimLine(aim, origin, dir, length, blocked);
        if (blocked && aimImpactSprite != null)
            aim.Draw(aimImpactSprite, origin + dir * length, dir, aimColor, aimScale);
    }

    // Значения по умолчанию для нового ассета (свои, не как у остальных метательных)
    public SpearThrowable()
    {
        damage = 25f;
        throwCooldown = 0.5f;
        speed = 20f;
        lifetime = 1.5f;
        radius = 0.15f;
        aimSpacing = 0.45f;
        aimColor = new Color(1f, 0.85f, 0.6f, 0.85f);
    }
}
