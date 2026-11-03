using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable] public class RoomEntry { public RoomType type; public Room prefab; }

// Загрузка комнат, двери и переходы.
// Живёт всегда одна комната: при переходе старая удаляется, новая создаётся из префаба нужного типа.
// Двери создаются здесь в местах DoorSlot новой комнаты:
//   вход - на стороне, противоположной выходу из прошлой комнаты (захлопывается за героем);
//   выходы - случайные 1-3 из остальных мест, тип каждого случайный (его видно по створке);
//   дверь на ключ - иногда, только если у героя есть ключ (тогда выходов минимум 2);
//   когда пройдено Rooms Before Boss комнат - единственный выход, к боссу.
// Забег: хаб -> комнаты -> босс -> хаб.
public class RoomManager : MonoBehaviour
{
    public static RoomManager Instance;

    public Room hubPrefab;
    public RoomEntry[] rooms;
    [Tooltip("Из чего выбираются типы выходов. Повторы увеличивают шанс")]
    public RoomType[] doorChoices = { RoomType.Combat, RoomType.Combat, RoomType.Shop, RoomType.Upgrade, RoomType.Item };
    [Tooltip("Затемнение экрана при переходе (необязательно)")]
    public ScreenFader fader;

    [Header("Двери")]
    [Tooltip("Внешний вид всех дверей (ассет Door Style)")]
    public DoorStyle doorStyle;

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

    // Вызывает открытая дверь, когда герой в неё зашёл
    public void GoToNext(RoomType type, DoorSide exitSide)
    {
        if (busy) return;
        Room prefab = PickPrefab(type);
        if (prefab == null) { Debug.LogWarning("Нет префаба комнаты типа " + type); return; }
        lastExit = exitSide;
        StartCoroutine(Transition(prefab));
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

        RoomType[] pool = current.NextChoices(doorChoices);

        // 2. Комната с одним заданным выходом (босс: выход сверху)
        if (current.ForcedExit.HasValue)
        {
            DoorSlot exit = current.FindSlot(current.ForcedExit.Value);
            if (exit == null || exit == entranceSlot) exit = free.Count > 0 ? free[0] : null;
            if (exit != null) AddDoor<Door>(exit, RandomType(pool));
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

        // 4. Обычные выходы: 1-3, иногда один из них - дверь на ключ (тогда выходов минимум 2)
        int min = Mathf.Clamp(current.minExits, 1, 3);
        int max = Mathf.Clamp(current.maxExits, min, 3);
        int count = Random.Range(min, max + 1);
        bool keyDoor = RunState.Keys > 0 && free.Count >= 2 && Random.value < keyDoorChance;
        if (keyDoor) count = Mathf.Max(count, 2);
        count = Mathf.Min(count, free.Count);

        for (int i = 0; i < count; i++)
        {
            if (keyDoor && i == 0) AddDoor<KeyDoor>(free[i], RoomType.Vault);
            else AddDoor<Door>(free[i], RandomType(pool));
        }
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
