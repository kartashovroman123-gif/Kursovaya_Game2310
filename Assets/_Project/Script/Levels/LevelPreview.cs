using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

// ОТЛАДКА ГЕНЕРАТОРА (этап 1): рисует схему уровня в окне Scene без создания комнат.
// Поставь на пустой объект в любой сцене, укажи Run Settings и жми кнопки в Inspector.
// Цвета: зелёный - старт, синий - выход, красный - босс, серый - бой, оранжевый - элитная,
// жёлтый - магазин, фиолетовый - прокачка, голубой - предмет. Цифра - переходов от старта.
// В игре не нужен - объект можно удалить, когда генератор настроен.
public class LevelPreview : MonoBehaviour
{
    public RunSettings settings;
    [Tooltip("Номер уровня (с 1). Последний - уровень босса")]
    [Min(1)] public int levelNumber = 1;
    [Tooltip("0 = случайный сид при каждой генерации")]
    public int seed = 0;
    [Tooltip("Размер квадрата комнаты на схеме")]
    public float drawSize = 1f;

    LevelLayout layout;
    public LevelLayout Layout => layout;

    public void Generate()
    {
        if (settings == null) { Debug.LogError("Level Preview: не задан Run Settings"); return; }
        int s = seed != 0 ? seed : Random.Range(1, int.MaxValue);
        layout = settings.Generate(levelNumber, s);
        if (layout != null) Debug.Log(layout);
#if UNITY_EDITOR
        SceneView.RepaintAll();
#endif
    }

    // Сгенерировать все уровни забега подряд и вывести сводку в Console
    public void GenerateWholeRun()
    {
        if (settings == null) { Debug.LogError("Level Preview: не задан Run Settings"); return; }
        for (int n = 1; n <= settings.BossLevelNumber; n++)
        {
            LevelLayout l = settings.Generate(n, settings.SeedFor(n));
            Debug.Log(l != null ? l.ToString() : $"Уровень {n}: не построен");
            if (n == levelNumber) layout = l;
        }
#if UNITY_EDITOR
        SceneView.RepaintAll();
#endif
    }

    static Color ColorOf(RoomType t)
    {
        switch (t)
        {
            case RoomType.Start: return new Color(0.3f, 0.9f, 0.4f);
            case RoomType.Exit: return new Color(0.3f, 0.6f, 1f);
            case RoomType.Boss: return new Color(1f, 0.2f, 0.25f);
            case RoomType.Elite: return new Color(1f, 0.55f, 0.15f);
            case RoomType.Shop: return new Color(1f, 0.9f, 0.2f);
            case RoomType.Upgrade: return new Color(0.75f, 0.4f, 1f);
            case RoomType.Item: return new Color(0.3f, 0.95f, 0.95f);
            default: return new Color(0.6f, 0.6f, 0.65f);
        }
    }

    void OnDrawGizmos()
    {
        if (layout == null) return;
        float step = drawSize * 1.6f;
        Vector3 Pos(Vector2Int c) => transform.position + new Vector3(c.x * step, c.y * step, 0f);

        foreach (RoomNode n in layout.rooms.Values)
        {
            Gizmos.color = Color.white;
            foreach (DoorSide s in n.exits)
                if (s == DoorSide.Up || s == DoorSide.Right)                 // каждую связь рисуем один раз
                    Gizmos.DrawLine(Pos(n.cell), Pos(n.cell + s.ToCell()));
        }
        foreach (RoomNode n in layout.rooms.Values)
        {
            Gizmos.color = ColorOf(n.type);
            Gizmos.DrawCube(Pos(n.cell), new Vector3(drawSize, drawSize, 0.01f));
#if UNITY_EDITOR
            Handles.Label(Pos(n.cell) + new Vector3(-drawSize * 0.35f, drawSize * 0.3f, 0f), n.distance.ToString());
#endif
        }
    }
}
