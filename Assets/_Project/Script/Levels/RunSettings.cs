using UnityEngine;

// НАСТРОЙКИ ЗАБЕГА: из каких уровней он состоит и чем они генерируются.
// Один ассет на игру (можно держать несколько: «тест», «обычный», «длинный» - и подставлять нужный).
// Забег:
//   Уровень 1 -> выбор награды -> Уровень 2 -> выбор -> ... (Choices Before Boss выборов)
//   -> последний обычный уровень -> один путь -> Уровень босса -> (позже: хаб)
// При Choices Before Boss = 3: обычные уровни 1-4, уровень босса - 5.
[CreateAssetMenu(fileName = "RunSettings", menuName = "Game/Levels/Run Settings")]
public class RunSettings : ScriptableObject
{
    [Header("Забег")]
    [Tooltip("Сколько раз игрок выбирает награду следующего уровня, прежде чем остаётся только путь к боссу")]
    [Min(0)] public int choicesBeforeBoss = 3;
    [Tooltip("Сколько вариантов в меню выбора")]
    [Min(1)] public int choiceOptions = 3;

    [Header("Генерация")]
    [Tooltip("Генератор обычных уровней")]
    public GridLevelGenerator levelGenerator;
    [Tooltip("Генератор уровня босса")]
    public BossLevelGenerator bossGenerator;
    [Tooltip("0 = каждый раз новая карта. Любое другое число = всегда одни и те же карты (для отладки)")]
    public int fixedSeed = 0;

    [Header("Размещение комнат в сцене")]
    [Tooltip("Расстояние между центрами соседних клеток карты, в клетках. Должно быть больше самой большой комнаты")]
    public Vector2 cellSize = new Vector2(40f, 30f);

    // Номер уровня босса (обычные - от 1 до него)
    public int BossLevelNumber => choicesBeforeBoss + 2;
    public bool IsBossLevel(int levelNumber) => levelNumber >= BossLevelNumber;

    public LevelGenerator GeneratorFor(int levelNumber) =>
        IsBossLevel(levelNumber) ? (LevelGenerator)bossGenerator : levelGenerator;

    // Сид для уровня: случайный или (при Fixed Seed) всегда один и тот же для этого номера
    public int SeedFor(int levelNumber) =>
        fixedSeed != 0 ? fixedSeed + levelNumber * 7919 : Random.Range(1, int.MaxValue);

    public LevelLayout Generate(int levelNumber, int seed)
    {
        LevelGenerator g = GeneratorFor(levelNumber);
        if (g == null)
        {
            Debug.LogError($"Run Settings: не задан генератор для уровня {levelNumber} " +
                           (IsBossLevel(levelNumber) ? "(Boss Generator)" : "(Level Generator)"));
            return null;
        }
        return g.Generate(levelNumber, seed);
    }

    // Где в мире стоит клетка карты (все комнаты уровня создаются сразу, каждая в своём месте)
    public Vector3 CellToWorld(Vector2Int cell) => new Vector3(cell.x * cellSize.x, cell.y * cellSize.y, 0f);
}
