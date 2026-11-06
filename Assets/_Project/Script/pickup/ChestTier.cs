// Уровень сундука-награды. Какой сундук появится, решает комната (CombatRoom.RewardTier):
// боевая - Normal, элитная - Elite, за дверью на ключ - Vault, босс - Boss.
// Новые значения добавляем только в конец списка.
public enum ChestTier
{
    Normal,
    Elite,
    Vault,
    Boss
}
