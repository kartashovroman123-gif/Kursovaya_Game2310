using UnityEngine;

// ЭЛИТНАЯ КОМНАТА (нижний уровень: Room -> CombatRoom -> EliteRoom).
// Бой как в обычной боевой комнате, но враги сильнее (какие именно - решается в префабе:
// в точки EnemySpawnPoint ставятся элитные мобы). Награда - сундук уровня Elite.
// Может встретиться и на обычном шаге (вместо боевой), и на особом (вместо магазина/прокачки/предмета).
public class EliteRoom : CombatRoom
{
    [Header("Элита")]
    [Tooltip("Толчок камеры при входе, в клетках (0 = без толчка)")]
    public float enterShake = 0.12f;

    public override ChestTier RewardTier => ChestTier.Elite;

    protected override void OnEnter()
    {
        base.OnEnter();
        if (enterShake > 0f) CameraShake.Kick(Vector2.down * enterShake);
    }
}
