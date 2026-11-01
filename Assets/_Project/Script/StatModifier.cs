using UnityEngine;

// Одно изменение параметра в процентах. Используется в пассивках, предметах и усилениях.
[System.Serializable]
public class StatModifier
{
    public StatType stat;

    [Tooltip("Изменение в процентах: 20 = +20%, -15 = -15%. Для перезарядки минус значит быстрее.")]
    public float percent;
}
