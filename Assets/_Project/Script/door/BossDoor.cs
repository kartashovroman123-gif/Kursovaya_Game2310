using UnityEngine;

// Дверь к боссу (нижний уровень: Door -> LockedDoor -> BossDoor).
// Запечатана, пока не пройдено нужное число комнат. Потом ведёт к боссу:
// когда комната пройдена, герой подходит к двери - она тяжело открывается с толчком камеры.
public class BossDoor : LockedDoor
{
    [Tooltip("Толчок камеры при открытии, в клетках")]
    public float openShake = 0.2f;

    void Reset() { lockedTint = new Color(1f, 0.45f, 0.45f); }   // красноватая (при добавлении компонента)

    public override void Configure(DoorPlan plan)
    {
        if (plan.bossDue) Setup(RoomType.Boss, plan.IconFor(RoomType.Boss));
        else Seal();
    }

    protected override bool CanUnlock() => true;     // достаточно пройти комнату

    protected override void OnUnlock()
    {
        CameraShake.Kick(Vector2.up * openShake);
    }
}
