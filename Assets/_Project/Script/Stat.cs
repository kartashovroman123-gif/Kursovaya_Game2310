using System.Collections.Generic;
using UnityEngine;

// Параметр во время игры: базовое значение + модификаторы двух видов - числа и проценты.
// Итог = (база + сумма чисел) * (1 + сумма процентов / 100). Не бывает меньше нуля.
public class Stat
{
    public float BaseValue { get; private set; }

    readonly List<(object source, float amount, ModifierMode mode)> modifiers =
        new List<(object source, float amount, ModifierMode mode)>();

    public Stat(float baseValue)
    {
        BaseValue = baseValue;
    }

    public void SetBase(float value)
    {
        BaseValue = value;
    }

    public float TotalPercent => Sum(ModifierMode.Percent);
    public float TotalFlat => Sum(ModifierMode.Flat);

    float Sum(ModifierMode mode)
    {
        float sum = 0f;
        foreach (var m in modifiers)
            if (m.mode == mode) sum += m.amount;
        return sum;
    }

    public float Value => Mathf.Max(0f, (BaseValue + TotalFlat) * (1f + TotalPercent / 100f));

    // Процентный модификатор (как раньше)
    public void Add(object source, float percent) => Add(source, percent, ModifierMode.Percent);

    public void Add(object source, float amount, ModifierMode mode)
    {
        modifiers.Add((source, amount, mode));
    }

    // Убрать всё, что дал этот источник (предмет, усиление и т.д.)
    public int RemoveAll(object source)
    {
        return modifiers.RemoveAll(m => ReferenceEquals(m.source, source));
    }
}
