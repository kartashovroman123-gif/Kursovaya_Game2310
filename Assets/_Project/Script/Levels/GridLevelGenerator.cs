using System.Collections.Generic;
using UnityEngine;

// ОБЫЧНЫЙ УРОВЕНЬ (средний уровень: LevelGenerator -> GridLevelGenerator -> BossLevelGenerator).
// Форма: от стартовой комнаты в клетке (0,0) по одной пристраиваются новые комнаты в свободные соседние клетки.
// Стартовая комната - тупик с одним выходом (настраивается: Start Is Dead End).
// Новая клетка не может касаться других комнат, кроме той, к которой пристроена, - поэтому карта получается
// ветвистой, без колец и «комков», с тупиками по краям.
// Типы: выход - самый дальний тупик; особые комнаты (1-2, случайного типа) - в других тупиках;
// остальное - боевые, часть из них элитные.
// Размер растёт с номером уровня: Base Combat Rooms + Combat Rooms Per Level * (номер - 1).
[CreateAssetMenu(fileName = "LevelGenerator", menuName = "Game/Levels/Level Generator")]
public class GridLevelGenerator : LevelGenerator
{
    [Header("Размер уровня")]
    [Tooltip("Боевых комнат на первом уровне")]
    [Min(1)] public int baseCombatRooms = 5;
    [Tooltip("На сколько боевых комнат больше на каждом следующем уровне")]
    [Min(0)] public int combatRoomsPerLevel = 2;
    [Tooltip("Больше этого числа боевых комнат не бывает (0 = без ограничения)")]
    [Min(0)] public int maxCombatRooms = 25;

    [Header("Особые комнаты (магазин, прокачка, предмет)")]
    [Min(0)] public int minSpecialRooms = 1;
    [Min(0)] public int maxSpecialRooms = 2;
    [Tooltip("Из каких типов выбираются особые комнаты")]
    public RoomType[] specialTypes = { RoomType.Shop, RoomType.Upgrade, RoomType.Item };
    [Tooltip("Особые комнаты одного уровня разных типов (если типов хватает)")]
    public bool uniqueSpecials = true;
    [Tooltip("Особые комнаты стоят в тупиках - боковых ответвлениях")]
    public bool specialsInDeadEnds = true;

    [Header("Элитные комнаты")]
    [Tooltip("Шанс, что боевая комната станет элитной")]
    [Range(0f, 1f)] public float eliteChance = 0.25f;
    [Tooltip("Элитная не ближе, чем столько переходов от старта")]
    [Min(1)] public int eliteMinDistance = 2;

    [Header("Форма карты")]
    [Tooltip("У стартовой комнаты только один выход")]
    public bool startIsDeadEnd = true;
    [Tooltip("Сколько проходов может быть у одной комнаты (2 = длинные коридоры, 4 = много развилок)")]
    [Range(2, 4)] public int maxExitsPerRoom = 3;
    [Tooltip("Шанс продолжить последнюю ветку вместо новой развилки (больше = длиннее и прямее)")]
    [Range(0f, 1f)] public float corridorBias = 0.4f;
    [Tooltip("Выход не ближе, чем столько переходов от старта")]
    [Min(1)] public int minExitDistance = 3;

    int plannedSpecials;   // сколько особых комнат решено сделать на этой попытке

    // ----- Размеры (потомок может поменять) -----
    public virtual int CombatRoomsFor(int levelNumber)
    {
        int n = baseCombatRooms + combatRoomsPerLevel * Mathf.Max(0, levelNumber - 1);
        return maxCombatRooms > 0 ? Mathf.Min(n, maxCombatRooms) : n;
    }

    protected virtual int SpecialRoomsFor(int levelNumber, System.Random rng)
    {
        int min = Mathf.Min(minSpecialRooms, maxSpecialRooms);
        return rng.Next(min, Mathf.Max(min, maxSpecialRooms) + 1);
    }

    // Чем отмечен конец уровня (у уровня босса - комната босса)
    protected virtual RoomType ExitType => RoomType.Exit;

    // ----- 1. Форма -----
    protected override bool BuildShape(LevelLayout layout, int levelNumber, System.Random rng)
    {
        plannedSpecials = SpecialRoomsFor(levelNumber, rng);
        int total = 1 + CombatRoomsFor(levelNumber) + plannedSpecials + 1;     // старт + бои + особые + выход

        var start = new RoomNode { cell = Vector2Int.zero, type = RoomType.Start };
        layout.rooms[start.cell] = start;
        layout.start = start;
        var placed = new List<RoomNode> { start };
        RoomNode last = start;

        int guard = total * 100;
        while (layout.Count < total && guard-- > 0)
        {
            RoomNode from = rng.NextDouble() < corridorBias ? last : placed[rng.Next(placed.Count)];
            if (from.exits.Count >= maxExitsPerRoom) continue;
            if (startIsDeadEnd && from == start && start.exits.Count >= 1) continue;   // у старта уже есть выход

            DoorSide side = DoorSideGrid.All[rng.Next(4)];
            Vector2Int cell = from.cell + side.ToCell();
            if (layout.rooms.ContainsKey(cell)) continue;
            if (OccupiedNeighbours(layout, cell) > 1) continue;     // касается ещё чьей-то комнаты - получилось бы кольцо

            var node = new RoomNode { cell = cell, parent = from };
            from.exits.Add(side);
            node.exits.Add(side.Opposite());
            layout.rooms[cell] = node;
            placed.Add(node);
            last = node;
        }
        return layout.Count == total;
    }

    static int OccupiedNeighbours(LevelLayout layout, Vector2Int cell)
    {
        int n = 0;
        foreach (DoorSide s in DoorSideGrid.All)
            if (layout.rooms.ContainsKey(cell + s.ToCell())) n++;
        return n;
    }

    // ----- 3. Типы -----
    protected override bool AssignTypes(LevelLayout layout, int levelNumber, System.Random rng)
    {
        var deadEnds = new List<RoomNode>();
        foreach (RoomNode n in layout.rooms.Values)
            if (n != layout.start && n.IsDeadEnd) deadEnds.Add(n);
        if (deadEnds.Count == 0) return false;

        // выход - самый дальний тупик
        RoomNode exit = deadEnds[0];
        foreach (RoomNode n in deadEnds) if (n.distance > exit.distance) exit = n;
        if (exit.distance < minExitDistance) return false;
        exit.type = ExitType;
        layout.exit = exit;

        // особые - в других тупиках (если тупиков не хватило - во второй половине попыток можно и не в тупиках)
        var candidates = new List<RoomNode>();
        foreach (RoomNode n in layout.rooms.Values)
        {
            if (n == layout.start || n == exit) continue;
            if (specialsInDeadEnds && !Relaxed && !n.IsDeadEnd) continue;
            candidates.Add(n);
        }
        if (candidates.Count < plannedSpecials) return false;
        Shuffle(candidates, rng);

        var pool = new List<RoomType>(specialTypes ?? new RoomType[0]);
        for (int i = 0; i < plannedSpecials; i++)
        {
            if (pool.Count == 0) pool.AddRange(specialTypes ?? new RoomType[0]);   // типов меньше, чем комнат
            if (pool.Count == 0) break;
            int k = rng.Next(pool.Count);
            candidates[i].type = pool[k];
            if (uniqueSpecials) pool.RemoveAt(k);
        }

        // остальное - боевые, часть элитные
        foreach (RoomNode n in layout.rooms.Values)
        {
            if (n.type != RoomType.Combat) continue;
            if (n.distance >= eliteMinDistance && rng.NextDouble() < eliteChance) n.type = RoomType.Elite;
        }
        return true;
    }
}
