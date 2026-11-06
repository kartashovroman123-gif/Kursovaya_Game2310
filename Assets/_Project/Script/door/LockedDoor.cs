using System.Collections;
using UnityEngine;

// ЗАПЕРТАЯ ДВЕРЬ (средний уровень: Door -> LockedDoor -> KeyDoor / BossDoor).
// После зачистки комнаты сама НЕ открывается. Герой подходит, над дверью появляется значок клавиши
// взаимодействия (F) - нажал, и если условие выполнено (CanUnlock), дверь отпирается и открывается.
// Чтобы войти, F нажимается ещё раз (как у любой открытой двери).
public abstract class LockedDoor : Door
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

    // Открыта - как обычная дверь (вход). Закрыта - можно ли отпереть
    public override bool CanInteract(GameObject player)
    {
        if (IsOpen) return base.CanInteract(player);
        return RoomCleared && !IsOpening && !unlocking && CanUnlock(player);
    }

    public override void Interact(GameObject player)
    {
        if (IsOpen) { base.Interact(player); return; }     // второе нажатие - вход
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
}
