using System.Collections.Generic;
using UnityEngine.Serialization;
using UnityEngine;

// Набор кадров для вида спереди и сзади (влево/вправо - зеркалом)
[System.Serializable]
public class FramesByFacing
{
    public Sprite[] front;   // лицом к камере
    public Sprite[] back;    // спиной к камере

    public bool IsEmpty => (front == null || front.Length == 0) && (back == null || back.Length == 0);

    // Если кадров сзади нет - используются кадры спереди (и наоборот)
    public Sprite[] Get(AttackFacing facing)
    {
        Sprite[] f = facing == AttackFacing.Back ? back : front;
        if (f != null && f.Length > 0) return f;
        return facing == AttackFacing.Back ? front : back;
    }
}

// Шаблон героя: стартовые значения. Во время игры НЕ меняется —
// все изменения от предметов и усилений живут в PlayerStats на объекте героя.
[CreateAssetMenu(fileName = "NewCharacter", menuName = "Game/Character Data")]
public class CharacterData : ScriptableObject
{
    [Header("Отображение")]
    public string displayName;
    [TextArea] public string description;
    public Sprite icon;
    public GameObject prefab;   // префаб героя для игры

    [Header("Базовые параметры")]
    public int maxHealth = 100;
    public float moveSpeed = 5f;
    [Tooltip("Неуязвимость после получения урона, сек")]
    public float invulnerabilityTime = 0.6f;

    [Header("Приёмы")]
    public AbilityData lightAttack;
    public AbilityData heavyAttack;
    public AbilityData ultimate;
    [Tooltip("Приём передвижения (рывок), кнопка - Movement Key в PlayerAbilities")]
    public AbilityData movement;

    [Header("Анимации героя")]
    [Tooltip("Animator Override Controller: стойка и ходьба лицом к камере. Пусто - берётся контроллер из префаба")]
    public RuntimeAnimatorController frontAnimator;
    [Tooltip("Animator Override Controller: стойка и ходьба спиной к камере. Пусто - герой всегда лицом")]
    public RuntimeAnimatorController backAnimator;

    [Header("Кадры слабой атаки (по 3: замах, удар, возврат)")]
    [FormerlySerializedAs("lightAttackFrames")]
    public Sprite[] lightAttackFront;   // бьёт вниз-вбок (вид спереди)
    public Sprite[] lightAttackBack;    // бьёт вверх-вбок (вид сзади)

    [Header("Кадры рывка (полуприсед; растягиваются на всю длину рывка)")]
    public Sprite[] dashFront;          // рывок лицом к камере
    public Sprite[] dashBack;           // рывок спиной к камере

    [Header("Кадры тяжёлой атаки (растягиваются на замах + удар + восстановление)")]
    [Tooltip("Ступень 1: взмах (замах, удар, проводка)")]
    public FramesByFacing heavyStage1;
    [Tooltip("Ступень 2: взмах полукругом. Пусто - кадры ступени 1")]
    public FramesByFacing heavyStage2;
    [Tooltip("Ступень 3: прыжок и удар в землю (молот над головой, удар)")]
    public FramesByFacing heavyStage3;

    [Header("Ультимейт: заряд")]
    public float ultimateMaxCharge = 100f;
    public float ultimateChargePerSecond = 2f;
    public float ultimateChargePerHit = 5f;

    [Header("Пассивка героя")]
    public PassiveData passive;

    [Header("Личные усиления (пул, из которого этот герой может их получать)")]
    public List<UpgradeData> heroUpgrades = new List<UpgradeData>();

    // Для совместимости со старым кодом (например, лобби показывает урон):
    // урон слабой атаки без модификаторов.
    public int damage => lightAttack != null ? Mathf.RoundToInt(lightAttack.damage) : 0;

    // Если кадров сзади нет - используются кадры спереди
    public Sprite[] GetLightAttackFrames(AttackFacing facing) => Pick(facing, lightAttackFront, lightAttackBack);
    public Sprite[] GetDashFrames(AttackFacing facing) => Pick(facing, dashFront, dashBack);

    // stage: 0, 1, 2 -> ступень 1, 2, 3
    public FramesByFacing GetHeavyFrames(int stage)
    {
        if (stage >= 2) return heavyStage3;
        if (stage == 1 && heavyStage2 != null && !heavyStage2.IsEmpty) return heavyStage2;
        return heavyStage1;
    }

    static Sprite[] Pick(AttackFacing facing, Sprite[] front, Sprite[] back)
    {
        Sprite[] f = facing == AttackFacing.Back ? back : front;
        return (f != null && f.Length > 0) ? f : front;
    }

    public AbilityData GetAbility(AbilitySlot slot)
    {
        switch (slot)
        {
            case AbilitySlot.Light: return lightAttack;
            case AbilitySlot.Heavy: return heavyAttack;
            case AbilitySlot.Ultimate: return ultimate;
            default: return movement;
        }
    }
}
