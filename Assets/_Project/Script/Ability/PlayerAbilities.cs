using System.Collections;
using UnityEngine;

// Лицом к камере (вид 3/4 спереди) или спиной (вид 3/4 сзади). Влево/вправо - зеркалом.
public enum AttackFacing { Front, Back }

// Запускает приёмы героя по кнопкам. Сейчас умеет слабую атаку (ЛКМ) в стиле Cult of the Lamb:
// - удар идёт в один из 4 углов (вверх-вправо, вверх-влево, вниз-вправо, вниз-влево), смотря где курсор;
//   дуга захватывает угол и обе соседние стороны, поэтому промахнуться «чуть-чуть» нельзя;
// - серия из 3 ударов, взмахи по очереди в разные стороны, третий сильнее;
// - короткий рывок к курсору, во время удара герой стоит;
// - клик во время удара запоминается.
// Здоровья и PlayerStats не меняет, только читает из него данные героя (stats.data).
[RequireComponent(typeof(PlayerStats))]
[DefaultExecutionOrder(100)]   // наш FixedUpdate идёт ПОСЛЕ скрипта движения, рывок не перезаписывается
public class PlayerAbilities : MonoBehaviour
{
    [Header("Управление")]
    public KeyCode lightAttackKey = KeyCode.Mouse0;   // зажатие повторяет удар
    [Tooltip("Клик во время удара запоминается на столько секунд и срабатывает сразу, как только можно")]
    public float inputBuffer = 0.2f;

    [Tooltip("Кнопка приёма передвижения (рывок). Работает, когда в Char Data назначен приём Movement")]
    public KeyCode movementKey = KeyCode.LeftShift;
    [Tooltip("Рывок можно нажать во время удара: удар прерывается (как в Cult of the Lamb)")]
    public bool dashCancelsAttack = true;

    [Header("Рывок: усиления (меняют предметы и баффы во время забега)")]
    [Tooltip("Множитель длины рывка: 1 = как в ассете, 1.5 = на 50% длиннее")]
    public float dashDistanceMultiplier = 1f;
    [Tooltip("Сколько секунд неуязвимости добавить к рывку сверх значения в ассете")]
    public float dashInvulnerabilityBonus = 0f;

    [Header("Точка удара")]
    [Tooltip("На сколько клеток выше ног «грудь»: от неё считаются направление и дуга")]
    public float chestHeight = 0.6f;

    [Header("Рывок вперёд при ударе")]
    [Tooltip("На сколько клеток герой подаётся к курсору в момент удара (0 = без рывка)")]
    public float lungeDistance = 0.35f;
    [Tooltip("За сколько секунд проходит рывок")]
    public float lungeTime = 0.08f;
    [Tooltip("Во сколько раз длиннее рывок у последнего удара серии")]
    public float finisherLungeMultiplier = 1.8f;
    [Tooltip("Герой не ходит, пока идёт удар (как в Cult of the Lamb)")]
    public bool lockMovement = true;

    [Header("Серия ударов")]
    [Tooltip("Сколько ударов в серии. Последний - сильнее и с большим рывком")]
    public int comboLength = 3;
    [Tooltip("Сколько секунд после удара можно продолжить серию")]
    public float comboWindow = 0.35f;
    [Tooltip("Дополнительная пауза после последнего удара серии, сек")]
    public float finisherExtraCooldown = 0.2f;

    [Header("Камера")]
    [Tooltip("Насколько камеру толкает в сторону удара при взмахе (нужен CameraShake на камере)")]
    public float swingKick = 0.05f;

    [Header("Поворот героя")]
    [Tooltip("Сколько секунд после удара герой ещё смотрит в сторону удара")]
    public float faceHoldTime = 0.15f;

    PlayerStats stats;
    Rigidbody2D rb;
    characterController mover;   // скрипт движения друга

    float lightCooldownLeft;
    float attackStart;
    float busyUntil;
    float faceUntil;
    float bufferedUntil;
    float movementCooldownLeft;
    float dashUntil;
    float dashStart;
    float invulnerableUntil;
    Vector2 lastPos;
    SpriteRenderer sr;
    FramesByFacing currentPose;   // кадры текущего действия (тяжёлая атака и т.п.), null = обычная атака
    bool dashPose = true;         // показывать ли кадры рывка (у прыжка молота свои кадры)
    int comboStep = -1;

    bool externalLock;   // управление выключил RoomManager (переход между комнатами)
    bool attackLock;     // движение выключили мы на время удара

    Vector2 lungeVelocity;
    float lungeUntil;
    bool lunging;
    readonly RaycastHit2D[] castHits = new RaycastHit2D[4];

    // ---------- Для других скриптов ----------
    public int AttackSideX { get; private set; } = 1;            // 1 = вправо, -1 = влево
    public bool AttackUp { get; private set; }                    // true = вверх (герой спиной)
    public AttackFacing Facing => AttackUp ? AttackFacing.Back : AttackFacing.Front;
    // Направление удара - всегда одна из 4 диагоналей
    public Vector2 AttackDirection => new Vector2(AttackSideX, AttackUp ? 1f : -1f).normalized;
    public int ComboStep => Mathf.Max(0, comboStep);
    public bool IsAttacking => Time.time < faceUntil;                 // PlayerAnimation: куда смотреть
    public bool IsAttackAnimating => Time.time < busyUntil;           // AttackPose: идёт ли удар
    public float AttackProgress => Mathf.InverseLerp(attackStart, busyUntil, Time.time);   // 0..1
    public Vector2 ChestPosition => (Vector2)transform.position + Vector2.up * chestHeight;

    // Для тяжёлой атаки и других приёмов
    public event System.Action LightAttackSwung;                      // каждый взмах когтями
    public bool LightAttackBlocked { get; set; }                       // например, пока целишься молотом
    public bool IsBusy => Time.time < busyUntil || Time.time < dashUntil;
    public bool ControlLocked => externalLock;                         // переход между комнатами
    public FramesByFacing CurrentPose => IsAttackAnimating ? currentPose : null;
    public bool ShowDashPose => IsDashing && dashPose;

    // Рывок и неуязвимость
    public bool IsDashing => Time.time < dashUntil;                                         // AttackPose: кадры рывка
    public float DashProgress => Mathf.InverseLerp(dashStart, dashUntil, Time.time);       // 0..1
    // ДЛЯ ДРУГА (здоровье): в начале TakeDamage написать  if (abilities.IsInvulnerable) return;
    public bool IsInvulnerable => Time.time < invulnerableUntil;
    public Vector2 MoveVelocity { get; private set; }                                       // реальная скорость героя

    // Сделать героя неуязвимым на seconds секунд (не сокращает уже идущую неуязвимость)
    public void GrantInvulnerability(float seconds)
    {
        if (seconds > 0f) invulnerableUntil = Mathf.Max(invulnerableUntil, Time.time + seconds);
    }

    void Awake()
    {
        stats = GetComponent<PlayerStats>();
        rb = GetComponent<Rigidbody2D>();
        mover = GetComponent<characterController>();
        sr = GetComponent<SpriteRenderer>();
        if (rb != null) lastPos = rb.position;
    }

    // Вызывает RoomManager вместо прямого включения/выключения скрипта движения
    public void SetControl(bool on)
    {
        externalLock = !on;
        if (!on) CancelAttack();
        ApplyMoverState();
    }

    void Update()
    {
        if (lightCooldownLeft > 0f) lightCooldownLeft -= Time.deltaTime;
        if (movementCooldownLeft > 0f) movementCooldownLeft -= Time.deltaTime;

        // удар или рывок закончился - возвращаем ходьбу
        if (attackLock && Time.time >= busyUntil && Time.time >= dashUntil)
        {
            attackLock = false;
            ApplyMoverState();
        }

        if (Time.timeScale == 0f) return;                                // пауза
        if (externalLock) return;                                        // переход между комнатами
        if (mover != null && !mover.enabled && !attackLock) return;      // движение выключил кто-то другой

        if (Input.GetKeyDown(movementKey)) TryMovement();

        if (LightAttackBlocked) { bufferedUntil = 0f; return; }
        if (Input.GetKeyDown(lightAttackKey)) bufferedUntil = Time.time + inputBuffer;
        if (Input.GetKey(lightAttackKey) || Time.time < bufferedUntil) TryLightAttack();
    }

    void TryLightAttack()
    {
        if (stats == null || stats.data == null) return;
        AbilityData ability = stats.data.lightAttack;
        if (ability == null) return;
        if (lightCooldownLeft > 0f || Time.time < busyUntil || Time.time < dashUntil) return;

        bufferedUntil = 0f;
        currentPose = null;

        // серия: продолжаем, если прошлый удар был недавно, иначе начинаем сначала
        int len = Mathf.Max(1, comboLength);
        bool continueCombo = comboStep >= 0 && comboStep < len - 1 && Time.time <= busyUntil + comboWindow;
        comboStep = continueCombo ? comboStep + 1 : 0;
        bool finisher = len > 1 && comboStep == len - 1;

        // угол удара по курсору; если курсор почти ровно над/под героем или сбоку - оставляем прошлую сторону
        Vector2 aim = AimDirection();
        if (Mathf.Abs(aim.x) > 0.05f) AttackSideX = aim.x > 0f ? 1 : -1;
        if (Mathf.Abs(aim.y) > 0.05f) AttackUp = aim.y > 0f;

        attackStart = Time.time;
        lightCooldownLeft = ability.cooldown + (finisher ? finisherExtraCooldown : 0f);
        busyUntil = Time.time + ability.windup + ability.recovery;
        faceUntil = busyUntil + faceHoldTime;

        if (lockMovement)
        {
            attackLock = true;
            ApplyMoverState();
            StopBody();
        }

        StartCoroutine(Fire(ability, aim, finisher));
        LightAttackSwung?.Invoke();
    }

    // Урон/эффект срабатывает не сразу, а после замаха
    IEnumerator Fire(AbilityData ability, Vector2 aim, bool finisher)
    {
        if (ability.windup > 0f) yield return new WaitForSeconds(ability.windup);

        Vector2 dir = AttackDirection;
        StartLunge(aim, lungeDistance * (finisher ? finisherLungeMultiplier : 1f));   // рывок - точно к курсору
        CameraShake.Kick(dir * swingKick);

        var ctx = new AbilityContext
        {
            owner = stats,
            slot = AbilitySlot.Light,
            fromItem = false,
            origin = ChestPosition,    // герой мог сдвинуться за время замаха
            direction = dir,           // диагональ угла
            aim = aim.normalized,      // точное направление на курсор
            damage = ability.damage,   // позже здесь будут учитываться предметы и усиления
            range = ability.range,
            comboStep = comboStep,
            comboFinisher = finisher,
            effectBehind = AttackUp
        };
        ability.Execute(ctx);
    }

    // ---------- Приём передвижения (рывок) ----------
    void TryMovement()
    {
        if (stats == null || stats.data == null) return;
        AbilityData ability = stats.data.movement;
        if (ability == null) return;
        if (movementCooldownLeft > 0f || Time.time < dashUntil) return;
        if (Time.time < busyUntil)
        {
            if (!dashCancelsAttack) return;
            CancelAttack();          // прерываем удар ради рывка
            attackLock = true;       // ходьба остаётся выключенной, рывок сейчас начнётся
        }

        Vector2 aim = AimDirection();
        movementCooldownLeft = ability.cooldown;

        // куда герой идёт сейчас; если стоит - куда смотрит (влево/вправо)
        Vector2 move = MoveVelocity.sqrMagnitude > 0.25f
            ? MoveVelocity.normalized
            : new Vector2(sr != null && sr.flipX ? -1f : 1f, 0f);

        var ctx = new AbilityContext
        {
            owner = stats,
            slot = AbilitySlot.Movement,
            origin = ChestPosition,
            direction = aim,
            aim = aim,
            moveDirection = move,
            damage = ability.damage,
            range = ability.range
        };
        ability.Execute(ctx);
    }

    // Вызывает DashAbility: полёт героя в нужную сторону, на это время ходьба выключена
    public void Dash(Vector2 dir, float distance, float time, bool showDashPose = true)
    {
        if (rb == null || distance <= 0f || time <= 0f) return;
        dashPose = showDashPose;
        lungeVelocity = dir.normalized * (distance / time);
        lungeUntil = Time.time + time;
        lunging = true;

        dashStart = Time.time;
        dashUntil = lungeUntil;
        attackLock = true;
        ApplyMoverState();
    }

    // ---------- Для других приёмов (тяжёлая атака) ----------

    // Повернуть героя в сторону dir (одна из 4 поз)
    public void FaceTowards(Vector2 dir)
    {
        if (Mathf.Abs(dir.x) > 0.05f) AttackSideX = dir.x > 0f ? 1 : -1;
        if (Mathf.Abs(dir.y) > 0.05f) AttackUp = dir.y > 0f;
        faceUntil = Mathf.Max(faceUntil, Time.time + faceHoldTime);
    }

    // Начать действие длиной duration: герой занят, стоит на месте, показывает кадры pose
    public void BeginAction(float duration, FramesByFacing pose)
    {
        attackStart = Time.time;
        busyUntil = Time.time + Mathf.Max(0.01f, duration);
        faceUntil = busyUntil + faceHoldTime;
        currentPose = pose;
        bufferedUntil = 0f;
        if (lockMovement)
        {
            attackLock = true;
            ApplyMoverState();
            StopBody();
        }
    }

    // Запустить корутину приёма на герое. Рывок (Shift) или переход между комнатами её прервёт
    public Coroutine RunAction(IEnumerator routine) => StartCoroutine(routine);

    // Небольшой шаг вперёд при ударе
    public void Lunge(Vector2 dir, float distance) => StartLunge(dir, distance);

    // ---------- Рывок при ударе ----------
    void StartLunge(Vector2 dir, float distance)
    {
        if (rb == null || distance <= 0f || lungeTime <= 0f) return;
        lungeVelocity = dir.normalized * (distance / lungeTime);
        lungeUntil = Time.time + lungeTime;
        lunging = true;
    }

    void FixedUpdate()
    {
        // реальная скорость героя (нужна рывку, чтобы лететь туда, куда идёшь)
        if (rb != null)
        {
            MoveVelocity = (rb.position - lastPos) / Time.fixedDeltaTime;
            lastPos = rb.position;
        }

        if (!lunging) return;

        if (Time.time >= lungeUntil)
        {
            lunging = false;
            if (lockMovement) StopBody();
            return;
        }

        if (rb.bodyType == RigidbodyType2D.Dynamic)
        {
            rb.linearVelocity = lungeVelocity;   // физика сама остановит героя у стены
        }
        else
        {
            // Kinematic: сами проверяем, нет ли впереди стены
            Vector2 step = lungeVelocity * Time.fixedDeltaTime;
            var filter = new ContactFilter2D { useTriggers = false };
            int n = rb.Cast(step.normalized, filter, castHits, step.magnitude + 0.02f);
            if (n > 0) step = step.normalized * Mathf.Max(0f, castHits[0].distance - 0.02f);
            rb.MovePosition(rb.position + step);
        }
    }

    void StopBody()
    {
        if (rb != null && rb.bodyType == RigidbodyType2D.Dynamic) rb.linearVelocity = Vector2.zero;
    }

    void CancelAttack()
    {
        StopAllCoroutines();
        lunging = false;
        attackLock = false;
        busyUntil = faceUntil = dashUntil = Time.time;
        currentPose = null;
        dashPose = true;
        bufferedUntil = 0f;
        comboStep = -1;
        StopBody();
    }

    void ApplyMoverState()
    {
        if (mover != null) mover.enabled = !externalLock && !attackLock;
    }

    // Направление от груди героя к курсору мыши
    Vector2 AimDirection()
    {
        Camera cam = Camera.main;
        if (cam == null) return AttackDirection;
        Vector3 mouse = cam.ScreenToWorldPoint(Input.mousePosition);
        Vector2 d = (Vector2)mouse - ChestPosition;
        return d.sqrMagnitude < 0.0001f ? AttackDirection : d.normalized;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(ChestPosition, 0.1f);   // точка, из которой бьёт герой
    }
}
