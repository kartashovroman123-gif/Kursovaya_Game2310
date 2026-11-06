using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

// ОБЩАЯ ОСНОВА ЛЮБОЙ КОМНАТЫ (верх дерева: Room -> SafeRoom / CombatRoom -> конкретные комнаты).
// Одинаково у всех комнат: пол (границы камеры), места для дверей (DoorSlot), «пройдена ли комната».
// Чем комнаты отличаются, решают потомки:
//   OnEnter   - что происходит, когда герой вошёл (безопасная комната сразу открывает двери, боевая создаёт врагов);
//   OnCleared - что происходит, когда комната пройдена (боевая выдаёт награду);
//   HasEntrance / ForcedEntrance / ForcedExit / FixedNext / AllowsBossDoor - как устроены двери этой комнаты.
// Сам по себе Room не вешается (abstract) - на префаб ставится один из потомков: HubRoom, CombatRoom, ShopRoom...
public abstract class Room : MonoBehaviour
{
    [Header("Комната")]
    [Tooltip("Тайлмап пола (для границ камеры и центра комнаты)")]
    public Tilemap floor;
    [Tooltip("Где появляется герой, если у комнаты нет входной двери (хаб)")]
    public Transform playerSpawn;

    [Header("Выходы")]
    [Tooltip("Сколько выходов минимум (не считая входа)")]
    [Range(1, 3)] public int minExits = 1;
    [Tooltip("Сколько выходов максимум (не считая входа)")]
    [Range(1, 3)] public int maxExits = 3;

    // Места для дверей (пустые объекты с DoorSlot внутри префаба) - находятся сами
    public DoorSlot[] Slots { get; private set; }
    // Двери, созданные в этой комнате
    public List<Door> Doors { get; } = new List<Door>();

    public bool IsCleared { get; private set; }
    public bool IsUnloading { get; private set; }

    protected virtual void Awake()
    {
        Slots = GetComponentsInChildren<DoorSlot>(true);
    }

    // ---------- Устройство дверей (переопределяют потомки) ----------

    // Есть ли входная дверь (у хаба нет - герой появляется в Player Spawn)
    public virtual bool HasEntrance => true;
    // Вход всегда с этой стороны (null = напротив выхода из прошлой комнаты)
    public virtual DoorSide? ForcedEntrance => null;
    // Выход всегда один и с этой стороны (null = случайные 1-3 выхода)
    public virtual DoorSide? ForcedExit => null;
    // Может ли здесь появиться дверь к боссу
    public virtual bool AllowsBossDoor => true;

    // Куда ведут выходы, если не по общему расписанию менеджера
    // (хаб - только в боевую, босс - в хаб). null = по расписанию
    public virtual RoomType[] FixedNext => null;

    // Найти место для двери с нужной стороны
    public DoorSlot FindSlot(DoorSide side)
    {
        foreach (DoorSlot s in Slots)
            if (s != null && s.side == side) return s;
        return null;
    }

    // ---------- Вызывает RoomManager ----------

    // Комната только что создана (до дверей). Обычная комната засчитывается в забег
    public virtual void OnLoaded() => RunState.RoomPassed();

    public void Enter()
    {
        IsCleared = false;
        OnEnter();
    }

    // Комната пройдена: выходы открываются, потомок делает своё (награда и т.п.)
    public void Clear()
    {
        if (IsCleared || IsUnloading) return;
        IsCleared = true;
        foreach (Door d in Doors)
            if (d != null) d.OnRoomCleared();
        OnCleared();
    }

    // Перед удалением комнаты (переход в другую): остановить всё, что ещё идёт
    public virtual void PrepareUnload()
    {
        IsUnloading = true;
        StopAllCoroutines();
    }

    // ---------- Для потомков ----------

    protected abstract void OnEnter();
    protected virtual void OnCleared() { }

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

    // Создать объект внутри комнаты (удалится вместе с ней). point пустой - в центре комнаты
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
