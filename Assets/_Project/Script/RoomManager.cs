using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable] public class RoomEntry { public RoomType type; public Room prefab; }
[System.Serializable] public class TypeIcon { public RoomType type; public Sprite icon; }

public class RoomManager : MonoBehaviour
{
    public static RoomManager Instance;

    public Room hubPrefab;
    public RoomEntry[] rooms;
    public TypeIcon[] icons;
    [Tooltip("Из чего выбираются двери. Повторы увеличивают шанс (например, Combat два раза)")]
    public RoomType[] doorChoices = { RoomType.Combat, RoomType.Combat, RoomType.Shop, RoomType.Upgrade, RoomType.Item };

    Room current;
    Room lastPrefab;
    bool loading;

    void Awake() { Instance = this; }

    IEnumerator Start()
    {
        yield return null;          // даём PlayerSpawner создать героя
        Load(hubPrefab);
    }

    public void GoToNext(RoomType type)
    {
        if (loading) return;
        Room prefab = PickPrefab(type);
        if (prefab == null) { Debug.LogWarning("Нет префаба комнаты типа " + type); return; }
        Load(prefab);
    }

    Room PickPrefab(RoomType type)
    {
        var list = new List<Room>();
        foreach (var e in rooms) if (e.type == type && e.prefab != null) list.Add(e.prefab);
        if (list.Count > 1) list.Remove(lastPrefab);       // не повторять ту же комнату подряд
        return list.Count == 0 ? null : list[Random.Range(0, list.Count)];
    }

    void Load(Room prefab)
    {
        loading = true;
        if (current != null) Destroy(current.gameObject);

        current = Instantiate(prefab, Vector3.zero, Quaternion.identity);
        lastPrefab = prefab;

        PlacePlayer();
        SetupCamera();
        SetupDoors();
        current.Enter();

        loading = false;
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

    void SetupDoors()
    {
        RoomType[] pool = (current.allowedNext != null && current.allowedNext.Length > 0)
            ? current.allowedNext : doorChoices;
        foreach (var d in current.doors)
        {
            RoomType t = pool[Random.Range(0, pool.Length)];
            d.Setup(t, IconFor(t));
        }
    }

    Sprite IconFor(RoomType t)
    {
        foreach (var i in icons) if (i.type == t) return i.icon;
        return null;
    }
}