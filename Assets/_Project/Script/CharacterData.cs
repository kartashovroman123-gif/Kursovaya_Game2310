using System.Collections.Generic;
using UnityEngine.Serialization;
using UnityEngine;

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
    public AbilityData movement;

    [Header("Анимации героя")]
    [Tooltip("Animator Override Controller: стойка и ходьба лицом к камере. Пусто - берётся контроллер из префаба")]
    public RuntimeAnimatorController frontAnimator;
    [Tooltip("Animator Override Controller: стойка и ходьба спиной к камере. Пусто - герой всегда лицом")]
    public RuntimeAnimatorController backAnimator;

    [Header("Временная анимация слабой атаки (по 3 кадра: замах, удар, возврат)")]
    [FormerlySerializedAs("lightAttackFrames")]
    public Sprite[] lightAttackFront;   // бьёт вниз-вбок (вид спереди)
    public Sprite[] lightAttackBack;    // бьёт вверх-вбок (вид сзади)

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
    public Sprite[] GetLightAttackFrames(AttackFacing facing)
    {
        Sprite[] f = facing == AttackFacing.Back ? lightAttackBack : lightAttackFront;
        return (f != null && f.Length > 0) ? f : lightAttackFront;
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