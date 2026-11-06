using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable] public class RoomEntry { public RoomType type; public Room prefab; }

// Загрузка комнат, двери и переходы.
// Живёт всегда одна комната: при переходе старая удаляется, новая создаётся из префаба нужного типа.
// Двери создаются здесь в местах DoorSlot новой комнаты:
//   вход - на стороне, противоположной выходу из прошлой комнаты (захлопывается за героем);
//   выходы - случайные 1-3 из остальных мест, тип каждого виден по створке и выбирается по расписанию:
//     обычный шаг - боевые и элитные (хотя бы один выход - обычная боевая);
//       дверь на ключ - иногда, вместо боевой, только если у героя есть ключ (тогда выходов минимум 2);
//     особый шаг (каждая Special Every-я комната) - магазин / прокачка / предмет / элитная,
//       без повторов и хотя бы один выход небоевой;
//   когда пройдено Rooms Before Boss комнат - единственный выход, к боссу.
// Номер комнаты = сколько раз герой вошёл в дверь в этом забеге (RunState.RoomsPassed).
// Забег: хаб -> (из хаба всегда обычная боевая) -> комнаты -> босс -> хаб.
public class RoomManager : MonoBehaviour
{
    public static RoomManager Instance;

    public Room hubPrefab;
    public RoomEntry[] rooms;
    [Tooltip("Затемнение экрана при переходе (необязательно)")]
    public ScreenFader fader;

    [Header("Двери")]
    [Tooltip("Внешний вид всех дверей (ассет Door Style)")]
    public DoorStyle doorStyle;

    [Header("Расписание выходов")]
    [Tooltip("Каждая какая комната особая (магазин, прокачка, предмет). 3 = комнаты 3, 6, 9... 0 = особых нет")]
    [Min(0)] public int specialEvery = 3;
    [Tooltip("Обычный шаг: из чего выбираются выходы (кроме одной гарантированной боевой). Повторы увеличивают шанс")]
    public RoomType[] normalChoices = { RoomType.Combat, RoomType.Combat, RoomType.Elite };
    [Tooltip("Особый шаг: из чего выбираются выходы. Без повторов, хотя бы один небоевой")]
    public RoomType[] specialChoices = { RoomType.Shop, RoomType.Upgrade, RoomType.Item, RoomType.Elite };

    [Header("Забег")]
    [Tooltip("После скольких комнат (не считая хаба) появляется проход к боссу")]
    public int roomsBeforeBoss = 8;
    [Tooltip("Шанс двери на ключ в комнате, если у героя есть ключ (0..1)")]
    [Range(0f, 1f)] public float keyDoorChance = 0.35f;

    Room current;
    Room lastPrefab;
    bool busy;
    DoorSide? lastExit;          // через какую сторону герой вышел из прошлой комнаты
    DoorSlot entranceSlot;

    public Room Current => current;
    public bool Busy => busy;                  // идёт переход между комнатами

    void Awake() { Instance = this; }

    IEnumerator Start()
    {
        busy = true;
        yield return null;                         // даём PlayerSpawner создать героя
        SetPlayerControl(false);
        lastExit = null;
        Load(hubPrefab);
        yield return null;
        if (fader != null) yield return fader.FadeIn();
        SetPlayerControl(true);
        busy = false;
    }

    // Вызывает открытая дверь, когда герой нажал F. true - переход начался
    public bool GoToNext(RoomType type, DoorSide exitSide)
    {
        if (busy) return false;
        Room prefab = PickPrefab(type);
        if (prefab == null) { Debug.LogWarning("Нет префаба комнаты типа " + type); return false; }
        lastExit = exitSide;
        StartCoroutine(Transition(prefab));
        return true;
    }

    IEnumerator Transition(Room prefab)
    {
        busy = true;
        SetPlayerControl(false);
        if (fader != null) yield return fader.FadeOut();
        Load(prefab);
        yield return null;                         // кадр на то, чтобы всё встало на место
        if (fader != null) yield return fader.FadeIn();
        SetPlayerControl(true);
        busy = false;
    }

    Room PickPrefab(RoomType type)
    {
        if (type == RoomType.Hub && hubPrefab != null) return hubPrefab;
        var list = new List<Room>();
        foreach (var e in rooms) if (e.type == type && e.prefab != null) list.Add(e.prefab);
        if (list.Count > 1) list.Remove(lastPrefab);
        return list.Count == 0 ? null : list[Random.Range(0, list.Count)];
    }

    void Load(Room prefab)
    {
        if (prefab == null) { Debug.LogError("RoomManager: не задан префаб комнаты (проверь Hub Prefab и список Rooms)"); return; }
        if (current != null)
        {
            current.PrepareUnload();               // враги и награды удалятся вместе с комнатой - это не «смерть»
            Destroy(current.gameObject);
        }
        current = Instantiate(prefab, Vector3.zero, Quaternion.identity);
        lastPrefab = prefab;

        current.OnLoaded();                        // хаб сбрасывает забег, остальные комнаты засчитываются
        BuildDoors();
        PlacePlayer();
        SetupCamera();
        current.Enter();
    }

    // ---------- Двери ----------

    void BuildDoors()
    {
        current.Doors.Clear();
        entranceSlot = null;
        if (doorStyle == null) { Debug.LogError("RoomManager: не задан Door Style"); return; }

        var free = new List<DoorSlot>();
        foreach (DoorSlot s in current.Slots) if (s != null) free.Add(s);

        // 1. Вход: напротив выхода из прошлой комнаты (или как задано у комнаты)
        if (current.HasEntrance)
        {
            DoorSide want = current.ForcedEntrance ?? (lastExit.HasValue ? lastExit.Value.Opposite() : DoorSide.Down);
            entranceSlot = current.FindSlot(want) ?? current.FindSlot(DoorSide.Down);
            if (entranceSlot == null && free.Count > 0) entranceSlot = free[0];
            if (entranceSlot != null)
            {
                free.Remove(entranceSlot);
                AddDoor<EntranceDoor>(entranceSlot, RoomType.Hub);   // тип у входа не важен
            }
        }

        // 2. Комната с одним заданным выходом (босс: выход сверху)
        if (current.ForcedExit.HasValue)
        {
            DoorSlot exit = current.FindSlot(current.ForcedExit.Value);
            if (exit == null || exit == entranceSlot) exit = free.Count > 0 ? free[0] : null;
            if (exit != null) AddDoor<Door>(exit, RandomType(current.FixedNext ?? normalChoices));
            return;
        }

        Shuffle(free);
        if (free.Count == 0) return;

        // 3. Пора к боссу: единственный выход
        if (current.AllowsBossDoor && RunState.RoomsPassed >= roomsBeforeBoss)
        {
            AddDoor<BossDoor>(free[0], RoomType.Boss);
            return;
        }

        // 4. Обычные выходы: 1-3, типы по расписанию
        int min = Mathf.Clamp(current.minExits, 1, 3);
        int max = Mathf.Clamp(current.maxExits, min, 3);
        int count = Random.Range(min, max + 1);

        List<RoomType> types;
        int nextRoom = RunState.RoomsPassed + 1;            // номер комнаты, в которую ведут эти двери
        if (current.FixedNext != null)                      // хаб: только боевая
        {
            types = new List<RoomType>();
            for (int i = 0; i < count; i++) types.Add(RandomType(current.FixedNext));
        }
        else if (IsSpecialRoom(nextRoom))
        {
            types = PickSpecial(count);
        }
        else
        {
            bool keyDoor = RunState.Keys > 0 && free.Count >= 2 && HasPrefab(RoomType.Vault)
                           && Random.value < keyDoorChance;
            if (keyDoor) count = Mathf.Max(count, 2);
            types = PickNormal(count, keyDoor);
        }

        count = Mathf.Min(types.Count, free.Count);
        for (int i = 0; i < count; i++)
        {
            if (types[i] == RoomType.Vault) AddDoor<KeyDoor>(free[i], RoomType.Vault);
            else AddDoor<Door>(free[i], types[i]);
        }
    }

    // ---------- Расписание ----------

    // Особая ли комната с этим номером (3, 6, 9... при Special Every = 3)
    public bool IsSpecialRoom(int roomNumber) => specialEvery > 0 && roomNumber % specialEvery == 0;

    // Боевые типы (за ними бой). Остальные - безопасные (магазин, прокачка, предмет)
    static bool IsCombatType(RoomType t) =>
        t == RoomType.Combat || t == RoomType.Elite || t == RoomType.Vault || t == RoomType.Boss;

    // Обычный шаг: первая дверь - всегда обычная боевая, затем (если выпала) дверь на ключ, остальные случайно
    List<RoomType> PickNormal(int count, bool keyDoor)
    {
        var result = new List<RoomType> { RoomType.Combat };
        if (keyDoor) result.Add(RoomType.Vault);
        var pool = Available(normalChoices, false);
        while (result.Count < count)
            result.Add(pool.Count > 0 ? pool[Random.Range(0, pool.Count)] : RoomType.Combat);
        return result;
    }

    // Особый шаг: без повторов, первая дверь - небоевая (чтобы магазин и т.п. нельзя было пропустить)
    List<RoomType> PickSpecial(int count)
    {
        var pool = Available(specialChoices, true);
        var result = new List<RoomType>();
        var safe = pool.FindAll(t => !IsCombatType(t));
        if (safe.Count > 0)
        {
            RoomType first = safe[Random.Range(0, safe.Count)];
            result.Add(first);
            pool.Remove(first);
        }
        Shuffle(pool);
        for (int i = 0; i < pool.Count && result.Count < count; i++) result.Add(pool[i]);
        if (result.Count == 0) result.Add(RoomType.Combat);     // список пуст - чтобы не застрять без выхода
        return result;
    }

    // Только типы, для которых есть префаб комнаты (иначе дверь вела бы в никуда). distinct - без повторов
    List<RoomType> Available(RoomType[] choices, bool distinct)
    {
        var list = new List<RoomType>();
        if (choices == null) return list;
        foreach (RoomType t in choices)
            if (HasPrefab(t) && (!distinct || !list.Contains(t))) list.Add(t);
        return list;
    }

    bool HasPrefab(RoomType type)
    {
        if (rooms == null) return false;
        foreach (var e in rooms) if (e != null && e.type == type && e.prefab != null) return true;
        return false;
    }

    // Создать дверь нужного класса в месте slot (внутри комнаты - удалится вместе с ней)
    T AddDoor<T>(DoorSlot slot, RoomType type) where T : Door
    {
        var go = new GameObject($"{typeof(T).Name}_{slot.side}");
        go.transform.SetParent(current.transform, false);
        go.transform.position = slot.transform.position;
        T door = go.AddComponent<T>();
        door.Init(slot.side, type, doorStyle);
        current.Doors.Add(door);
        return door;
    }

    static RoomType RandomType(RoomType[] pool) =>
        pool != null && pool.Length > 0 ? pool[Random.Range(0, pool.Length)] : RoomType.Combat;

    static void Shuffle<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    // ---------- Герой и камера ----------

    // Отключаем управление героем на время перехода.
    // Движение включает/выключает PlayerAbilities: он знает, идёт ли удар,
    // и не включит ходьбу посреди перехода между комнатами.
    void SetPlayerControl(bool on)
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;

        var abilities = player.GetComponent<PlayerAbilities>();
        if (abilities != null) { abilities.SetControl(on); return; }

        var mover = player.GetComponent<characterController>();
        if (mover != null) mover.enabled = on;
    }

    // Герой появляется у входной двери (а в хабе - в Player Spawn)
    void PlacePlayer()
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;
        Vector3 p;
        if (entranceSlot != null) p = entranceSlot.SpawnPoint;
        else if (current.playerSpawn != null) p = current.playerSpawn.position;
        else return;

        var rb = player.GetComponent<Rigidbody2D>();
        if (rb != null) { rb.position = p; rb.linearVelocity = Vector2.zero; }
        player.transform.position = p;
    }

    void SetupCamera()
    {
        var cam = Camera.main != null ? Camera.main.GetComponent<CameraFollow>() : null;
        if (cam == null) return;
        cam.SetBoundsFromTilemap(current.floor);
        cam.Snap();
    }
}
