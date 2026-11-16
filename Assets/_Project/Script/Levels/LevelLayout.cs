using System.Collections.Generic;
using UnityEngine;

// Одна комната на схеме уровня (ещё не объект в сцене - только данные)
public class RoomNode
{
    public Vector2Int cell;                                     // клетка на сетке карты
    public RoomType type = RoomType.Combat;
    public int distance;                                        // сколько переходов от стартовой комнаты
    public RoomNode parent;                                     // к какой комнате пристроена (у старта - null)
    public readonly HashSet<DoorSide> exits = new HashSet<DoorSide>();   // стороны, где есть проход к соседу

    public bool IsDeadEnd => exits.Count == 1;                  // тупик: один проход
    public bool Has(DoorSide side) => exits.Contains(side);
}

// СХЕМА УРОВНЯ: какие комнаты в каких клетках и как они связаны.
// Её строит генератор (LevelGenerator), а по ней потом создаются комнаты в сцене.
// Схема - дерево: между двумя комнатами всегда ровно один путь (без колец).
public class LevelLayout
{
    public readonly Dictionary<Vector2Int, RoomNode> rooms = new Dictionary<Vector2Int, RoomNode>();
    public int levelNumber;      // номер уровня в забеге, с 1
    public int seed;             // по этому числу схему можно повторить точь-в-точь
    public RoomNode start;
    public RoomNode exit;        // комната выхода (на уровне босса - комната босса)

    public int Count => rooms.Count;

    public RoomNode Get(Vector2Int cell) => rooms.TryGetValue(cell, out RoomNode n) ? n : null;

    public RoomNode Neighbour(RoomNode node, DoorSide side) =>
        node.Has(side) ? Get(node.cell + side.ToCell()) : null;

    public int CountOf(RoomType type)
    {
        int n = 0;
        foreach (RoomNode r in rooms.Values) if (r.type == type) n++;
        return n;
    }

    // Краткая запись для Console: размеры и сколько комнат какого типа
    public override string ToString()
    {
        var counts = new Dictionary<RoomType, int>();
        foreach (RoomNode r in rooms.Values) counts[r.type] = counts.TryGetValue(r.type, out int c) ? c + 1 : 1;
        var sb = new System.Text.StringBuilder($"Уровень {levelNumber} (сид {seed}): {Count} комнат, до выхода {exit?.distance} переходов. ");
        foreach (var kv in counts) sb.Append($"{kv.Key} {kv.Value}  ");
        return sb.ToString();
    }
}
