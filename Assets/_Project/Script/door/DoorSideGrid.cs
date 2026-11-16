using UnityEngine;

// Стороны комнаты на сетке карты уровня
public static class DoorSideGrid
{
    public static readonly DoorSide[] All = { DoorSide.Up, DoorSide.Down, DoorSide.Left, DoorSide.Right };

    // Сдвиг клетки карты в эту сторону: Up = (0, 1), Right = (1, 0)...
    public static Vector2Int ToCell(this DoorSide s)
    {
        switch (s)
        {
            case DoorSide.Up: return Vector2Int.up;
            case DoorSide.Down: return Vector2Int.down;
            case DoorSide.Left: return Vector2Int.left;
            default: return Vector2Int.right;
        }
    }
}
