using System.Collections.Generic;
using UnityEngine;

public enum ItemKind { Active, Passive }

// Предмет. Активный занимает слот активного предмета и срабатывает по кнопке,
// пассивный занимает слот пассивного и просто меняет параметры.
// Оба могут менять параметры героя через модификаторы.
// Имя, описание и иконка - в общем родителе GameItemData (общем для всех предметов, включая метательные).
[CreateAssetMenu(fileName = "NewItem", menuName = "Game/Item")]
public class ItemData : GameItemData
{
    public ItemKind kind = ItemKind.Passive;

    [Tooltip("Изменения параметров, пока предмет в слоте")]
    public List<StatModifier> modifiers = new List<StatModifier>();

    [Header("Только для активного предмета")]
    [Tooltip("Приём, который запускается по кнопке")]
    public AbilityData activeAbility;
    [Tooltip("Перезарядка предмета, сек")]
    public float cooldown = 10f;
}
