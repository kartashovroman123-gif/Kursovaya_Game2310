using System.Collections.Generic;
using UnityEngine;

// Врождённая пассивка героя: действует весь забег, с самого начала.
[CreateAssetMenu(fileName = "NewPassive", menuName = "Game/Passive")]
public class PassiveData : ScriptableObject
{
    public string displayName;
    [TextArea] public string description;
    public Sprite icon;

    [Tooltip("Какие параметры и на сколько процентов меняет пассивка")]
    public List<StatModifier> modifiers = new List<StatModifier>();
}
