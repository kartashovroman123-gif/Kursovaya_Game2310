using System.Collections;
using UnityEngine;

// ЗАПЕРТАЯ ДВЕРЬ (средний уровень: Door -> LockedDoor -> KeyDoor / BossDoor).
// После зачистки комнаты сама НЕ открывается. Герой подходит, над дверью появляется значок клавиши
// взаимодействия (F) - нажал, и если условие выполнено (CanUnlock), дверь отпирается и открывается.
public abstract class LockedDoor : Door, IInteractable
{
    bool unlocking;

    // ----- Что решает конкретная дверь -----
    protected abstract bool CanUnlock(GameObject player);
    protected virtual void OnUnlock(GameObject player) { }
    // Что проигрывается перед открытием (например, падает замок)
    protected virtual IEnumerator UnlockEffect() { yield break; }

    public override void OnRoomCleared()
    {
        RoomCleared = true;          // можно отпирать, но сама не открывается
    }

    // ----- Взаимодействие (клавиша F) -----

    public bool CanInteract(GameObject player)
    {
        return RoomCleared && !IsOpen && !IsOpening && !unlocking && CanUnlock(player);
    }

    public void Interact(GameObject player)
    {
        if (!CanInteract(player)) return;
        unlocking = true;
        OnUnlock(player);
        StartCoroutine(UnlockRoutine());
    }

    IEnumerator UnlockRoutine()
    {
        yield return UnlockEffect();
        Open();
    }

    // Где показать значок клавиши: над дверью
    public Vector3 PromptPosition =>
        transform.position + Vector3.up * (Side == DoorSide.Down ? 1.2f : 1.75f);
}
