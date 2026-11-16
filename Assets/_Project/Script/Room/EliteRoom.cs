using UnityEngine;

// ЭЛИТНАЯ КОМНАТА (нижний уровень: Room -> CombatRoom -> EliteRoom).
// Бой как в обычной боевой комнате, но враги сильнее (какие именно - решается в префабе:
// в точки EnemySpawnPoint ставятся элитные мобы). Заходить в неё не обязательно - это ответвление на карте.
// Награда - ДВА сундука: элитный открывается как обычно, второй (уровня Locked Chest Tier) - только ключом.
public class EliteRoom : CombatRoom
{
    [Header("Элита")]
    [Tooltip("Толчок камеры при первом входе, в клетках (0 = без толчка)")]
    public float enterShake = 0.12f;
    [Tooltip("Уровень второго сундука, который открывается ключом")]
    public ChestTier lockedChestTier = ChestTier.Vault;
    [Tooltip("Расстояние между двумя сундуками, в клетках")]
    public float chestSpacing = 1.6f;

    public override ChestTier RewardTier => ChestTier.Elite;

    protected override void OnFirstEnter()
    {
        base.OnFirstEnter();
        if (enterShake > 0f) CameraShake.Kick(Vector2.down * enterShake);
    }

    protected override void OnCleared()
    {
        SpawnChest(RewardTier, Vector2.left * chestSpacing * 0.5f, false);
        SpawnChest(lockedChestTier, Vector2.right * chestSpacing * 0.5f, true);
    }
}
