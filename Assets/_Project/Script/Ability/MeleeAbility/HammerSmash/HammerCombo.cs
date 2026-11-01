using UnityEngine;

// Настройки одной ступени удара молотом
[System.Serializable]
public class HammerStage
{
    [Header("Зона (вокруг героя; у 3-й ступени - вокруг точки приземления)")]
    [Tooltip("Радиус удара, в клетках")]
    public float radius = 1.6f;
    [Tooltip("Ширина зоны в градусах: 120 = сектор перед героем, 180 = полукруг, 360 = полный круг")]
    [Range(10f, 360f)] public float arcAngle = 120f;
    [Tooltip("Ширина края, по которому проходит стальная часть молота, в клетках")]
    public float edgeWidth = 0.5f;

    [Header("Урон")]
    [Tooltip("Урон краем (сталь молота)")]
    public float edgeDamage = 30f;
    [Tooltip("Урон рукоятью (внутренняя часть зоны)")]
    public float handleDamage = 12f;
    [Tooltip("Сила отбрасывания от края (рукоять не отбрасывает)")]
    public float knockback = 7f;

    [Header("Время")]
    [Tooltip("Замах: от нажатия до удара, сек")]
    public float windup = 0.18f;
    [Tooltip("Восстановление после удара, сек")]
    public float recovery = 0.25f;

    [Header("Ощущение")]
    [Tooltip("Шаг вперёд при ударе, в клетках (для 3-й ступени не используется - там прыжок)")]
    public float lungeDistance = 0.4f;
    [Tooltip("Толчок камеры при ударе, в клетках")]
    public float cameraKick = 0.15f;
    [Tooltip("Стоп-кадр при попадании, сек")]
    public float hitStop = 0.07f;

    [Header("Перезарядка и переход к следующей ступени")]
    [Tooltip("Перезарядка молота после этой ступени, сек")]
    public float cooldown = 5f;
    [Tooltip("Сколько взмахов когтями нужно, чтобы открыть следующую ступень (у 3-й не используется)")]
    public int clawHitsToNext = 3;
    [Tooltip("За сколько секунд после удара их нужно сделать (у 3-й не используется)")]
    public float window = 5f;
}

// Тяжёлая атака молотом из трёх ступеней (как Q у Атрокса).
// Ступень 1: короткий взмах перед героем. 3 взмаха когтями за 5 с -> ступень 2.
// Ступень 2: взмах полукругом. 5 взмахов когтями за 8 с -> ступень 3.
// Ступень 3: прыжок в точку и удар по кругу с огнём. Потом перезарядка 8 с и снова ступень 1.
// У каждой ступени два кольца: край (сталь, сильный урон + отбрасывание) и рукоять (слабый урон).
// Сам ассет только описывает удар; счётчики и прицел живут на герое в PlayerHeavyAttack.
[CreateAssetMenu(fileName = "HammerCombo", menuName = "Game/Abilities/Hammer Combo")]
public class HammerCombo : MeleeAbility
{
    [Header("Ступени (1 - взмах, 2 - полукруг, 3 - прыжок)")]
    public HammerStage stage1 = new HammerStage
    {
        radius = 1.6f, arcAngle = 110f, edgeWidth = 0.5f, edgeDamage = 30f, handleDamage = 12f, knockback = 7f,
        windup = 0.18f, recovery = 0.25f, lungeDistance = 0.4f, cameraKick = 0.15f, hitStop = 0.07f,
        cooldown = 5f, clawHitsToNext = 3, window = 5f
    };
    public HammerStage stage2 = new HammerStage
    {
        radius = 1.9f, arcAngle = 180f, edgeWidth = 0.55f, edgeDamage = 40f, handleDamage = 15f, knockback = 8f,
        windup = 0.22f, recovery = 0.3f, lungeDistance = 0.5f, cameraKick = 0.2f, hitStop = 0.08f,
        cooldown = 8f, clawHitsToNext = 5, window = 8f
    };
    public HammerStage stage3 = new HammerStage
    {
        radius = 1.8f, arcAngle = 360f, edgeWidth = 0.6f, edgeDamage = 60f, handleDamage = 25f, knockback = 10f,
        windup = 0.12f, recovery = 0.35f, lungeDistance = 0f, cameraKick = 0.3f, hitStop = 0.12f,
        cooldown = 8f, clawHitsToNext = 0, window = 0f
    };

    [Tooltip("Сколько секунд открытая ступень 2 или 3 ждёт удара, потом всё сбрасывается на ступень 1")]
    public float unlockedTimeout = 5f;

    [Header("Ступень 3: прыжок")]
    [Tooltip("Максимальная дальность прыжка, в клетках. Дальше курсор не уведёт")]
    public float maxLeapDistance = 3.5f;
    [Tooltip("Сколько длится полёт, сек")]
    public float leapTime = 0.3f;
    [Tooltip("Сколько неуязвимости добавить после приземления, сек")]
    public float landingInvulnerability = 0.1f;

    [Header("Ступень 3: огонь")]
    [Tooltip("Префаб язычка огня (как VioletFlame, но с кадрами Fire_Flame)")]
    public FlamePuff firePrefab;
    [Tooltip("Сколько язычков огня при приземлении")]
    public int fireCount = 16;

    [Header("Цвета зоны")]
    public Color aimFill = new Color(0.55f, 0.25f, 0.9f, 0.3f);
    public Color aimEdge = new Color(0.7f, 0.35f, 1f, 0.45f);
    public Color aimOutline = new Color(0.85f, 0.6f, 1f, 0.95f);
    [Tooltip("Насколько бледнее зона, пока молот перезаряжается")]
    [Range(0f, 1f)] public float cooldownAlpha = 0.35f;
    public Color flashFill = new Color(0.85f, 0.6f, 1f, 0.35f);
    public Color flashEdge = new Color(1f, 0.85f, 1f, 0.7f);
    [Tooltip("Сколько секунд гаснет вспышка зоны после удара")]
    public float flashTime = 0.25f;
    [Tooltip("Order in Layer зоны (пол = 0, герой = 5)")]
    public int zoneSortingOrder = 1;

    // 0, 1, 2 -> ступень 1, 2, 3
    public HammerStage GetStage(int index)
    {
        return index <= 0 ? stage1 : index == 1 ? stage2 : stage3;
    }

    // ctx.comboStep = номер ступени (0..2), ctx.origin = центр зоны, ctx.direction = свободное направление удара
    HammerStage Current(AbilityContext ctx) => GetStage(ctx.comboStep);

    // ---------- Переопределения MeleeAbility ----------

    protected override bool IsInZone(AbilityContext ctx, Vector2 toTarget)
    {
        HammerStage st = Current(ctx);
        if (st.arcAngle >= 359f) return true;
        return Vector2.Angle(ctx.direction, toTarget) <= st.arcAngle * 0.5f;
    }

    // Край, если хоть часть цели дотягивается до стального кольца
    public bool IsEdge(AbilityContext ctx, Collider2D target)
    {
        HammerStage st = Current(ctx);
        Bounds b = target.bounds;
        float far = Vector2.Distance(ctx.origin, b.center) + Mathf.Min(b.extents.x, b.extents.y);
        return far >= st.radius - st.edgeWidth;
    }

    protected override float DamageFor(AbilityContext ctx, Collider2D target, Vector2 toTarget)
    {
        HammerStage st = Current(ctx);
        return IsEdge(ctx, target) ? st.edgeDamage : st.handleDamage;
    }

    // Край отбрасывает от центра удара
    protected override void OnTargetHit(GameObject target, AbilityContext ctx, Collider2D col)
    {
        if (!IsEdge(ctx, col)) return;
        Rigidbody2D body = target.GetComponent<Rigidbody2D>();
        if (body == null || body.bodyType != RigidbodyType2D.Dynamic) return;

        Vector2 away = (Vector2)target.transform.position - ctx.origin;
        if (away.sqrMagnitude < 0.0001f) away = ctx.direction;
        body.AddForce(away.normalized * Current(ctx).knockback, ForceMode2D.Impulse);
    }

    protected override void OnHitFeel(AbilityContext ctx)
    {
        HitStop.Do(Current(ctx).hitStop);
    }

    // Вспышка зоны удара (и огонь у 3-й ступени)
    protected override void SpawnEffect(AbilityContext ctx)
    {
        HammerStage st = Current(ctx);
        HammerZone flash = HammerZone.Create(zoneSortingOrder);
        flash.Show(ctx.origin, ctx.direction, st.radius, st.arcAngle, st.edgeWidth, flashFill, flashEdge, aimOutline);
        flash.FadeAndDestroy(flashTime);

        if (ctx.comboStep >= 2 && firePrefab != null)
        {
            for (int i = 0; i < fireCount; i++)
            {
                // половина огня - по краю круга, половина - внутри
                float r = (i % 2 == 0) ? st.radius * Random.Range(0.8f, 1f) : st.radius * Mathf.Sqrt(Random.value) * 0.8f;
                float a = Random.Range(0f, Mathf.PI * 2f);
                Vector2 p = ctx.origin + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r;
                Object.Instantiate(firePrefab, p, Quaternion.identity);
            }
        }
    }

    protected override void DrawDebugZone(AbilityContext ctx)
    {
        HammerStage st = Current(ctx);
        int steps = 24;
        float half = st.arcAngle >= 359f ? 180f : st.arcAngle * 0.5f;
        Vector3 o = ctx.origin;
        Vector3 prevOuter = Vector3.zero, prevInner = Vector3.zero;
        for (int i = 0; i <= steps; i++)
        {
            float ang = Mathf.Lerp(-half, half, i / (float)steps);
            Vector3 d = Quaternion.Euler(0f, 0f, ang) * (Vector3)ctx.direction;
            Vector3 outer = o + d * st.radius, inner = o + d * (st.radius - st.edgeWidth);
            if (i > 0)
            {
                Debug.DrawLine(prevOuter, outer, Color.red, 0.3f);
                Debug.DrawLine(prevInner, inner, Color.yellow, 0.3f);
            }
            prevOuter = outer; prevInner = inner;
        }
    }
}
