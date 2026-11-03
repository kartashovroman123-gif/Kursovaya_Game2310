using UnityEngine;

// Дверь к боссу (нижний уровень: Door -> LockedDoor -> BossDoor).
// Когда пройдено нужное число комнат, в комнате появляется только этот выход.
// После зачистки герой сам решает, когда идти: подходит и нажимает F - дверь тяжело открывается с толчком камеры.
public class BossDoor : LockedDoor
{
    [Tooltip("Толчок камеры при открытии, в клетках")]
    public float openShake = 0.2f;

    protected override bool CanUnlock(GameObject player) => true;    // ключ не нужен

    protected override void OnUnlock(GameObject player)
    {
        CameraShake.Kick(Vector2.up * openShake);
    }
}
