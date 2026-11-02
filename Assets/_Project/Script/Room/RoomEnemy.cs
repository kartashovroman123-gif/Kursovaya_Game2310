using UnityEngine;

// Метка «этот враг из боевой комнаты». Вешать вручную не нужно - CombatRoom добавляет её сама.
// Когда враг уничтожается (Destroy), сообщает комнате о смерти.
public class RoomEnemy : MonoBehaviour
{
    [HideInInspector] public CombatRoom room;
    bool reported;

    // Можно вызвать вручную, если враг при смерти не удаляется
    public void ReportDeath()
    {
        if (reported) return;
        reported = true;
        if (room != null && !room.IsUnloading) room.EnemyDied();
    }

    static bool quitting;
    [RuntimeInitializeOnLoadMethod]
    static void Init()
    {
        quitting = false;
        Application.quitting += () => quitting = true;
    }

    void OnDestroy()
    {
        // при переходе в другую комнату враги удаляются вместе с ней - это не смерть;
        // при выходе из игры / остановке Play - тоже
        if (quitting || !gameObject.scene.isLoaded) return;
        if (room != null && !room.IsUnloading) ReportDeath();
    }
}
