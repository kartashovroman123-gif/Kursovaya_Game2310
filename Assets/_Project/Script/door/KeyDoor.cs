using UnityEngine;

// Дверь на ключ (нижний уровень: Door -> LockedDoor -> KeyDoor).
// Появляется не в каждой комнате (шанс задаётся в RoomManager), иначе запечатана как обычная стена-дверь.
// Ведёт в сокровищницу (Vault). Отпирается, если у героя есть ключ; ключ тратится.
public class KeyDoor : LockedDoor
{
    void Reset() { lockedTint = new Color(1f, 0.85f, 0.4f); }   // золотистая (при добавлении компонента)

    public override void Configure(DoorPlan plan)
    {
        if (Random.value < plan.keyDoorChance) Setup(RoomType.Vault, plan.IconFor(RoomType.Vault));
        else Seal();
    }

    protected override bool CanUnlock() => RunState.Keys > 0;

    protected override void OnUnlock()
    {
        RunState.TryUseKey();
        CameraShake.Kick(Vector2.up * 0.06f);
    }
}
