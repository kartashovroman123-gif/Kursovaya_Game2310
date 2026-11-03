using UnityEngine;

// Сторона комнаты, у которой стоит дверь
public enum DoorSide
{
    Up,
    Down,
    Left,
    Right
}

public static class DoorSideExtensions
{
    // Противоположная сторона: вышел в правую дверь - в следующей комнате вошёл через левую
    public static DoorSide Opposite(this DoorSide s)
    {
        switch (s)
        {
            case DoorSide.Up: return DoorSide.Down;
            case DoorSide.Down: return DoorSide.Up;
            case DoorSide.Left: return DoorSide.Right;
            default: return DoorSide.Left;
        }
    }

    // Направление от двери внутрь комнаты
    public static Vector2 Inward(this DoorSide s)
    {
        switch (s)
        {
            case DoorSide.Up: return Vector2.down;
            case DoorSide.Down: return Vector2.up;
            case DoorSide.Left: return Vector2.right;
            default: return Vector2.left;
        }
    }
}
