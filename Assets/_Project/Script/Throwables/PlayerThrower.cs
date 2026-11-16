using System;
using UnityEngine;

// СЛОТ МЕТАТЕЛЬНОГО ОРУЖИЯ героя. Вешается на префаб героя (рядом с PlayerAbilities).
// В слоте один тип оружия (ThrowableData) и количество. Слот живёт на герое, поэтому переходит между комнатами;
// в хабе (новый забег) очищается.
//
// Управление (клавиша GameKeys.Throw, по умолчанию E):
//   короткое нажатие          - бросок сразу в сторону курсора;
//   удержание                 - прицел этого оружия к курсору, отпустил - бросок;
//   ЛКМ / ПКМ / рывок / Extra Cancel Keys во время удержания - прицел сбрасывается, броска не будет
//                               (а нажатая способность срабатывает как обычно).
// Нельзя бросать во время удара, рывка и прицела молотом: бросок выполнится, как только герой освободится
// (если успеет за Throw Buffer).
//
// Подбор с земли - клавишей взаимодействия (F) через ThrowablePickup:
//   тот же тип - добирается до максимума; другой тип - заменяет, старое со своим количеством ложится на землю.
[RequireComponent(typeof(PlayerAbilities))]
public class PlayerThrower : MonoBehaviour
{
    [Header("Управление")]
    [Tooltip("Если кнопку держат дольше (сек) - появляется прицел")]
    public float holdThreshold = 0.15f;
    [Tooltip("Ещё кнопки, которые сбрасывают прицел (ЛКМ, ПКМ молота и рывок сбрасывают всегда). Например, ультимейт")]
    public KeyCode[] extraCancelKeys = { KeyCode.Q };
    [Tooltip("Бросок, нажатый во время удара или рывка, выполнится, если герой освободится за это время (сек)")]
    public float throwBuffer = 0.2f;

    [Header("Прицел")]
    [Tooltip("Order in Layer прицела относительно героя (-1 = под героем, над полом)")]
    public int aimOrderOffset = -1;

    [Header("Отладка: выдать через меню компонента (⋮) во время игры")]
    public ThrowableData debugThrowable;
    public int debugAmount = 8;

    ThrowableData current;
    int count;
    float cooldownUntil;
    float cooldownDuration;
    bool pressed, aiming;
    float pressTime;
    float queuedUntil;

    PlayerAbilities abilities;
    PlayerHeavyAttack heavy;
    PlayerStats stats;
    SpriteRenderer sr;
    ThrowAim aim;

    // ---------- Для интерфейса и предметов ----------
    public ThrowableData Current => current;
    public int Count => count;
    public int Capacity => current != null ? CapacityFor(current) : 0;
    public bool IsAiming => aiming;
    public float CooldownLeft => Mathf.Max(0f, cooldownUntil - Time.time);
    public float CooldownFraction => cooldownDuration > 0f ? Mathf.Clamp01(CooldownLeft / cooldownDuration) : 0f;
    // Изменилось оружие или количество
    public event Action Changed;

    // Сколько штук этого оружия можно нести: максимум предмета + бонус баффов (ThrowableCapacity, режим «Число»)
    public int CapacityFor(ThrowableData data)
    {
        if (data == null) return 0;
        int bonus = stats != null ? Mathf.RoundToInt(stats.Get(StatType.ThrowableCapacity)) : 0;
        return Mathf.Max(1, data.maxCount + bonus);
    }

    void Awake()
    {
        abilities = GetComponent<PlayerAbilities>();
        heavy = GetComponent<PlayerHeavyAttack>();
        stats = GetComponent<PlayerStats>();
        sr = GetComponent<SpriteRenderer>();

        var go = new GameObject("ThrowAim");          // не дочерний: не крутится и не зеркалится вместе с героем
        aim = go.AddComponent<ThrowAim>();
        aim.sortingLayerID = sr != null ? sr.sortingLayerID : 0;
        aim.sortingOrder = (sr != null ? sr.sortingOrder : 0) + aimOrderOffset;
    }

    void OnEnable() { RunState.RunReset += Clear; }
    void OnDisable() { RunState.RunReset -= Clear; StopAiming(); }

    void OnDestroy()
    {
        if (aim != null) Destroy(aim.gameObject);
    }

    // ---------- Управление ----------

    void Update()
    {
        if (current == null || count <= 0 || abilities.ControlLocked || Time.timeScale == 0f)
        {
            pressed = false;
            queuedUntil = 0f;
            StopAiming();
            return;
        }

        KeyCode key = GameKeys.Throw;
        // пока целишься молотом - метание не начинается (два прицела сразу не нужны)
        if (Input.GetKeyDown(key) && !abilities.LightAttackBlocked)
        {
            pressed = true;
            pressTime = Time.time;
        }

        if (pressed && OtherAbilityPressed())
        {
            pressed = false;          // прицел сброшен, броска не будет
            StopAiming();
            return;
        }

        if (pressed && !aiming && Input.GetKey(key) && Time.time - pressTime >= holdThreshold)
            aiming = true;

        if (aiming) UpdateAim();

        if (pressed && Input.GetKeyUp(key))
        {
            pressed = false;
            StopAiming();
            queuedUntil = Time.time + throwBuffer;     // короткое нажатие или конец прицела - бросок
        }

        if (Time.time < queuedUntil && TryThrow()) queuedUntil = 0f;
    }

    bool OtherAbilityPressed()
    {
        if (Input.GetKeyDown(abilities.lightAttackKey) || Input.GetKeyDown(abilities.movementKey)) return true;
        if (heavy != null && Input.GetKeyDown(heavy.heavyKey)) return true;
        if (extraCancelKeys != null)
            foreach (KeyCode k in extraCancelKeys)
                if (k != KeyCode.None && Input.GetKeyDown(k)) return true;
        return false;
    }

    bool CanThrow =>
        current != null && count > 0 && Time.time >= cooldownUntil &&
        !abilities.IsBusy && !abilities.LightAttackBlocked;

    bool TryThrow()
    {
        if (!CanThrow) return false;

        Vector2 origin = abilities.ChestPosition;
        Vector2 dir = AimDirection(origin);
        abilities.FaceTowards(dir);

        float mult = stats != null ? stats.Get(StatType.AllDamage) : 1f;
        if (mult <= 0f) mult = 1f;                    // параметр не задан в Character Data

        Room room = RoomManager.Instance != null ? RoomManager.Instance.Current : null;
        var ctx = new ThrowContext
        {
            owner = gameObject,
            origin = origin,
            direction = dir,
            damageMultiplier = mult,
            sortingLayerID = sr != null ? sr.sortingLayerID : 0,
            sortingOrder = (sr != null ? sr.sortingOrder : 0) + 1,
            parent = room != null ? room.transform : null
        };

        ThrowableData thrown = current;
        thrown.Throw(ctx);
        CameraShake.Kick(dir * thrown.throwKick);

        cooldownDuration = thrown.throwCooldown;
        cooldownUntil = Time.time + cooldownDuration;
        count--;
        if (count <= 0) { count = 0; current = null; }   // кончилось - слот пуст
        Changed?.Invoke();
        return true;
    }

    // ---------- Прицел ----------

    void UpdateAim()
    {
        Vector2 origin = abilities.ChestPosition;
        Vector2 dir = AimDirection(origin);
        abilities.FaceTowards(dir);
        aim.Begin();
        current.DrawAim(aim, origin, dir, gameObject);
        aim.End();
    }

    void StopAiming()
    {
        aiming = false;
        if (aim != null) aim.Hide();
    }

    Vector2 AimDirection(Vector2 origin)
    {
        Camera cam = Camera.main;
        if (cam == null) return abilities.AttackDirection;
        Vector2 d = (Vector2)cam.ScreenToWorldPoint(Input.mousePosition) - origin;
        return d.sqrMagnitude < 0.0001f ? abilities.AttackDirection : d.normalized;
    }

    // ---------- Подбор ----------

    // Можно ли сейчас взять это оружие (другой тип - всегда, тот же - если не полный запас)
    public bool CanTake(ThrowableData data)
    {
        if (data == null) return false;
        return current != data || count < CapacityFor(data);
    }

    // Взять amount штук. Другой тип заменяет текущий: старое ложится на землю в точке dropAt.
    // Возвращает, сколько не влезло (остаётся лежать)
    public int Take(ThrowableData data, int amount, Vector3 dropAt)
    {
        if (data == null || amount <= 0) return amount;
        if (current != null && current != data) Drop(dropAt);
        if (current == null) { current = data; count = 0; }

        int add = Mathf.Clamp(CapacityFor(data) - count, 0, amount);
        count += add;
        Changed?.Invoke();
        return amount - add;
    }

    // Выложить всё из слота на землю
    public void Drop(Vector3 position)
    {
        if (current == null) return;
        if (current.pickupPrefab != null && count > 0)
            ThrowablePickup.Spawn(current.pickupPrefab, current, count, position);
        else if (count > 0)
            Debug.LogWarning($"У {current.name} не задан Pickup Prefab - старое оружие пропало, а не легло на землю");
        current = null;
        count = 0;
        StopAiming();
        Changed?.Invoke();
    }

    // Очистить слот (новый забег)
    public void Clear()
    {
        current = null;
        count = 0;
        pressed = false;
        queuedUntil = 0f;
        StopAiming();
        Changed?.Invoke();
    }

    [ContextMenu("Выдать отладочное оружие")]
    void GiveDebug()
    {
        if (debugThrowable != null) Take(debugThrowable, debugAmount, transform.position);
    }
}
