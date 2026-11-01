using UnityEngine;

public enum AbilitySlot { Light = 0, Heavy = 1, Ultimate = 2, Movement = 3 }

// Что получает приём в момент использования (уже с учётом всех модификаторов)
public class AbilityContext
{
    public PlayerStats owner;
    public AbilitySlot slot;
    public bool fromItem;        // приём запущен активным предметом
    public Vector2 origin;       // откуда
    public Vector2 direction;    // куда (нормализовано; для ударов это одна из 4 диагоналей)
    public Vector2 aim;          // точное направление на курсор (нормализовано), для рывков и снарядов
    public Vector2 moveDirection; // куда герой идёт сейчас (или куда смотрит, если стоит)
    public float damage;
    public float range;

    // Для «ощущения» удара (заполняет PlayerAbilities)
    public int comboStep;        // номер удара в серии: 0, 1, 2...
    public bool comboFinisher;   // последний, самый сильный удар серии
    public bool effectBehind;    // удар вверх: вспышку рисуем за героем
}

// Базовый шаблон любого приёма. Конкретное поведение задают наследники:
// MeleeAbility (удары ближнего боя) и DashAbility (рывки), у каждого свои потомки.
public abstract class AbilityData : ScriptableObject
{
    [Header("Описание")]
    public string displayName;
    [TextArea] public string description;
    public Sprite icon;

    [Header("Общие параметры")]
    public float damage = 10f;
    [Tooltip("Секунд до повторного использования (у ультимейта не используется)")]
    public float cooldown = 0.5f;
    [Tooltip("Дальность или радиус в клетках. Для рывка: дистанция")]
    public float range = 1f;
    [Tooltip("Замах: задержка от нажатия до срабатывания, сек")]
    public float windup = 0.1f;
    [Tooltip("Восстановление после приёма: другие приёмы нельзя использовать, сек")]
    public float recovery = 0.15f;

    [Header("Анимация (подключим позже)")]
    public string animationTrigger;

    // Что делает приём. Сейчас у всех типов заглушки: пишут в Console и рисуют линию в Scene.
    public abstract void Execute(AbilityContext ctx);
}
