using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable] public class RoomEntry { public RoomType type; public Room prefab; }
[System.Serializable] public class TypeIcon { public RoomType type; public Sprite icon; }

// Загрузка комнат и переходы между ними.
// Живёт всегда одна комната: при переходе старая удаляется, новая создаётся из префаба нужного типа.
// Забег: хаб -> обычные комнаты -> после Rooms Before Boss комнат появляется дверь к боссу -> после босса в хаб.
// Что делать при входе, когда открывать двери и т.п. - решает сама комната (Room и её потомки).
public class RoomManager : MonoBehaviour
{
    public static RoomManager Instance;

    public Room hubPrefab;
    public RoomEntry[] rooms;
    public TypeIcon[] icons;
    [Tooltip("Из чего выбираются обычные двери. Повторы увеличивают шанс")]
    public RoomType[] doorChoices = { RoomType.Combat, RoomType.Combat, RoomType.Shop, RoomType.Upgrade, RoomType.Item };
    [Tooltip("Затемнение экрана при переходе (необязательно)")]
    public ScreenFader fader;

    [Header("Забег")]
    [Tooltip("После скольких комнат (не считая хаба) появляется дверь к боссу")]
    public int roomsBeforeBoss = 8;
    [Tooltip("Шанс, что дверь на ключ (KeyDoor) в комнате будет активна (0..1)")]
    [Range(0f, 1f)] public float keyDoorChance = 0.35f;

    Room current;
    Room lastPrefab;
    bool busy;

    public Room Current => current;

    void Awake() { Instance = this; }

    IEnumerator Start()
    {
        busy = true;
        yield return null;                         // даём PlayerSpawner создать героя
        SetPlayerControl(false);
        Load(hubPrefab);
        yield return null;
        if (fader != null) yield return fader.FadeIn();
        SetPlayerControl(true);
        busy = false;
    }

    public void GoToNext(RoomType type)
    {
        if (busy) return;
        Room prefab = PickPrefab(type);
        if (prefab == null) { Debug.LogWarning("Нет префаба комнаты типа " + type); return; }
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
        if (current != null)
        {
            current.PrepareUnload();               // враги и награды удалятся вместе с комнатой - это не «смерть»
            Destroy(current.gameObject);
        }
        current = Instantiate(prefab, Vector3.zero, Quaternion.identity);
        lastPrefab = prefab;

        current.OnLoaded();                        // хаб сбрасывает забег, остальные комнаты засчитываются
        PlacePlayer();
        SetupCamera();
        SetupDoors();
        current.Enter();
    }

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

    void PlacePlayer()
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player == null || current.playerSpawn == null) return;
        Vector2 p = current.playerSpawn.position;
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

    // Каждая дверь сама решает, куда вести (обычная, на ключ, к боссу) - по общему плану
    void SetupDoors()
    {
        bool hasBossDoor = false;
        foreach (Door d in current.doors)
            if (d is BossDoor) hasBossDoor = true;

        var plan = new DoorPlan
        {
            pool = current.NextChoices(doorChoices),
            bossDue = current.AllowsBossDoor && RunState.RoomsPassed >= roomsBeforeBoss,
            roomHasBossDoor = hasBossDoor,
            keyDoorChance = keyDoorChance,
            iconFor = IconFor
        };

        foreach (Door d in current.doors)
            if (d != null) d.Configure(plan);
    }

    Sprite IconFor(RoomType t)
    {
        foreach (var i in icons) if (i.type == t) return i.icon;
        return null;
    }
}
