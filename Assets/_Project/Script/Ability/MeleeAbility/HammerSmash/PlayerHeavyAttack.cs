using System.Collections;
using UnityEngine;

// Управление тяжёлой атакой молотом (ПКМ) и её ступенями.
// Короткое нажатие - удар сразу в сторону курсора.
// Удержание - показывается зона удара (можно ходить), отпустил - удар.
// ЛКМ во время прицела - отменить прицел (удара не будет).
// Выпущенный удар ничто не прерывает. Рывок (Shift) во время замаха двигает героя:
// волна выйдет из новой точки, но в направлении, выбранном при нажатии.
// Удар можно начать прямо во время рывка; сразу после рывка замах короче.
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
    bool pressed, aiming, clawsHeldAfterCancel;
    float pressTime, castQueuedUntil;
    HammerZone indicator;
    readonly RaycastHit2D[] castHits = new RaycastHit2D[8];

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

        // после отмены прицела когти не бьют, пока ЛКМ не отпущена (тот клик был «отменой»)
        if (clawsHeldAfterCancel && !Input.GetKey(abilities.lightAttackKey))
        {
            clawsHeldAfterCancel = false;
            if (!aiming) abilities.LightAttackBlocked = false;
        }

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

        if (aiming)
        {
            if (Input.GetKeyDown(abilities.lightAttackKey))
            {
                // ЛКМ отменяет прицел: удара не будет
                pressed = false;
                clawsHeldAfterCancel = true;
                StopAiming();
                return;
            }
            UpdateIndicator(hammer);
        }

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

    // Вызывается в момент удара о землю
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
        if (abilities != null && !clawsHeldAfterCancel) abilities.LightAttackBlocked = false;
        if (indicator != null) indicator.Hide();
    }

    void UpdateIndicator(HammerCombo hammer)
    {
        if (indicator == null) indicator = HammerZone.Create(hammer.zoneSortingOrder);

        Vector2 aim = AimDirection(hammer);
        abilities.FaceTowards(aim);                          // герой поворачивается туда, куда целится
        HammerStage st = hammer.GetStage(nextStage);
        Vector2 center = nextStage >= 2
            ? LandingPoint(hammer, out _) + Vector2.up * hammer.groundOffset
            : WaveOrigin(hammer);
        float alpha = IsReady ? 1f : hammer.cooldownAlpha;
        indicator.Show(center, aim, st, hammer.aimFill, hammer.aimEdge, hammer.aimOutline, alpha);
    }

    // ---------- Удар ----------

    bool TryCast(HammerCombo hammer)
    {
        if (!IsReady || abilities.IsAttackBusy) return false;   // во время рывка - можно

        int stage = nextStage;
        if (stage >= 2 && abilities.IsDashing) return false;     // прыжок - только после конца рывка (ждём в буфере)
        Vector2 aim = AimDirection(hammer);
        abilities.FaceTowards(aim);

        // связка с рывком: во время рывка или сразу после - замах короче
        bool afterDash = abilities.IsDashing || Time.time - abilities.DashEndTime <= hammer.afterDashWindow;
        float windupMul = afterDash ? hammer.afterDashWindupMultiplier : 1f;

        if (stage >= 2) abilities.RunAction(CastLeap(hammer, windupMul));
        else abilities.RunAction(CastSwing(hammer, stage, aim, windupMul));
        return true;
    }

    // Ступени 1 и 2: замах, удар о землю, волна вперёд
    IEnumerator CastSwing(HammerCombo hammer, int stage, Vector2 aim, float windupMul)
    {
        HammerStage st = hammer.GetStage(stage);
        float windup = st.windup * windupMul;
        abilities.BeginAction(windup + st.recovery, stats.data.GetHeavyFrames(stage), true);
        if (windup > 0f) yield return new WaitForSeconds(windup);

        if (!abilities.IsDashing) abilities.Lunge(aim, st.lungeDistance);   // во время рывка шаг не нужен
        CameraShake.Kick(aim * st.cameraKick);
        hammer.Execute(MakeContext(stage, WaveOrigin(hammer), aim));        // из того места, где герой сейчас
        Commit(hammer, stage);
    }

    // Ступень 3: прыжок в точку, удар о землю, волна полным кругом
    IEnumerator CastLeap(HammerCombo hammer, float windupMul)
    {
        HammerStage st = hammer.GetStage(2);

        Vector2 target = LandingPoint(hammer, out Vector2 leapDir);
        float distance = Vector2.Distance(transform.position, target);
        float windup = st.windup * windupMul;

        abilities.BeginAction(windup + hammer.leapTime + st.recovery, stats.data.GetHeavyFrames(2), true);
        abilities.GrantInvulnerability(windup + hammer.leapTime + hammer.landingInvulnerability);
        if (windup > 0f) yield return new WaitForSeconds(windup);

        abilities.Dash(leapDir, distance, hammer.leapTime, false);   // полёт; стены его остановят
        yield return new WaitForSeconds(hammer.leapTime);

        CameraShake.Kick(Vector2.down * st.cameraKick);
        hammer.Execute(MakeContext(2, WaveOrigin(hammer), leapDir));
        Commit(hammer, 2);
    }

    AbilityContext MakeContext(int stage, Vector2 origin, Vector2 aim)
    {
        return new AbilityContext
        {
            owner = stats,
            slot = AbilitySlot.Heavy,
            origin = origin,
            direction = aim,
            aim = aim,
            range = 0f,
            comboStep = stage,
            comboFinisher = stage >= 2,
            effectBehind = aim.y > 0f
        };
    }

    // Откуда идёт волна: ноги героя, чуть выше
    Vector2 WaveOrigin(HammerCombo hammer)
    {
        return (Vector2)transform.position + Vector2.up * hammer.groundOffset;
    }

    // Куда приземлится прыжок (точка ног): к курсору, но не дальше maxLeapDistance и не сквозь стену
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
            for (int i = 0; i < n; i++)
                if (MeleeAbility.IsWall(castHits[i].collider))
                    dist = Mathf.Min(dist, Mathf.Max(0f, castHits[i].distance - 0.05f));
        }
        return feet + dir * dist;
    }

    Vector2 AimDirection(HammerCombo hammer)
    {
        Vector2 d = CursorWorld() - WaveOrigin(hammer);
        return d.sqrMagnitude < 0.0001f ? abilities.AttackDirection : d.normalized;
    }

    Vector2 CursorWorld()
    {
        Camera cam = Camera.main;
        if (cam == null) return transform.position;
        return cam.ScreenToWorldPoint(Input.mousePosition);
    }
}
