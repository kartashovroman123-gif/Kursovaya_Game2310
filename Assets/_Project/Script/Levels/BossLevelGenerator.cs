using UnityEngine;

// УРОВЕНЬ БОССА (нижний уровень: LevelGenerator -> GridLevelGenerator -> BossLevelGenerator).
// Строится так же, как обычный, но:
//   - вместо комнаты выхода в самом дальнем тупике - комната босса;
//   - число боевых комнат своё и не растёт с номером уровня;
//   - по умолчанию карта прямее (длинный путь к боссу) и с одной особой комнатой.
[CreateAssetMenu(fileName = "BossLevelGenerator", menuName = "Game/Levels/Boss Level Generator")]
public class BossLevelGenerator : GridLevelGenerator
{
    [Header("Уровень босса")]
    [Tooltip("Боевых комнат на уровне босса")]
    [Min(0)] public int bossLevelCombatRooms = 4;

    protected override RoomType ExitType => RoomType.Boss;

    public override int CombatRoomsFor(int levelNumber) => bossLevelCombatRooms;

    // Значения по умолчанию для нового ассета
    public BossLevelGenerator()
    {
        minSpecialRooms = 1;
        maxSpecialRooms = 1;
        corridorBias = 0.7f;
        maxExitsPerRoom = 3;
        minExitDistance = 4;
    }
}
