using UnityEngine;

// Место для двери в префабе комнаты. Пустой объект на полу у стены.
// Точка объекта = низ двери по центру (там, где дверь стоит на полу).
// При входе в комнату RoomManager решает, что здесь будет: вход, выход нужного типа или ничего (просто стена).
// В окне Scene видно, где появится дверь (рамка) и где встанет герой, если вошёл через неё (кружок).
public class DoorSlot : MonoBehaviour
{
    public DoorSide side = DoorSide.Up;
    [Tooltip("На каком расстоянии от двери появляется герой, если вошёл через неё (в клетках, в сторону комнаты)")]
    public float spawnDistance = 1.2f;

    public Vector3 SpawnPoint
    {
        get
        {
            Vector2 p = (Vector2)transform.position + side.Inward() * spawnDistance;
            if (side == DoorSide.Left || side == DoorSide.Right) p += Vector2.up * 0.4f;   // напротив середины боковой двери
            return p;
        }
    }

    // Размер картинки двери в клетках (для рамки в Scene)
    public static Vector2 DoorSize(DoorSide s)
    {
        if (s == DoorSide.Up) return new Vector2(1.5f, 1.5f);
        if (s == DoorSide.Down) return new Vector2(1.5f, 1f);
        return new Vector2(0.75f, 1.5f);
    }

    void OnDrawGizmos()
    {
        Vector2 size = DoorSize(side);
        Vector3 c = transform.position + Vector3.up * size.y * 0.5f;
        Gizmos.color = new Color(1f, 0.8f, 0.2f, 0.9f);
        Gizmos.DrawWireCube(c, new Vector3(size.x, size.y, 0f));
        Gizmos.color = new Color(0.3f, 1f, 0.5f, 0.9f);
        Gizmos.DrawWireSphere(SpawnPoint, 0.25f);
        Gizmos.DrawLine(transform.position, SpawnPoint);
    }
}
