using UnityEngine;

// ВЫХОД ИЗ КОМНАТЫ (верх дерева: RoomExit -> Passage;  позже RoomExit -> LevelDoor -> ExitDoor / BossDoor).
// Общее для всего, через что герой покидает комнату: в какой комнате стоит, с какой стороны, закрыт ли.
// Как именно закрывается/открывается и как через него проходят - решают потомки.
public abstract class RoomExit : MonoBehaviour
{
    public Room Owner { get; private set; }
    public DoorSlot Slot { get; private set; }
    public DoorSide Side => Slot != null ? Slot.side : DoorSide.Up;
    public bool IsLocked { get; private set; }

    // Вызывает RoomManager сразу после создания
    public virtual void Init(Room owner, DoorSlot slot)
    {
        Owner = owner;
        Slot = slot;
    }

    // Закрыть / открыть. animate = с анимацией (при входе в бой и после зачистки), иначе сразу
    public void SetLocked(bool locked, bool animate)
    {
        if (IsLocked == locked) return;
        IsLocked = locked;
        OnLockChanged(locked, animate);
    }

    // Комната зачищена: по умолчанию выход открывается
    public virtual void OnRoomCleared() => SetLocked(false, true);

    protected abstract void OnLockChanged(bool locked, bool animate);
}
