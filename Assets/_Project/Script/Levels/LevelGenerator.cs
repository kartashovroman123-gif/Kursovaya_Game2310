using System.Collections.Generic;
using UnityEngine;

// ГЕНЕРАТОР УРОВНЯ (верх дерева: LevelGenerator -> GridLevelGenerator -> BossLevelGenerator).
// Строит СХЕМУ уровня (LevelLayout) - какие комнаты где и как связаны. Объекты в сцене по схеме создаются отдельно.
// Порядок всегда один (шаблонный метод Generate):
//   1. BuildShape   - расставить клетки комнат и проходы между ними;
//   2. расстояния от старта до каждой комнаты (считаются здесь);
//   3. AssignTypes  - решить, где выход, где особые и элитные комнаты;
//   4. Validate     - проверить, что схема годится.
// Не получилось - пробуем заново (это доли миллисекунды). Как именно строить форму и раздавать типы - решают потомки.
// Случайность только через System.Random с сидом: одинаковый сид = одинаковая карта (удобно ловить баги).
public abstract class LevelGenerator : ScriptableObject
{
    [Header("Попытки")]
    [Tooltip("Сколько раз пробовать построить схему, если условия не выполнились")]
    [Min(1)] public int maxAttempts = 200;

    // Во второй половине попыток потомки могут смягчить требования (например, особые комнаты не обязательно в тупиках)
    protected bool Relaxed { get; private set; }

    public LevelLayout Generate(int levelNumber, int seed)
    {
        var rng = new System.Random(seed);
        LevelLayout last = null;
        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            Relaxed = attempt >= maxAttempts / 2;
            var layout = new LevelLayout { levelNumber = levelNumber, seed = seed };
            last = layout;
            if (!BuildShape(layout, levelNumber, rng)) continue;
            ComputeDistances(layout);
            if (!AssignTypes(layout, levelNumber, rng)) continue;
            if (Validate(layout)) return layout;
        }
        Debug.LogWarning($"{name}: не удалось построить уровень {levelNumber} за {maxAttempts} попыток (сид {seed}). " +
                         "Проверь настройки: слишком много особых комнат или слишком большое Min Exit Distance");
        return last != null && last.start != null && last.exit != null ? last : null;
    }

    // ----- Решают потомки -----
    protected abstract bool BuildShape(LevelLayout layout, int levelNumber, System.Random rng);
    protected abstract bool AssignTypes(LevelLayout layout, int levelNumber, System.Random rng);

    protected virtual bool Validate(LevelLayout layout) => layout.start != null && layout.exit != null;

    // ----- Общее -----
    // Расстояние (число переходов) от старта до каждой комнаты - обход в ширину
    protected static void ComputeDistances(LevelLayout layout)
    {
        foreach (RoomNode n in layout.rooms.Values) n.distance = -1;
        if (layout.start == null) return;
        var queue = new Queue<RoomNode>();
        layout.start.distance = 0;
        queue.Enqueue(layout.start);
        while (queue.Count > 0)
        {
            RoomNode n = queue.Dequeue();
            foreach (DoorSide side in n.exits)
            {
                RoomNode next = layout.Neighbour(n, side);
                if (next == null || next.distance >= 0) continue;
                next.distance = n.distance + 1;
                queue.Enqueue(next);
            }
        }
    }

    protected static void Shuffle<T>(List<T> list, System.Random rng)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}
