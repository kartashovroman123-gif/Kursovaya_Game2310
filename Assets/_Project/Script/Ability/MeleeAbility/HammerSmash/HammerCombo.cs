using UnityEngine;

// Форма зоны удара
public enum HammerShape
{
    Lane,         // дорожка: прямоугольник вперёд от героя (Length - длина, Width - ширина)
    HalfEllipse,  // дуга вперёд, как 2-я ступень у Атрокса (Length - вперёд, Width - вширь)
    Circle,       // полный круг (Length - радиус)
    Cone          // сектор («кусок пиццы»): из точки у героя расходится вперёд, конец - дуга
                  // (Length - длина, Width - ширина конца)
}

// Настройки одной ступени удара молотом
[System.Serializable]
public class HammerStage
{
    [Header("Зона")]
    public HammerShape shape = HammerShape.Lane;
    [Tooltip("Дорожка: длина вперёд. Дуга: насколько далеко вперёд. Круг: радиус. Конус: вся длина. В клетках")]
    public float length = 3.2f;
    [Tooltip("Дорожка: ширина. Дуга: полная ширина вбок. Конус: ширина конца. Круг: не используется. В клетках")]
    public float width = 1.3f;
    [Tooltip("Только конус: ширина у самого героя. 0 = острая точка, 1 = почти квадратное начало. В клетках")]
    public float baseWidth = 0f;
    [Tooltip("Какая часть пути - пустая у героя (0.35 = зона начинается на 35% длины). Для дорожки и круга обычно 0")]
    [Range(0f, 0.9f)] public float innerFraction = 0f;
    [Tooltip("Какая часть пути - опасный край с сильным пламенем (0.3 = последние 30%)")]
    [Range(0.05f, 0.9f)] public float edgeFraction = 0.3f;

    [Header("Урон и отбрасывание")]
    public float damage = 14f;
    [Tooltip("Урон фиолетовой частью волны")]
    public float edgeDamage = 32f;
    public float knockback = 3f;
    [Tooltip("Отбрасывание фиолетовой частью волны")]
    public float edgeKnockback = 8f;

    [Header("Цепочка вспышек")]
    [Tooltip("Сколько «шагов» вспышек от героя до конца зоны")]
    public int steps = 6;
    [Tooltip("За сколько секунд цепочка доходит от первого шага до последнего")]
    public float waveTime = 0.35f;

    [Header("Время")]
    [Tooltip("Замах: от нажатия до удара о землю, сек")]
    public float windup = 0.2f;
    [Tooltip("Восстановление после удара, сек")]
    public float recovery = 0.25f;

    [Header("Ощущение")]
    [Tooltip("Шаг вперёд при ударе, в клетках (у 3-й ступени не используется - там прыжок)")]
    public float lungeDistance = 0.3f;
    [Tooltip("Толчок камеры при ударе о землю, в клетках")]
    public float cameraKick = 0.15f;
    [Tooltip("Стоп-кадр при первом попадании волны, сек")]
    public float hitStop = 0.07f;

    [Header("Перезарядка и переход к следующей ступени")]
    [Tooltip("Перезарядка молота после этой ступени, сек")]
    public float cooldown = 5f;
    [Tooltip("Сколько взмахов когтями нужно, чтобы открыть следующую ступень (у 3-й не используется)")]
    public int clawHitsToNext = 3;
    [Tooltip("За сколько секунд после удара их нужно сделать (у 3-й не используется)")]
    public float window = 5f;
}

// Тяжёлая атака молотом из трёх ступеней (как Q у Атрокса). Урон наносит ЦЕПОЧКА ВСПЫШЕК:
// от героя наружу по очереди трескается земля и вырывается фиолетовое пламя. Враг получает удар,
// когда вспыхивает его место. На опасном краю пламя сильнее: больше урона и отбрасывания.
// Цепочка обрывается у стен.
// Ступень 1: длинная дорожка вперёд.            3 взмаха когтями за 5 с -> ступень 2.
// Ступень 2: сектор: от героя расходится вперёд, конец - широкая дуга. 5 взмахов за 8 с -> ступень 3.
// Ступень 3: прыжок в точку и полный круг. Потом перезарядка 8 с и снова ступень 1.
// Ассет только описывает удар; счётчики и прицел - в PlayerHeavyAttack, вспышки - в Shockwave.
[CreateAssetMenu(fileName = "HammerCombo", menuName = "Game/Abilities/Hammer Combo")]
public class HammerCombo : MeleeAbility
{
    [Header("Ступени (1 - дорожка, 2 - конус, 3 - прыжок)")]
    public HammerStage stage1 = new HammerStage
    {
        shape = HammerShape.Lane, length = 5f, width = 1.3f, innerFraction = 0f, edgeFraction = 0.3f,
        damage = 14f, edgeDamage = 32f, knockback = 3f, edgeKnockback = 8f, steps = 7, waveTime = 0.35f,
        windup = 0.2f, recovery = 0.25f, lungeDistance = 0.3f, cameraKick = 0.15f, hitStop = 0.07f,
        cooldown = 5f, clawHitsToNext = 3, window = 5f
    };
    public HammerStage stage2 = new HammerStage
    {
        shape = HammerShape.Cone, length = 4.8f, width = 4f, baseWidth = 1.2f, innerFraction = 0f, edgeFraction = 0.25f,
        damage = 16f, edgeDamage = 40f, knockback = 3.5f, edgeKnockback = 9f, steps = 6, waveTime = 0.38f,
        windup = 0.24f, recovery = 0.3f, lungeDistance = 0.35f, cameraKick = 0.2f, hitStop = 0.08f,
        cooldown = 8f, clawHitsToNext = 5, window = 8f
    };
    public HammerStage stage3 = new HammerStage
    {
        shape = HammerShape.Circle, length = 2.4f, width = 0f, innerFraction = 0f, edgeFraction = 0.25f,
        damage = 25f, edgeDamage = 60f, knockback = 5f, edgeKnockback = 11f, steps = 4, waveTime = 0.3f,
        windup = 0.12f, recovery = 0.35f, lungeDistance = 0f, cameraKick = 0.3f, hitStop = 0.12f,
        cooldown = 8f, clawHitsToNext = 0, window = 0f
    };

    [Tooltip("Сколько секунд открытая ступень 2 или 3 ждёт удара, потом всё сбрасывается на ступень 1")]
    public float unlockedTimeout = 5f;
    [Tooltip("Насколько выше ног начинается волна, в клетках")]
    public float groundOffset = 0.1f;

    [Header("Связка с рывком")]
    [Tooltip("Если ударить молотом во время рывка или вскоре после него - замах короче")]
    public float afterDashWindow = 0.35f;
    [Tooltip("Во сколько раз короче замах после рывка (0.5 = вдвое)")]
    [Range(0.1f, 1f)] public float afterDashWindupMultiplier = 0.5f;

    [Header("Ступень 3: прыжок")]
    [Tooltip("Максимальная дальность прыжка, в клетках. Дальше курсор не уведёт")]
    public float maxLeapDistance = 3.5f;
    [Tooltip("Сколько длится полёт, сек")]
    public float leapTime = 0.3f;
    [Tooltip("Сколько неуязвимости добавить после приземления, сек")]
    public float landingInvulnerability = 0.1f;

    [Header("Вспышки: картинка")]
    [Tooltip("Трещина в земле (FlamePuff, Frames = Crack_0..2, Random Single Frame)")]
    public FlamePuff crackPrefab;
    [Tooltip("Большая трещина на опасном краю (Crack_Edge_0..1)")]
    public FlamePuff edgeCrackPrefab;
    [Tooltip("Извержение фиолетового пламени (Eruption_0..5)")]
    public FlamePuff eruptionPrefab;
    [Tooltip("Сильное извержение на опасном краю (Eruption_Edge_0..6)")]
    public FlamePuff edgeEruptionPrefab;
    [Tooltip("Пыль у вспышек (необязательно, Dust_0..4)")]
    public FlamePuff dustPrefab;
    [Tooltip("Вспышка в месте удара молотом о землю (Impact_0..3, Rise Speed 0)")]
    public FlamePuff impactPrefab;
    [Tooltip("Расстояние между вспышками поперёк зоны, в клетках (меньше = гуще)")]
    public float burstSpacing = 1f;
    [Tooltip("Насколько случайно сдвигать вспышки, чтобы не стояли по линейке, в клетках")]
    public float burstJitter = 0.15f;
    [Tooltip("Общий размер всех вспышек")]
    public float burstScale = 1f;

    [Header("Ступень 3: огонь (необязательно)")]
    [Tooltip("Префаб язычка огня (Fire_Flame) - дополнительно к фиолетовым вспышкам")]
    public FlamePuff firePrefab;
    [Tooltip("Сколько язычков огня при приземлении")]
    public int fireCount = 16;

    [Header("Цвета зоны")]
    public Color aimFill = new Color(0.55f, 0.25f, 0.9f, 0.3f);
    public Color aimEdge = new Color(0.7f, 0.35f, 1f, 0.5f);
    public Color aimOutline = new Color(0.85f, 0.6f, 1f, 0.95f);
    [Tooltip("Насколько бледнее зона, пока молот перезаряжается")]
    [Range(0f, 1f)] public float cooldownAlpha = 0.35f;
    [Tooltip("Order in Layer зоны прицела (пол = 0, герой = 5)")]
    public int zoneSortingOrder = 1;

    // 0, 1, 2 -> ступень 1, 2, 3
    public HammerStage GetStage(int index)
    {
        return index <= 0 ? stage1 : index == 1 ? stage2 : stage3;
    }

    // ---------- Геометрия зоны (общая для прицела и волны) ----------

    // Попадает ли позиция пути u в зону (учитывая пустую часть у героя)
    public static bool InZone(HammerStage st, float u) => u >= st.innerFraction && u <= 1f;

    // Насколько далеко точка local (x - вперёд, y - вбок) по пути волны: 0 = у героя, 1 = конец зоны.
    // Точка вне формы -> больше 1 (или бесконечность, если позади героя).
    public static float PathPosition(HammerStage st, Vector2 local)
    {
        switch (st.shape)
        {
            case HammerShape.Lane:
                if (local.x < 0f || Mathf.Abs(local.y) > st.width * 0.5f) return float.PositiveInfinity;
                return local.x / Mathf.Max(0.01f, st.length);
            case HammerShape.HalfEllipse:
                if (local.x < 0f) return float.PositiveInfinity;
                float fx = local.x / Mathf.Max(0.01f, st.length);
                float fy = local.y / Mathf.Max(0.01f, st.width * 0.5f);
                return Mathf.Sqrt(fx * fx + fy * fy);
            case HammerShape.Cone:
                if (ConeDistance(st, local) < 0f) return float.PositiveInfinity;
                return local.magnitude / Mathf.Max(0.01f, st.length);
            default:
                return local.magnitude / Mathf.Max(0.01f, st.length);
        }
    }

    // ---------- Конус (ступень 2) ----------
    // Сектор, как кусок пиццы, но у героя начинается не острой точкой, а отрезком шириной Base Width.
    // Дальше две прямые расходятся до ширины Width на конце, конец - дуга круга радиусом Length.

    static float ConeBase(HammerStage st) => Mathf.Clamp(st.baseWidth, 0f, Mathf.Max(0f, st.width - 0.1f));

    // Угол наклона боковых прямых (радианы): насколько быстро расходится конус
    public static float ConeSideAngle(HammerStage st)
    {
        float L = Mathf.Max(0.05f, st.length);
        return Mathf.Atan(Mathf.Max(0f, (st.width - ConeBase(st)) * 0.5f / L));
    }

    // Насколько точка внутри конуса, в клетках: больше 0 - внутри, меньше 0 - снаружи (для контура прицела)
    public static float ConeDistance(HammerStage st, Vector2 local)
    {
        float a = ConeSideAngle(st);
        float b = ConeBase(st) * 0.5f;
        float sides = (b + local.x * Mathf.Tan(a) - Mathf.Abs(local.y)) * Mathf.Cos(a);   // до боковых прямых
        float arc = st.length - local.magnitude;                                            // до дуги на конце
        return Mathf.Min(Mathf.Min(sides, arc), local.x);                                   // и не позади героя
    }

    // Половина угла, который занимает конус на расстоянии r от героя (радианы). -1 = на этом расстоянии зоны нет
    public static float ConeHalfAngleAt(HammerStage st, float r)
    {
        if (r <= 0f || r > st.length) return -1f;
        float a = ConeSideAngle(st);
        float b = ConeBase(st) * 0.5f;
        float k = Mathf.Clamp(b * Mathf.Cos(a) / r, 0f, 1f);
        return Mathf.Min(Mathf.PI * 0.5f, a + Mathf.Asin(k));    // у героя - почти полукруг, дальше сужается до боковых прямых
    }

    // Самая дальняя точка зоны от центра (для поиска целей)
    public static float MaxReach(HammerStage st)
    {
        if (st.shape == HammerShape.Lane) return Mathf.Sqrt(st.length * st.length + st.width * st.width * 0.25f);
        if (st.shape == HammerShape.HalfEllipse) return Mathf.Max(st.length, st.width * 0.5f);
        return st.length;   // круг и конус
    }

    // ---------- Удар ----------

    // ctx.comboStep = номер ступени (0..2), ctx.origin = откуда идёт волна, ctx.direction = куда
    public override void Execute(AbilityContext ctx)
    {
        HammerStage st = GetStage(ctx.comboStep);
        SpawnEffect(ctx);
        Shockwave.Launch(this, st, ctx);
        DrawDebugZone(ctx);
    }

    // Вызывает Shockwave, когда фронт волны дошёл до цели
    public void HitByWave(GameObject target, Collider2D col, AbilityContext ctx, bool edge, Vector2 pushDir)
    {
        HammerStage st = GetStage(ctx.comboStep);
        DealDamage(target, ctx, edge ? st.edgeDamage : st.damage);

        Rigidbody2D body = target.GetComponent<Rigidbody2D>();
        if (body != null && body.bodyType == RigidbodyType2D.Dynamic && pushDir.sqrMagnitude > 0.0001f)
            body.AddForce(pushDir.normalized * (edge ? st.edgeKnockback : st.knockback), ForceMode2D.Impulse);
    }

    // Стоп-кадр при первом попадании волны
    public void WaveFirstHit(AbilityContext ctx) => OnHitFeel(ctx);

    protected override void OnHitFeel(AbilityContext ctx)
    {
        HitStop.Do(GetStage(ctx.comboStep).hitStop);
    }

    // Вспышка в месте удара о землю и огонь у 3-й ступени
    protected override void SpawnEffect(AbilityContext ctx)
    {
        HammerStage st = GetStage(ctx.comboStep);
        Vector2 hitPoint = st.shape == HammerShape.Circle ? ctx.origin : ctx.origin + ctx.direction * 0.6f;
        if (impactPrefab != null) Object.Instantiate(impactPrefab, hitPoint, Quaternion.identity);

        if (st.shape == HammerShape.Circle && firePrefab != null)
        {
            for (int i = 0; i < fireCount; i++)
            {
                float r = (i % 2 == 0) ? st.length * Random.Range(0.75f, 1f) : st.length * Mathf.Sqrt(Random.value) * 0.75f;
                float a = Random.Range(0f, Mathf.PI * 2f);
                Vector2 p = ctx.origin + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r;
                // огонь не ставим за стену
                if (WallDistance(ctx.origin, p - ctx.origin, r) < r) continue;
                Object.Instantiate(firePrefab, p, Quaternion.identity);
            }
        }
    }

    // Контур зоны в окне Scene: красный - граница, жёлтый - начало фиолетового края
    protected override void DrawDebugZone(AbilityContext ctx)
    {
        HammerStage st = GetStage(ctx.comboStep);
        Vector2 f = ctx.direction.normalized, s = new Vector2(-f.y, f.x);
        Vector2 o = ctx.origin;
        float edgeStart = 1f - st.edgeFraction;
        if (st.shape == HammerShape.Lane)
        {
            float w = st.width * 0.5f;
            Vector2 a = o - s * w, b = o + s * w, c = b + f * st.length, d = a + f * st.length;
            Debug.DrawLine(a, b, Color.red, 0.5f); Debug.DrawLine(b, c, Color.red, 0.5f);
            Debug.DrawLine(c, d, Color.red, 0.5f); Debug.DrawLine(d, a, Color.red, 0.5f);
            Debug.DrawLine(a + f * st.length * edgeStart, b + f * st.length * edgeStart, Color.yellow, 0.5f);
            return;
        }
        if (st.shape == HammerShape.Cone)
        {
            float bw = ConeBase(st) * 0.5f;
            float endAng = ConeHalfAngleAt(st, st.length);
            float re = st.length * edgeStart, edgeAng = ConeHalfAngleAt(st, re);
            Vector2 endL = o + (f * Mathf.Cos(-endAng) + s * Mathf.Sin(-endAng)) * st.length;
            Vector2 endR = o + (f * Mathf.Cos(endAng) + s * Mathf.Sin(endAng)) * st.length;
            Debug.DrawLine(o - s * bw, o + s * bw, Color.red, 0.5f);          // основание у героя
            Debug.DrawLine(o - s * bw, endL, Color.red, 0.5f);                // боковые прямые
            Debug.DrawLine(o + s * bw, endR, Color.red, 0.5f);
            Vector2 prevC = endL, prevE = Vector2.zero;
            for (int i = 0; i <= 32; i++)
            {
                float t = Mathf.Lerp(-1f, 1f, i / 32f);
                Vector2 pc = o + (f * Mathf.Cos(t * endAng) + s * Mathf.Sin(t * endAng)) * st.length;
                Vector2 pe = o + (f * Mathf.Cos(t * edgeAng) + s * Mathf.Sin(t * edgeAng)) * re;
                if (i > 0)
                {
                    Debug.DrawLine(prevC, pc, Color.red, 0.5f);                   // дуга на конце
                    Debug.DrawLine(prevE, pe, Color.yellow, 0.5f);                // начало края
                }
                prevC = pc; prevE = pe;
            }
            return;
        }
        bool half = st.shape == HammerShape.HalfEllipse;
        float F = st.length, S = half ? st.width * 0.5f : st.length;
        float from = half ? -90f : 0f, to = half ? 90f : 360f;
        Vector2 prevO = Vector2.zero, prevI = Vector2.zero, prevN = Vector2.zero;
        for (int i = 0; i <= 32; i++)
        {
            float t = Mathf.Lerp(from, to, i / 32f) * Mathf.Deg2Rad;
            Vector2 p = f * (F * Mathf.Cos(t)) + s * (S * Mathf.Sin(t));
            Vector2 po = o + p, pi = o + p * edgeStart, pn = o + p * st.innerFraction;
            if (i > 0)
            {
                Debug.DrawLine(prevO, po, Color.red, 0.5f);
                Debug.DrawLine(prevI, pi, Color.yellow, 0.5f);
                if (st.innerFraction > 0f) Debug.DrawLine(prevN, pn, Color.red, 0.5f);
            }
            prevO = po; prevI = pi; prevN = pn;
        }
    }
}
