// КОМНАТА ЗА ДВЕРЬЮ НА КЛЮЧ (нижний уровень: Room -> CombatRoom -> VaultRoom).
// Теперь это боевая комната: сначала бой (враги - в префабе, возможно мини-босс), потом награда -
// сундук уровня Vault. Дверь на ключ появляется только вместо обычной боевой двери (решает RoomManager).
public class VaultRoom : CombatRoom
{
    public override ChestTier RewardTier => ChestTier.Vault;
}
