using UnityEngine;

// КОМНАТА БОССА (нижний уровень: Room -> CombatRoom -> BossRoom). Самая дальняя комната уровня босса.
// Всё как в боевой комнате (босс - это враг в точке EnemySpawnPoint), плюс толчок камеры при входе и после победы.
// Награда - сундук уровня Boss. Что происходит после победы (конец забега) - этап 4.
public class BossRoom : CombatRoom
{
    [Header("Босс")]
    [Tooltip("Толчок камеры при входе и после победы, в клетках")]
    public float shake = 0.25f;

    public override ChestTier RewardTier => ChestTier.Boss;

    protected override void OnFirstEnter()
    {
        base.OnFirstEnter();
        CameraShake.Kick(Vector2.down * shake);
    }

    protected override void OnCleared()
    {
        base.OnCleared();
        CameraShake.Kick(Vector2.up * shake);
    }
}
