using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

// ОБЩАЯ ОСНОВА ЛЮБОЙ КОМНАТЫ (верх дерева: Room -> SafeRoom / CombatRoom -> конкретные комнаты).
// Все комнаты уровня создаются сразу (RoomManager, по схеме уровня), включена только та, где герой.
// Комнаты не удаляются до конца уровня, поэтому всё, что в них произошло (зачистка, открытые сундуки,
// брошенное оружие), сохраняется само - в пройденные комнаты можно возвращаться.
// Одинаково у всех: пол (границы камеры), 4 места прохода (DoorSlot), выходы, «пройдена ли комната».
// Чем комнаты отличаются, решают потомки:
//   OnFirstEnter - что происходит при ПЕРВОМ входе (безопасная - открывается, боевая - закрывается и зовёт врагов);
//   OnReenter    - при возвращении (по умолчанию ничего);
//   OnCleared    - когда комната пройдена (боевая выдаёт награду).
// Сам по себе Room не вешается (abstract) - на префаб ставится потомок: StartRoom, CombatRoom, ShopRoom...
public abstract class Room : MonoBehaviour
{
    [Header("Комната")]
    [Tooltip("Тайлмап пола (для границ камеры и центра комнаты)")]
    public Tilemap floor;
    [Tooltip("Где появляется герой, если пришёл не через проход (начало уровня)")]
    public Transform playerSpawn;

    // Места проходов (пустые объекты с DoorSlot внутри префаба) - находятся сами
    public DoorSlot[] Slots { get; private set; }
    // Выходы, созданные в этой комнате (проходы, позже - дверь уровня)
    public List<RoomExit> Exits { get; } = new List<RoomExit>();
    // Место комнаты на схеме уровня
    public RoomNode Node { get; private set; }

    public bool IsCleared { get; private set; }
    public bool Visited { get; private set; }
    // Уровень удаляется целиком (переход на следующий) - враги удаляются вместе с ним, это не «смерть»
    public bool IsUnloading { get; private set; }

    protected virtual void Awake()
    {
        Slots = GetComponentsInChildren<DoorSlot>(true);
    }

    // ---------- Вызывает RoomManager ----------

    public void Setup(RoomNode node) => Node = node;

    public DoorSlot FindSlot(DoorSide side)
    {
        foreach (DoorSlot s in Slots)
            if (s != null && s.side == side) return s;
        return null;
    }

    // Герой вошёл в комнату
    public void Enter()
    {
        if (!Visited)
        {
            Visited = true;
            OnFirstEnter();
        }
        else OnReenter();
    }

    // Герой ушёл из комнаты (она сейчас выключится)
    public virtual void OnExit() { }

    // Перед удалением уровня: остановить всё, что ещё идёт
    public virtual void PrepareUnload()
    {
        IsUnloading = true;
        StopAllCoroutines();
    }

    // Комната пройдена: выходы открываются, потомок делает своё (награда и т.п.)
    public void Clear()
    {
        if (IsCleared || IsUnloading) return;
        IsCleared = true;
        foreach (RoomExit e in Exits)
            if (e != null) e.OnRoomCleared();
        OnCleared();
    }

    // ---------- Для потомков ----------

    protected abstract void OnFirstEnter();
    protected virtual void OnReenter() { }
    protected virtual void OnCleared() { }

    // Закрыть все выходы (боевая комната при входе)
    protected void LockExits()
    {
        foreach (RoomExit e in Exits)
            if (e != null) e.SetLocked(true, true);
    }

    // Центр комнаты по полу (если пола нет - позиция комнаты)
    public Vector3 Center
    {
        get
        {
            if (floor == null) return transform.position;
            floor.CompressBounds();
            return floor.transform.TransformPoint(floor.localBounds.center);
        }
    }

    // Создать объект внутри комнаты (останется в ней). point пустой - в центре комнаты
    protected GameObject Spawn(GameObject prefab, Transform point, Vector2 offset = default)
    {
        if (prefab == null) return null;
        Vector3 pos = (point != null ? point.position : Center) + (Vector3)offset;
        pos.z = 0f;
        return Instantiate(prefab, pos, Quaternion.identity, transform);
    }

#if UNITY_EDITOR
    // Для теста, пока нет врагов: клавиша K проходит комнату
    protected virtual void Update()
    {
        if (Input.GetKeyDown(KeyCode.K)) Clear();
    }
#endif
}
