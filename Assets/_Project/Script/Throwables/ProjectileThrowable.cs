using UnityEngine;

// МЕТАТЕЛЬНОЕ, КОТОРОЕ ЗАПУСКАЕТ СНАРЯДЫ (уровень 3: GameItemData -> ThrowableData -> ProjectileThrowable -> Kukri / Spear).
// Снаряды летят по прямой, пока не попадут во врага или в стену (или не выйдет время).
// Потомок решает: сколько снарядов и под какими углами (Angles), каким скриптом летит снаряд (CreateProjectile),
// и может дорисовать свой прицел (DrawAimLine).
public abstract class ProjectileThrowable : ThrowableData
{
    [Header("Снаряд")]
    [Tooltip("Картинка снаряда, остриём ВПРАВО")]
    public Sprite projectileSprite;
    [Tooltip("Скорость, клеток в секунду")]
    public float speed = 15f;
    [Tooltip("Через сколько секунд снаряд исчезнет, если ни во что не попал")]
    public float lifetime = 1.5f;
    [Tooltip("Толщина снаряда для попаданий, в клетках (радиус)")]
    public float radius = 0.15f;
    [Tooltip("В какие слои снаряд может попасть (обычно Everything)")]
    public LayerMask hitLayers = ~0;
    [Tooltip("Эффект при попадании (необязательно)")]
    public GameObject hitEffect;

    [Header("Вид прицела")]
    [Tooltip("Картинка, которой рисуется линия прицела (точка или черта, белая - красится в Aim Color)")]
    public Sprite aimLineSprite;
    [Tooltip("Картинка на конце линии (остриём вправо, белая)")]
    public Sprite aimTipSprite;
    [Tooltip("Расстояние между точками линии, в клетках")]
    public float aimSpacing = 0.35f;
    [Tooltip("Размер картинок прицела")]
    public float aimScale = 1f;
    [Tooltip("Линия начинается на таком расстоянии от героя")]
    public float aimStart = 0.5f;

    // ----- Решают потомки -----
    // Углы снарядов относительно направления на курсор, в градусах (0 = прямо)
    protected abstract float[] Angles { get; }
    // Добавить на объект снаряда нужный скрипт полёта
    protected abstract ThrownProjectile CreateProjectile(GameObject go);

    // ----- Бросок -----
    public override void Throw(ThrowContext ctx)
    {
        foreach (float angle in Angles)
        {
            Vector2 dir = Rotate(ctx.direction, angle);
            var go = new GameObject(name + "_Projectile");
            go.transform.SetParent(ctx.parent, false);
            go.transform.position = ctx.origin;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = projectileSprite;
            sr.sortingLayerID = ctx.sortingLayerID;
            sr.sortingOrder = ctx.sortingOrder;

            ThrownProjectile p = CreateProjectile(go);
            p.Launch(this, ctx, dir);
        }
    }

    // ----- Прицел: линия на каждый снаряд, до первой стены или врага -----
    public override void DrawAim(ThrowAim aim, Vector2 origin, Vector2 direction, GameObject owner)
    {
        foreach (float angle in Angles)
        {
            Vector2 dir = Rotate(direction, angle);
            bool blocked = ThrownProjectile.CastFirst(origin, radius, dir, aimLength, hitLayers, owner,
                                                      out RaycastHit2D hit, out _);
            float length = blocked ? hit.distance : aimLength;
            DrawAimLine(aim, origin, dir, length, blocked);
        }
    }

    // Одна линия прицела. Потомок может дорисовать своё (например, отметку попадания)
    protected virtual void DrawAimLine(ThrowAim aim, Vector2 origin, Vector2 dir, float length, bool blocked)
    {
        float from = Mathf.Min(aimStart, length);
        aim.DrawLine(aimLineSprite, origin + dir * from, dir, length - from, aimSpacing, aimColor, aimScale);
        if (aimTipSprite != null)
            aim.Draw(aimTipSprite, origin + dir * length, dir, aimColor, aimScale);
    }

    public static Vector2 Rotate(Vector2 v, float degrees)
    {
        float a = degrees * Mathf.Deg2Rad;
        float c = Mathf.Cos(a), s = Mathf.Sin(a);
        return new Vector2(v.x * c - v.y * s, v.x * s + v.y * c);
    }
}
