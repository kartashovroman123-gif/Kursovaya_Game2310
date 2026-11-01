using System.Collections.Generic;
using UnityEngine;

// Параметр во время игры: базовое значение + список процентных модификаторов.
// Итог = база * (1 + сумма процентов / 100). Не бывает меньше нуля.
public class Stat
{
    public float BaseValue { get; private set; }

    readonly List<(object source, float percent)> modifiers = new List<(object source, float percent)>();

    public Stat(float baseValue)
    {
        BaseValue = baseValue;
    }

    public void SetBase(float value)
    {
        BaseValue = value;
    }

    public float TotalPercent
    {
        get
        {
            float sum = 0f;
            foreach (var m in modifiers) sum += m.percent;
            return sum;
        }
    }

    public float Value => Mathf.Max(0f, BaseValue * (1f + TotalPercent / 100f));

    public void Add(object source, float percent)
    {
        modifiers.Add((source, percent));
    }

    // Убрать всё, что дал этот источник (предмет, усиление и т.д.)
    public int RemoveAll(object source)
    {
        return modifiers.RemoveAll(m => ReferenceEquals(m.source, source));
    }
}
