using UnityEngine;

// Что RoomManager сообщает дверям при загрузке комнаты (каждая дверь решает сама, что с этим делать)
public class DoorPlan
{
    public RoomType[] pool;              // из каких типов выбирать обычную дверь
    public bool bossDue;                 // пора к боссу
    public bool roomHasBossDoor;         // в комнате есть отдельная дверь босса
    public float keyDoorChance;          // шанс, что дверь на ключ будет активна
    public System.Func<RoomType, Sprite> iconFor;

    public RoomType RandomType() =>
        pool != null && pool.Length > 0 ? pool[Random.Range(0, pool.Length)] : RoomType.Combat;
    public Sprite IconFor(RoomType t) => iconFor != null ? iconFor(t) : null;
}

// ОБЫЧНАЯ ДВЕРЬ (верх дерева: Door -> LockedDoor -> KeyDoor / BossDoor).
// Закрыта, пока комната не пройдена, потом открывается сама. Вошёл в открытую дверь - переход.
// Над дверью иконка: куда она ведёт. Неиспользуемая дверь «запечатана»: закрыта навсегда, без иконки.
// Потомки меняют: когда дверь активна (Configure) и как она открывается (OnRoomCleared, OnPlayerTouch).
public class Door : MonoBehaviour
{
    public SpriteRenderer doorRenderer;
    [Tooltip("Если пусто - берётся спрайт, который стоит на двери в префабе")]
    public Sprite closedSprite;
    public Sprite openSprite;
    public SpriteRenderer iconRenderer;   // дочерний Icon
    public Collider2D blocker;            // дочерний Blocker (твёрдый)

    public RoomType NextType { get; private set; }
    public bool IsOpen { get; private set; }
    public bool IsSealed { get; private set; }
    protected bool RoomCleared { get; set; }

    protected virtual void Awake()
    {
        if (doorRenderer == null) doorRenderer = GetComponent<SpriteRenderer>();
        if (closedSprite == null && doorRenderer != null) closedSprite = doorRenderer.sprite;
    }

    // ---------- Настройка при загрузке комнаты ----------

    // Обычная дверь: ведёт в случайную комнату. Когда пора к боссу - к боссу,
    // а если у комнаты есть отдельная дверь босса - эта запечатывается
    public virtual void Configure(DoorPlan plan)
    {
        if (plan.bossDue)
        {
            if (plan.roomHasBossDoor) Seal();
            else Setup(RoomType.Boss, plan.IconFor(RoomType.Boss));
            return;
        }
        RoomType t = plan.RandomType();
        Setup(t, plan.IconFor(t));
    }

    public void Setup(RoomType type, Sprite icon)
    {
        IsSealed = false;
        RoomCleared = false;
        NextType = type;
        SetIcon(icon);
        SetOpen(false);
        OnSetup();
    }

    // Дверь не используется: закрыта навсегда, без иконки
    public void Seal()
    {
        IsSealed = true;
        RoomCleared = false;
        SetIcon(null);
        SetOpen(false);
        OnSealed();
    }

    protected virtual void OnSetup() { }
    protected virtual void OnSealed() { }

    // ---------- Во время игры ----------

    // Комната пройдена. Обычная дверь открывается сразу
    public virtual void OnRoomCleared()
    {
        RoomCleared = true;
        if (!IsSealed) SetOpen(true);
    }

    // Герой касается двери. Обычная: если открыта - переход
    protected virtual void OnPlayerTouch()
    {
        if (IsOpen && !IsSealed && RoomManager.Instance != null)
            RoomManager.Instance.GoToNext(NextType);
    }

    public void SetOpen(bool open)
    {
        IsOpen = open;
        if (blocker != null) blocker.enabled = !open;
        if (doorRenderer != null)
        {
            if (open && openSprite != null) doorRenderer.sprite = openSprite;
            else if (!open && closedSprite != null) doorRenderer.sprite = closedSprite;
        }
    }

    void SetIcon(Sprite icon)
    {
        if (iconRenderer == null) return;
        iconRenderer.sprite = icon;
        iconRenderer.enabled = icon != null;
    }

    void OnTriggerEnter2D(Collider2D other) { if (other.CompareTag("Player")) OnPlayerTouch(); }
    void OnTriggerStay2D(Collider2D other) { if (other.CompareTag("Player")) OnPlayerTouch(); }
}
