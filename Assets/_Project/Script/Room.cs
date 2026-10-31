using UnityEngine;
using UnityEngine.Tilemaps;

public class Room : MonoBehaviour
{
    public RoomType type = RoomType.Combat;
    public Tilemap floor;               // тайлмап пола (для границ камеры)
    public Transform playerSpawn;
    public Door[] doors;
    [Tooltip("Куда могут вести двери. Если пусто, берётся общий список менеджера")]
    public RoomType[] allowedNext;
    [Tooltip("Хаб, магазин и т.д.: двери открыты сразу")]
    public bool clearOnEnter;

    public bool IsCleared { get; private set; }
    int enemiesAlive;

    public void Enter()
    {
        IsCleared = false;
        if (clearOnEnter) Clear();
    }

    // понадобится, когда появятся враги
    public void RegisterEnemy() { enemiesAlive++; }
    public void EnemyDied()
    {
        enemiesAlive--;
        if (enemiesAlive <= 0) Clear();
    }

    public void Clear()
    {
        IsCleared = true;
        foreach (var d in doors) d.SetOpen(true);
    }

#if UNITY_EDITOR
    // для теста, пока нет врагов: клавиша K «убивает» всех
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.K)) Clear();
    }
#endif
}