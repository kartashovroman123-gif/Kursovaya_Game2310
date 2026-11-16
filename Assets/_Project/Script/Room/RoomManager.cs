using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable] public class RoomEntry { public RoomType type; public Room prefab; }

// УРОВНИ И ПЕРЕХОДЫ МЕЖДУ КОМНАТАМИ.
// Уровень: по Run Settings генерируется схема (LevelLayout), и по ней сразу создаются ВСЕ комнаты уровня -
// каждая в своём месте мира (клетка карты * Cell Size). Включена только комната, где сейчас герой.
// В каждой комнате в местах DoorSlot: есть сосед - проход (Passage), нет - заглушка.
// Герой зашёл в открытый проход -> камера уезжает, экран темнеет -> герой в соседней комнате
// у противоположного прохода -> камера въезжает, экран светлеет. Возвращаться можно куда угодно.
// Забег начинается с уровня 1, герой появляется в стартовой комнате (Player Spawn).
// Переход на следующий уровень (дверь выхода, выбор награды) - этап 3; для теста - меню компонента ⋮.
public class RoomManager : MonoBehaviour
{
    public static RoomManager Instance;

    [Header("Забег")]
    [Tooltip("Настройки забега: генераторы уровней, число выборов до босса, размер клетки карты")]
    public RunSettings settings;

    [Header("Префабы комнат")]
    [Tooltip("По одной строке на каждый префаб. Несколько префабов одного типа - будут чередоваться")]
    public RoomEntry[] rooms;
    [Tooltip("Затемнение экрана при переходе (необязательно)")]
    public ScreenFader fader;

    [Header("Отладка")]
    [Tooltip("С какого уровня начинать (для проверки больших уровней и уровня босса)")]
    [Min(1)] public int startLevel = 1;
    [Tooltip("Пустая боевая комната (без врагов) проходится сама через секунду. Выключить, когда появятся враги")]
    public bool autoClearEmptyRooms = true;
    [Tooltip("Писать схему уровня в Console")]
    public bool logLayout = true;

    Room current;
    bool busy;
    LevelLayout layout;
    Transform levelRoot;
    readonly Dictionary<Vector2Int, Room> built = new Dictionary<Vector2Int, Room>();
    readonly Dictionary<Room, int> prefabUses = new Dictionary<Room, int>();

    public Room Current => current;
    public bool Busy => busy;                 // идёт переход
    public LevelLayout Layout => layout;
    public int LevelNumber { get; private set; }
    public IEnumerable<Room> LevelRooms => built.Values;

    void Awake() { Instance = this; }

    IEnumerator Start()
    {
        busy = true;
        yield return null;                    // даём PlayerSpawner создать героя
        SetPlayerControl(false);
        RunState.ResetRun();
        BuildLevel(startLevel);
        EnterAtStart();
        yield return null;
        if (fader != null) yield return fader.FadeIn();
        SetPlayerControl(true);
        busy = false;
    }

    // ================= Уровень =================

    // Сгенерировать уровень и создать все его комнаты (выключенными)
    public bool BuildLevel(int number)
    {
        if (settings == null) { Debug.LogError("RoomManager: не задан Run Settings"); return false; }
        DestroyLevel();

        LevelNumber = number;
        RunState.SetLevel(number);
        layout = settings.Generate(number, settings.SeedFor(number));
        if (layout == null) { Debug.LogError($"RoomManager: уровень {number} не сгенерировался"); return false; }
        if (logLayout) Debug.Log(layout);

        levelRoot = new GameObject($"Level_{number}").transform;
        prefabUses.Clear();
        foreach (RoomNode node in layout.rooms.Values)
        {
            Room prefab = PickPrefab(node.type);
            if (prefab == null) continue;
            Room room = Instantiate(prefab, settings.CellToWorld(node.cell), Quaternion.identity, levelRoot);
            room.name = $"{node.type} {node.cell.x},{node.cell.y} ({prefab.name})";
            room.Setup(node);
            BuildExits(room);
            room.gameObject.SetActive(false);
            built[node.cell] = room;
        }
        return built.ContainsKey(layout.start.cell);
    }

    void DestroyLevel()
    {
        current = null;
        foreach (Room r in built.Values) if (r != null) r.PrepareUnload();
        built.Clear();
        if (levelRoot != null) Destroy(levelRoot.gameObject);
        levelRoot = null;
    }

    // Проходы и заглушки в 4 местах DoorSlot по схеме
    void BuildExits(Room room)
    {
        foreach (DoorSide side in DoorSideGrid.All)
        {
            bool open = room.Node.Has(side);
            DoorSlot slot = room.FindSlot(side);
            if (slot == null)
            {
                if (open) Debug.LogError($"В префабе {room.name} нет DoorSlot стороны {side}, а по схеме там проход");
                continue;
            }
            slot.SetOpen(open);
            if (!open) continue;

            var go = new GameObject($"Passage_{side}");
            go.transform.SetParent(slot.transform, false);
            Passage p = go.AddComponent<Passage>();
            p.Init(room, slot);
            room.Exits.Add(p);
        }
    }

    // Префаб для типа: из нескольких - тот, что использовался реже (комнаты не повторяются, пока есть другие)
    Room PickPrefab(RoomType type)
    {
        var list = new List<Room>();
        foreach (RoomType t in FallbackChain(type))
        {
            if (rooms != null)
                foreach (RoomEntry e in rooms)
                    if (e != null && e.type == t && e.prefab != null) list.Add(e.prefab);
            if (list.Count > 0)
            {
                if (t != type) Debug.LogWarning($"RoomManager: нет префаба комнаты {type}, вместо неё {t}");
                break;
            }
        }
        if (list.Count == 0) { Debug.LogError($"RoomManager: нет префаба комнаты {type} (и замены). Добавь его в список Rooms"); return null; }

        int least = int.MaxValue;
        foreach (Room r in list) least = Mathf.Min(least, Uses(r));
        list.RemoveAll(r => Uses(r) > least);
        Room pick = list[Random.Range(0, list.Count)];
        prefabUses[pick] = Uses(pick) + 1;
        return pick;
    }

    int Uses(Room r) => prefabUses.TryGetValue(r, out int n) ? n : 0;

    // Чем заменить комнату, если для её типа нет префаба (чтобы уровень всё равно собрался)
    static IEnumerable<RoomType> FallbackChain(RoomType type)
    {
        yield return type;
        switch (type)
        {
            case RoomType.Elite: yield return RoomType.Combat; break;
            case RoomType.Boss: yield return RoomType.Elite; yield return RoomType.Combat; break;
            case RoomType.Exit:
            case RoomType.Shop:
            case RoomType.Upgrade:
            case RoomType.Item: yield return RoomType.Start; break;
        }
    }

    // ================= Комнаты =================

    void EnterAtStart()
    {
        if (layout == null || !built.TryGetValue(layout.start.cell, out Room start)) return;
        Activate(start);
        Vector3 p = start.playerSpawn != null ? start.playerSpawn.position : start.Center;
        PlacePlayer(p);
        SetupCamera();
        start.Enter();
    }

    // Вызывает открытый проход, когда в него зашёл герой
    public void GoThrough(Passage passage)
    {
        if (busy || layout == null || passage.Owner != current) return;
        RoomNode next = layout.Neighbour(current.Node, passage.Side);
        if (next == null || !built.TryGetValue(next.cell, out Room nextRoom)) return;
        StartCoroutine(Transition(nextRoom, passage.Side));
    }

    IEnumerator Transition(Room next, DoorSide exitSide)
    {
        busy = true;
        SetPlayerControl(false);
        Vector2 dir = exitSide.ToCell();

        // отъезд камеры + затемнение одновременно
        Coroutine slide = StartCoroutine(CameraTransition.SlideOut(dir));
        if (fader != null) yield return fader.FadeOut();
        yield return slide;

        current.OnExit();
        Activate(next);
        DoorSlot entrance = next.FindSlot(exitSide.Opposite());
        PlacePlayer(entrance != null ? entrance.SpawnPoint : next.Center);
        SetupCamera();
        next.Enter();
        yield return null;                    // кадр на то, чтобы всё встало на место

        // въезд камеры + осветление
        slide = StartCoroutine(CameraTransition.SlideIn(dir));
        if (fader != null) yield return fader.FadeIn();
        yield return slide;

        SetPlayerControl(true);
        busy = false;
    }

    void Activate(Room room)
    {
        if (current != null && current != room) current.gameObject.SetActive(false);
        current = room;
        current.gameObject.SetActive(true);
    }

    // ================= Переход на уровень (этап 3 вызовет это из двери выхода) =================

    public void GoToLevel(int number)
    {
        if (busy) return;
        StartCoroutine(LevelTransition(number));
    }

    IEnumerator LevelTransition(int number)
    {
        busy = true;
        SetPlayerControl(false);
        if (fader != null) yield return fader.FadeOut();
        BuildLevel(number);
        EnterAtStart();
        yield return null;
        if (fader != null) yield return fader.FadeIn();
        SetPlayerControl(true);
        busy = false;
    }

    [ContextMenu("Отладка: следующий уровень")]
    void DebugNextLevel() => GoToLevel(LevelNumber + 1);

    [ContextMenu("Отладка: пересоздать этот уровень")]
    void DebugRebuild() => GoToLevel(LevelNumber);

    [ContextMenu("Отладка: дать ключ")]
    void DebugKey() => RunState.AddKey();

    [ContextMenu("Отладка: зачистить все комнаты уровня")]
    void DebugClearAll()
    {
        foreach (Room r in built.Values) r.Clear();
    }

    // ================= Герой и камера =================

    // Отключаем управление героем на время перехода.
    void SetPlayerControl(bool on)
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;

        var abilities = player.GetComponent<PlayerAbilities>();
        if (abilities != null) { abilities.SetControl(on); return; }

        var mover = player.GetComponent<characterController>();
        if (mover != null) mover.enabled = on;
    }

    void PlacePlayer(Vector3 p)
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;
        p.z = 0f;
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
