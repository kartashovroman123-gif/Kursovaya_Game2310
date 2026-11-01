using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Игровое состояние героя на время забега: итоговые параметры (база + модификаторы),
// перезарядки приёмов, заряд ультимейта, слоты предметов, полученные усиления.
// Шаблон (CharacterData) при этом не меняется.
public class PlayerStats : MonoBehaviour
{
    public CharacterData data;

    [Header("Слоты предметов")]
    public ItemData activeItem;
    public ItemData passiveItem;

    [Header("Полученные усиления (до конца забега)")]
    public List<UpgradeData> upgrades = new List<UpgradeData>();

    [Header("Ультимейт")]
    public float ultimateCharge;

    [Header("Отладка: выдать через меню компонента (⋮) во время игры")]
    public ItemData debugItem;
    public UpgradeData debugUpgrade;

    // События для интерфейса, здоровья и т.д.
    public event Action OnStatsChanged;                       // изменились параметры (подписывается здоровье, UI)
    public event Action<float, float> OnUltimateChargeChanged; // текущий заряд, максимум
    public event Action<AbilitySlot> OnAbilityUsed;

    readonly Dictionary<StatType, Stat> stats = new Dictionary<StatType, Stat>();
    readonly float[] cooldownLeft = new float[4];
    float activeItemCooldownLeft;
    float busyUntil;   // до этого времени идёт замах/восстановление

    // --- совместимость со старым кодом ---
    public int maxHealth => Mathf.RoundToInt(Get(StatType.MaxHealth));
    public float moveSpeed => Get(StatType.MoveSpeed);
    public int damage => Mathf.RoundToInt(GetDamage(AbilitySlot.Light));

    // ================= Инициализация =================
    public void Init(CharacterData d)
    {
        data = d;
        stats.Clear();
        for (int i = 0; i < cooldownLeft.Length; i++) cooldownLeft[i] = 0f;
        activeItemCooldownLeft = 0f;
        busyUntil = 0f;
        upgrades.Clear();
        activeItem = null;
        passiveItem = null;
        ultimateCharge = 0f;

        if (d == null) return;

        SetBase(StatType.MaxHealth, d.maxHealth);
        SetBase(StatType.MoveSpeed, d.moveSpeed);
        SetBase(StatType.InvulnerabilityTime, d.invulnerabilityTime);
        SetBase(StatType.AllDamage, 1f);
        SetBase(StatType.UltimateChargePerSecond, d.ultimateChargePerSecond);
        SetBase(StatType.UltimateChargePerHit, d.ultimateChargePerHit);
        SetBase(StatType.ActiveItemCooldown, 0f);

        foreach (AbilitySlot slot in Enum.GetValues(typeof(AbilitySlot)))
        {
            AbilityData a = d.GetAbility(slot);
            SetBase(StatOf(slot, 0), a != null ? a.damage : 0f);
            SetBase(StatOf(slot, 1), a != null ? a.cooldown : 0f);
            SetBase(StatOf(slot, 2), a != null ? a.range : 0f);
        }

        if (d.passive != null) AddModifiers(d.passive, d.passive.modifiers);

        OnStatsChanged?.Invoke();
        RaiseUltimate();
    }

    // ================= Параметры =================
    public float Get(StatType type)
    {
        return stats.TryGetValue(type, out Stat s) ? s.Value : 0f;
    }

    void SetBase(StatType type, float value)
    {
        if (stats.TryGetValue(type, out Stat s)) s.SetBase(value);
        else stats[type] = new Stat(value);
    }

    // 0 = урон, 1 = перезарядка, 2 = дальность
    static StatType StatOf(AbilitySlot slot, int kind)
    {
        return (StatType)(10 * ((int)slot + 1) + kind);
    }

    public void AddModifiers(object source, List<StatModifier> list)
    {
        if (source == null || list == null) return;
        foreach (StatModifier m in list)
        {
            if (m == null) continue;
            if (!stats.TryGetValue(m.stat, out Stat s))
            {
                s = new Stat(0f);
                stats[m.stat] = s;
            }
            s.Add(source, m.percent);
        }
        OnStatsChanged?.Invoke();
    }

    public void RemoveModifiers(object source)
    {
        if (source == null) return;
        foreach (Stat s in stats.Values) s.RemoveAll(source);
        OnStatsChanged?.Invoke();
    }

    // ================= Приёмы =================
    public AbilityData GetAbility(AbilitySlot slot) => data != null ? data.GetAbility(slot) : null;
    public float GetDamage(AbilitySlot slot) => Get(StatOf(slot, 0)) * Get(StatType.AllDamage);
    public float GetCooldown(AbilitySlot slot) => Get(StatOf(slot, 1));
    public float GetRange(AbilitySlot slot) => Get(StatOf(slot, 2));
    public float GetCooldownLeft(AbilitySlot slot) => cooldownLeft[(int)slot];
    public bool IsBusy => Time.time < busyUntil;

    public bool CanUse(AbilitySlot slot)
    {
        if (data == null || GetAbility(slot) == null || IsBusy) return false;
        if (slot == AbilitySlot.Ultimate) return ultimateCharge >= data.ultimateMaxCharge;
        return cooldownLeft[(int)slot] <= 0f;
    }

    public bool TryUse(AbilitySlot slot, Vector2 direction)
    {
        if (!CanUse(slot)) return false;
        AbilityData a = GetAbility(slot);

        var ctx = new AbilityContext
        {
            owner = this,
            slot = slot,
            fromItem = false,
            origin = transform.position,
            direction = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.right,
            damage = GetDamage(slot),
            range = GetRange(slot)
        };

        busyUntil = Time.time + a.windup + a.recovery;
        if (slot == AbilitySlot.Ultimate)
        {
            ultimateCharge = 0f;
            RaiseUltimate();
        }
        else
        {
            cooldownLeft[(int)slot] = GetCooldown(slot);
        }

        StartCoroutine(ExecuteAfter(a, ctx));
        OnAbilityUsed?.Invoke(slot);
        return true;
    }

    public bool TryUseActiveItem(Vector2 direction)
    {
        if (activeItem == null || activeItem.activeAbility == null) return false;
        if (activeItemCooldownLeft > 0f || IsBusy) return false;
        AbilityData a = activeItem.activeAbility;

        var ctx = new AbilityContext
        {
            owner = this,
            slot = AbilitySlot.Light,
            fromItem = true,
            origin = transform.position,
            direction = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.right,
            damage = a.damage * Get(StatType.AllDamage),
            range = a.range
        };

        busyUntil = Time.time + a.windup + a.recovery;
        activeItemCooldownLeft = Get(StatType.ActiveItemCooldown);
        StartCoroutine(ExecuteAfter(a, ctx));
        return true;
    }

    IEnumerator ExecuteAfter(AbilityData a, AbilityContext ctx)
    {
        if (a.windup > 0f) yield return new WaitForSeconds(a.windup);
        ctx.origin = transform.position;   // герой мог сдвинуться за время замаха
        a.Execute(ctx);
    }

    // ================= Ультимейт =================
    // Вызывать из атаки за каждое попадание по врагу
    public void RegisterHit()
    {
        AddUltimateCharge(Get(StatType.UltimateChargePerHit));
    }

    public void AddUltimateCharge(float amount)
    {
        if (data == null) return;
        float before = ultimateCharge;
        ultimateCharge = Mathf.Clamp(ultimateCharge + amount, 0f, data.ultimateMaxCharge);
        if (!Mathf.Approximately(before, ultimateCharge)) RaiseUltimate();
    }

    void RaiseUltimate()
    {
        if (data != null) OnUltimateChargeChanged?.Invoke(ultimateCharge, data.ultimateMaxCharge);
    }

    // ================= Предметы и усиления =================
    public void EquipItem(ItemData item)
    {
        if (item == null) return;
        if (item.kind == ItemKind.Active)
        {
            if (activeItem != null) RemoveModifiers(activeItem);
            activeItem = item;
            SetBase(StatType.ActiveItemCooldown, item.cooldown);
            activeItemCooldownLeft = 0f;
        }
        else
        {
            if (passiveItem != null) RemoveModifiers(passiveItem);
            passiveItem = item;
        }
        AddModifiers(item, item.modifiers);
    }

    public void AddUpgrade(UpgradeData upgrade)
    {
        if (upgrade == null) return;
        upgrades.Add(upgrade);
        AddModifiers(upgrade, upgrade.modifiers);
    }

    // ================= Время =================
    void Update()
    {
        float dt = Time.deltaTime;
        for (int i = 0; i < cooldownLeft.Length; i++)
            if (cooldownLeft[i] > 0f) cooldownLeft[i] = Mathf.Max(0f, cooldownLeft[i] - dt);
        if (activeItemCooldownLeft > 0f) activeItemCooldownLeft = Mathf.Max(0f, activeItemCooldownLeft - dt);

        if (data != null && ultimateCharge < data.ultimateMaxCharge)
            AddUltimateCharge(Get(StatType.UltimateChargePerSecond) * dt);
    }

    // ================= Отладка (меню ⋮ у компонента в Inspector) =================
    [ContextMenu("Debug: выдать предмет (Debug Item)")]
    void DebugEquipItem() { EquipItem(debugItem); DebugPrintStats(); }

    [ContextMenu("Debug: выдать усиление (Debug Upgrade)")]
    void DebugAddUpgrade() { AddUpgrade(debugUpgrade); DebugPrintStats(); }

    [ContextMenu("Debug: зарядить ультимейт")]
    void DebugFillUltimate() { if (data != null) AddUltimateCharge(data.ultimateMaxCharge); }

    [ContextMenu("Debug: вывести параметры в Console")]
    void DebugPrintStats()
    {
        var sb = new System.Text.StringBuilder($"Параметры {(data != null ? data.displayName : "?")}:\n");
        foreach (var kv in stats)
            sb.AppendLine($"  {kv.Key}: {kv.Value.Value:0.##} (база {kv.Value.BaseValue:0.##}, {kv.Value.TotalPercent:+0;-0;0}%)");
        Debug.Log(sb.ToString());
    }
}
