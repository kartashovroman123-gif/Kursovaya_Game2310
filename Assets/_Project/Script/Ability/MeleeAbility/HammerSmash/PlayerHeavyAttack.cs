using System.Collections;
using UnityEngine;

// Управление тяжёлой атакой молотом (ПКМ) и её ступенями.
// Короткое нажатие - удар сразу в сторону курсора.
// Удержание - показывается зона удара (можно ходить, нельзя бить когтями), отпустил - удар.
// Ступени, перезарядки и счётчик взмахов когтями хранятся здесь, на герое
// (ассет HammerCombo общий и во время игры не меняется).
// Вешается на префаб героя рядом с PlayerAbilities. Работает, если в Char Data -> Heavy Attack стоит HammerCombo.
[RequireComponent(typeof(PlayerAbilities))]
public class PlayerHeavyAttack : MonoBehaviour
{
    [Header("Управление")]
    public KeyCode heavyKey = KeyCode.Mouse1;
    [Tooltip("Дольше этого держишь - включается прицел. Короче - удар сразу")]
    public float holdThreshold = 0.2f;
    [Tooltip("Если отпустил кнопку, а герой ещё занят ударом когтей, удар молотом подождёт столько секунд")]
    public float castBuffer = 0.25f;

    PlayerAbilities abilities;
    PlayerStats stats;
    Rigidbody2D rb;

    // ---------- Ступени ----------
    int nextStage;          // какая ступень выйдет следующей: 0, 1, 2
    float cooldownUntil;
    int waitingFor = -1;    // какую ступень сейчас «набираем» взмахами когтей (-1 = никакую)
    int hits, hitsNeeded;
    float windowUntil;
    float unlockedUntil;

    // ---------- Ввод и прицел ----------
    bool pressed, aiming;
    float pressTime, castQueuedUntil;
    HammerZone indicator;
    readonly RaycastHit2D[] castHits = new RaycastHit2D[4];

    // Для интерфейса (позже): номер ступени 1..3, сколько осталось перезарядки, сколько взмахов набрано
    public int NextStageNumber => nextStage + 1;
    public float CooldownLeft => Mathf.Max(0f, cooldownUntil - Time.time);
    public int ClawHits => hits;
    public int ClawHitsNeeded => hitsNeeded;
    public bool IsReady => Time.time >= cooldownUntil;

    HammerCombo Hammer => stats != null && stats.data != null ? stats.data.heavyAttack as HammerCombo : null;

    void Awake()
    {
        abilities = GetComponent<PlayerAbilities>();
        stats = GetComponent<PlayerStats>();
        rb = GetComponent<Rigidbody2D>();
    }

    void OnEnable() { abilities.LightAttackSwung += OnClawSwing; }

    void OnDisable()
    {
        abilities.LightAttackSwung -= OnClawSwing;
        StopAiming();
    }

    void OnDestroy()
    {
        if (indicator != null) Destroy(indicator.gameObject);
    }

    void Update()
    {
        UpdateStages();

        HammerCombo hammer = Hammer;
        if (hammer == null || abilities.ControlLocked || Time.timeScale == 0f)
        {
            pressed = false;
            castQueuedUntil = 0f;
            StopAiming();
            return;
        }

        if (Input.GetKeyDown(heavyKey))
        {
            pressed = true;
            pressTime = Time.time;
        }

        if (pressed && !aiming && Input.GetKey(heavyKey) && Time.time - pressTime >= holdThreshold)
            StartAiming();

        if (aiming) UpdateIndicator(hammer);

        if (pressed && Input.GetKeyUp(heavyKey))
        {
            pressed = false;
            StopAiming();
            castQueuedUntil = Time.time + castBuffer;   // ударить, как только будет можно
        }

        if (Time.time < castQueuedUntil && TryCast(hammer)) castQueuedUntil = 0f;
    }

    // ---------- Ступени ----------

    void OnClawSwing()
    {
        if (waitingFor < 0 || Time.time > windowUntil) return;
        hits++;
        if (hits < hitsNeeded) return;

        // набрал нужное число взмахов: следующая ступень открыта и готова сразу
        nextStage = waitingFor;
        waitingFor = -1;
        cooldownUntil = Mathf.Min(cooldownUntil, Time.time);
        HammerCombo hammer = Hammer;
        unlockedUntil = Time.time + (hammer != null ? hammer.unlockedTimeout : 5f);
    }

    void UpdateStages()
    {
        // не успел набрать взмахи - следующей будет ступень 1
        if (waitingFor >= 0 && Time.time > windowUntil)
        {
            waitingFor = -1;
            hits = 0;
        }
        // открытая ступень не использована вовремя - сброс на ступень 1
        if (nextStage > 0 && Time.time > unlockedUntil)
        {
            nextStage = 0;
            hits = 0;
        }
    }

    // Вызывается в момент удара (если удар отменили рывком - ступень не тратится)
    void Commit(HammerCombo hammer, int stage)
    {
        HammerStage st = hammer.GetStage(stage);
        cooldownUntil = Time.time + st.cooldown;
        nextStage = 0;
        hits = 0;
        if (stage < 2)
        {
            waitingFor = stage + 1;
            hitsNeeded = Mathf.Max(0, st.clawHitsToNext);
            windowUntil = Time.time + st.window;
            if (hitsNeeded == 0) OnClawSwing();   // 0 взмахов = следующая ступень сразу
        }
        else
        {
            waitingFor = -1;      // после 3-й ступени - полный откат и сброс
            hitsNeeded = 0;
        }
    }

    // ---------- Прицел ----------

    void StartAiming()
    {
        aiming = true;
        abilities.LightAttackBlocked = true;
    }

    void StopAiming()
    {
        aiming = false;
        if (abilities != null) abilities.LightAttackBlocked = false;
        if (indicator != null) indicator.Hide();
    }

    void UpdateIndicator(HammerCombo hammer)
    {
        if (indicator == null) indicator = HammerZone.Create(hammer.zoneSortingOrder);

        Vector2 aim = AimDirection();
        abilities.FaceTowards(aim);                          // герой поворачивается туда, куда целится
        HammerStage st = hammer.GetStage(nextStage);
        Vector2 center = nextStage >= 2 ? LandingPoint(hammer, out _) : abilities.ChestPosition;
        float alpha = IsReady ? 1f : hammer.cooldownAlpha;
        indicator.Show(center, aim, st.radius, st.arcAngle, st.edgeWidth,
                       hammer.aimFill, hammer.aimEdge, hammer.aimOutline, alpha);
    }

    // ---------- Удар ----------

    bool TryCast(HammerCombo hammer)
    {
        if (!IsReady || abilities.IsBusy) return false;

        int stage = nextStage;
        Vector2 aim = AimDirection();
        abilities.FaceTowards(aim);
        if (stage >= 2) abilities.RunAction(CastLeap(hammer, aim));
        else abilities.RunAction(CastSwing(hammer, stage, aim));
        return true;
    }

    // Ступени 1 и 2: замах, шаг вперёд, удар вокруг героя
    IEnumerator CastSwing(HammerCombo hammer, int stage, Vector2 aim)
    {
        HammerStage st = hammer.GetStage(stage);
        abilities.BeginAction(st.windup + st.recovery, stats.data.GetHeavyFrames(stage));
        if (st.windup > 0f) yield return new WaitForSeconds(st.windup);

        abilities.Lunge(aim, st.lungeDistance);
        CameraShake.Kick(aim * st.cameraKick);
        hammer.Execute(MakeContext(hammer, stage, abilities.ChestPosition, aim));
        Commit(hammer, stage);
    }

    // Ступень 3: прыжок в точку, удар по кругу вокруг места приземления
    IEnumerator CastLeap(HammerCombo hammer, Vector2 aim)
    {
        HammerStage st = hammer.GetStage(2);
        Vector2 target = LandingPoint(hammer, out Vector2 leapDir);
        float distance = Vector2.Distance(transform.position, target);
        if (distance > 0.05f) aim = leapDir;

        abilities.BeginAction(st.windup + hammer.leapTime + st.recovery, stats.data.GetHeavyFrames(2));
        abilities.GrantInvulnerability(st.windup + hammer.leapTime + hammer.landingInvulnerability);
        if (st.windup > 0f) yield return new WaitForSeconds(st.windup);

        abilities.Dash(leapDir, distance, hammer.leapTime, false);   // полёт; стены его остановят
        yield return new WaitForSeconds(hammer.leapTime);

        CameraShake.Kick(Vector2.down * st.cameraKick);
        hammer.Execute(MakeContext(hammer, 2, transform.position, aim));
        Commit(hammer, 2);
    }

    AbilityContext MakeContext(HammerCombo hammer, int stage, Vector2 origin, Vector2 aim)
    {
        HammerStage st = hammer.GetStage(stage);
        return new AbilityContext
        {
            owner = stats,
            slot = AbilitySlot.Heavy,
            origin = origin,
            direction = aim,
            aim = aim,
            damage = st.edgeDamage,
            range = st.radius,
            comboStep = stage,
            comboFinisher = stage >= 2,
            effectBehind = aim.y > 0f
        };
    }

    // Куда приземлится прыжок: к курсору, но не дальше maxLeapDistance и не сквозь стену
    Vector2 LandingPoint(HammerCombo hammer, out Vector2 dir)
    {
        Vector2 feet = transform.position;
        Vector2 toCursor = CursorWorld() - feet;
        float dist = Mathf.Min(toCursor.magnitude, hammer.maxLeapDistance);
        dir = toCursor.sqrMagnitude > 0.0001f ? toCursor.normalized : abilities.AttackDirection;

        if (rb != null && dist > 0.01f)
        {
            var filter = new ContactFilter2D { useTriggers = false };
            int n = rb.Cast(dir, filter, castHits, dist);
            if (n > 0) dist = Mathf.Max(0f, castHits[0].distance - 0.05f);
        }
        return feet + dir * dist;
    }

    Vector2 AimDirection()
    {
        Vector2 d = CursorWorld() - abilities.ChestPosition;
        return d.sqrMagnitude < 0.0001f ? abilities.AttackDirection : d.normalized;
    }

    Vector2 CursorWorld()
    {
        Camera cam = Camera.main;
        if (cam == null) return transform.position;
        return cam.ScreenToWorldPoint(Input.mousePosition);
    }
}
