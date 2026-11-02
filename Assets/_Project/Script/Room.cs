using UnityEngine;
using UnityEngine.Tilemaps;

// ОБЩАЯ ОСНОВА ЛЮБОЙ КОМНАТЫ (верх дерева: Room -> SafeRoom / CombatRoom -> конкретные комнаты).
// Одинаково у всех комнат: пол (границы камеры), точка появления героя, двери, «пройдена ли комната».
// Чем комнаты отличаются, решают потомки:
//   OnEnter   - что происходит, когда герой вошёл (безопасная комната сразу открывает двери, боевая создаёт врагов);
//   OnCleared - что происходит, когда комната пройдена (боевая выдаёт награду);
//   OnLoaded / NextChoices / AllowsBossDoor - как комната влияет на забег и куда ведут её двери.
// Сам по себе Room не вешается (abstract) - на префаб ставится один из потомков: HubRoom, CombatRoom, ShopRoom...
public abstract class Room : MonoBehaviour
{
    [Header("Комната")]
    [Tooltip("Тайлмап пола (для границ камеры и центра комнаты)")]
    public Tilemap floor;
    public Transform playerSpawn;
    [Tooltip("Двери комнаты. Заполняется само: берутся все двери внутри префаба")]
    public Door[] doors;
    [Tooltip("Куда могут вести обычные двери. Если пусто, берётся общий список менеджера")]
    public RoomType[] allowedNext;

    public bool IsCleared { get; private set; }
    public bool IsUnloading { get; private set; }

    protected virtual void Awake()
    {
        doors = GetComponentsInChildren<Door>(true);   // чтобы не забыть добавить новую дверь в список
    }

    // ---------- Вызывает RoomManager ----------

    // Комната только что создана (до настройки дверей). Обычная комната засчитывается в забег
    public virtual void OnLoaded() => RunState.RoomPassed();

    // Может ли в этой комнате появиться дверь к боссу
    public virtual bool AllowsBossDoor => true;

    // Из каких типов выбираются обычные двери
    public virtual RoomType[] NextChoices(RoomType[] defaults)
    {
        return allowedNext != null && allowedNext.Length > 0 ? allowedNext : defaults;
    }

    public void Enter()
    {
        IsCleared = false;
        OnEnter();
    }

    // Комната пройдена: двери открываются, потомок делает своё (награда и т.п.)
    public void Clear()
    {
        if (IsCleared || IsUnloading) return;
        IsCleared = true;
        foreach (Door d in doors)
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
