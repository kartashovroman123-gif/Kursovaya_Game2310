using UnityEngine;

// ОБЩИЙ РОДИТЕЛЬ ВСЕХ ПРЕДМЕТОВ (верх дерева):
//   GameItemData -> ItemData (активный / пассивный)
//   GameItemData -> ThrowableData -> ProjectileThrowable -> KukriThrowable / SpearThrowable
// Здесь только то, что есть у любого предмета: имя, описание, иконка.
// Сам по себе ассетом не создаётся (abstract).
public abstract class GameItemData : ScriptableObject
{
    public string displayName;
    [TextArea] public string description;
    [Tooltip("Иконка в интерфейсе")]
    public Sprite icon;
}
