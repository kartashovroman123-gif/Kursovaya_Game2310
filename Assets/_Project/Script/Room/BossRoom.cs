using UnityEngine;

// Комната босса (нижний уровень: Room -> CombatRoom -> BossRoom).
// Всё как в боевой комнате (босс - это враг в точке EnemySpawnPoint), плюс:
//  - толчок камеры при входе и после победы;
//  - после победы все двери ведут в хаб (позже - на следующий этаж);
//  - здесь не появляется ещё одна дверь к боссу.
public class BossRoom : CombatRoom
{
    [Header("Босс")]
    [Tooltip("Толчок камеры при входе и после победы, в клетках")]
    public float shake = 0.25f;

    static readonly RoomType[] afterBoss = { RoomType.Hub };

    public override bool AllowsBossDoor => false;
    public override RoomType[] NextChoices(RoomType[] defaults) => afterBoss;

    protected override void OnEnter()
    {
        base.OnEnter();
        CameraShake.Kick(Vector2.down * shake);
    }

    protected override void OnCleared()
    {
        base.OnCleared();
        CameraShake.Kick(Vector2.up * shake);
    }
}
