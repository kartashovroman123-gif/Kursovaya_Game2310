using UnityEngine;
using UnityEngine.Serialization;

// Как модификатор меняет параметр
public enum ModifierMode
{
    Percent,   // процент от базы: 20 = +20%
    Flat       // ровно столько: 2 = +2 (например +2 метательных ножа)
}

// Одно изменение параметра. Используется в пассивках, предметах и усилениях.
[System.Serializable]
public class StatModifier
{
    public StatType stat;

    [Tooltip("Проценты - от базы (+20%). Число - прибавляется ровно столько (+2)")]
    public ModifierMode mode = ModifierMode.Percent;

    [FormerlySerializedAs("percent")]   // раньше поле называлось percent - старые ассеты не потеряются
    [Tooltip("Проценты: 20 = +20%, -15 = -15% (для перезарядки минус значит быстрее). Число: 2 = +2, -1 = -1")]
    public float value;

    // Для старого кода: процент (у модификатора в режиме «Число» - 0)
    public float percent => mode == ModifierMode.Percent ? value : 0f;
}
