using UnityEngine;

// МЕСТО ПРОХОДА в префабе комнаты. В каждой комнате их четыре: сверху, снизу, слева, справа.
// Точка объекта = центр проёма на внутренней кромке стены (там, где кончается пол).
// Что здесь будет, решает схема уровня:
//   есть сосед с этой стороны - проход: заглушка (Cap) выключается, появляется зона перехода, решётка (Bars)
//                               опускается, пока боевая комната не зачищена;
//   соседа нет                - заглушка включена (кусок стены), прохода нет.
// В окне Scene: жёлтая линия - ширина проёма, голубая рамка - зона перехода, зелёный кружок - где встанет герой,
// если пришёл через этот проход.
public class DoorSlot : MonoBehaviour
{
    public DoorSide side = DoorSide.Up;

    [Header("Проём")]
    [Tooltip("Ширина проёма, в клетках")]
    public float width = 2f;
    [Tooltip("На каком расстоянии от проёма появляется герой, если вошёл через него (в клетках, в сторону комнаты)")]
    public float spawnDistance = 1.2f;
    [Tooltip("Насколько зона перехода уходит в стену (в клетках). Герой заходит в неё - переход")]
    public float triggerDepth = 0.8f;

    [Header("Объекты в префабе")]
    [Tooltip("Заглушка: кусок стены с коллайдером, закрывающий проём, когда прохода нет. " +
             "Пусто = создастся невидимая стенка (только коллайдер)")]
    public GameObject cap;
    [Tooltip("Решётка: картинка + обычный (не триггер) коллайдер, перекрывающий проём, пока проход закрыт. " +
             "Пусто = невидимая преграда")]
    public GameObject bars;

    // Направление от проёма в стену (наружу из комнаты)
    public Vector2 Outward => -side.Inward();

    public Vector3 SpawnPoint => (Vector2)transform.position + side.Inward() * spawnDistance;

    // Размер прямоугольника поперёк проёма заданной глубины (для зоны перехода и невидимых преград)
    public Vector2 BoxSize(float depth) =>
        side == DoorSide.Up || side == DoorSide.Down ? new Vector2(width, depth) : new Vector2(depth, width);

    // Есть ли проход: заглушка выключается, если есть
    public void SetOpen(bool open)
    {
        if (cap != null) cap.SetActive(!open);
        else if (!open) CreateInvisibleWall("AutoCap", 0.6f);
        if (bars != null) bars.SetActive(false);         // решётку включает проход, когда нужно
    }

    // Невидимая стенка в проёме (если художник не сделал свою)
    public GameObject CreateInvisibleWall(string name, float depth)
    {
        var go = new GameObject(name);
        go.transform.SetParent(transform, false);
        go.transform.position = (Vector2)transform.position + Outward * depth * 0.5f;
        var box = go.AddComponent<BoxCollider2D>();
        box.size = BoxSize(depth);
        return go;
    }

    void OnDrawGizmos()
    {
        Vector2 p = transform.position;
        Vector2 along = side == DoorSide.Up || side == DoorSide.Down ? Vector2.right : Vector2.up;
        Gizmos.color = new Color(1f, 0.8f, 0.2f, 0.95f);
        Gizmos.DrawLine(p - along * width * 0.5f, p + along * width * 0.5f);
        Gizmos.color = new Color(0.3f, 0.8f, 1f, 0.8f);
        Gizmos.DrawWireCube(p + Outward * triggerDepth * 0.5f, BoxSize(triggerDepth));
        Gizmos.color = new Color(0.3f, 1f, 0.5f, 0.9f);
        Gizmos.DrawWireSphere(SpawnPoint, 0.25f);
        Gizmos.DrawLine(p, SpawnPoint);
    }
}
