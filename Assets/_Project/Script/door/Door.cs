using System.Collections;
using UnityEngine;

// ОБЫЧНАЯ ДВЕРЬ-ВЫХОД (верх дерева: Door -> LockedDoor -> KeyDoor / BossDoor;  Door -> EntranceDoor).
// Дверь не стена: она стоит на полу у стены, не мешает ходить и не имеет твёрдых коллизий.
// Создаётся сама (RoomManager) в месте DoorSlot при входе в комнату - на сцену и в префабы её ставить не нужно.
// Пока комната не пройдена - закрыта. После зачистки проигрывает анимацию открытия.
// Открытая: герой подходит, над дверью появляется значок клавиши (F) - нажал, переход в комнату
// того типа, что нарисован на створке. Просто зайти в дверь недостаточно.
// Потомки меняют: какие кадры показывать (GetFrames), что делать при зачистке (OnRoomCleared)
// и что происходит по клавише (CanInteract / Interact).
[RequireComponent(typeof(SpriteRenderer))]
public class Door : MonoBehaviour, IInteractable
{
    public RoomType NextType { get; private set; }
    public DoorSide Side { get; private set; }
    public bool IsOpen { get; private set; }          // открыта полностью (анимация закончилась)
    protected bool IsOpening { get; private set; }
    protected bool RoomCleared { get; set; }
    protected DoorStyle Style { get; private set; }
    protected SpriteRenderer Renderer { get; private set; }
    protected Sprite[] Frames { get; private set; }
    protected BoxCollider2D Trigger { get; private set; }
    bool entering;                                     // уже нажали F, идёт переход

    // Вызывает RoomManager сразу после создания
    public void Init(DoorSide side, RoomType nextType, DoorStyle style)
    {
        Side = side;
        NextType = nextType;
        Style = style;

        Renderer = GetComponent<SpriteRenderer>();
        if (Renderer == null) Renderer = gameObject.AddComponent<SpriteRenderer>();
        Renderer.flipX = side == DoorSide.Left;                    // левая - зеркальная правая
        Renderer.sortingOrder = side == DoorSide.Down ? style.frontSortingOrder : style.backSortingOrder;
        if (style.material != null) Renderer.sharedMaterial = style.material;

        Frames = GetFrames();
        if (Frames == null || Frames.Length == 0)
            Debug.LogWarning($"Door Style: нет кадров для {GetType().Name} ({nextType}, {side})");
        SetFrame(0);

        SetupTrigger();
        OnInit();
    }

    // ----- Что решают потомки -----
    protected virtual Sprite[] GetFrames()
    {
        DoorFrames f = Style != null ? Style.For(NextType) : null;
        return f != null ? f.Get(Side) : null;
    }
    protected virtual void OnInit() { }

    // Комната пройдена: обычная дверь открывается сама
    public virtual void OnRoomCleared()
    {
        RoomCleared = true;
        Open();
    }

    // ----- Взаимодействие (клавиша F) -----

    // Обычная дверь: можно войти, когда она открыта и не идёт другой переход
    public virtual bool CanInteract(GameObject player)
    {
        return IsOpen && !entering && RoomManager.Instance != null && !RoomManager.Instance.Busy;
    }

    public virtual void Interact(GameObject player)
    {
        if (CanInteract(player)) EnterNext();
    }

    // Переход в следующую комнату
    protected void EnterNext()
    {
        entering = RoomManager.Instance != null && RoomManager.Instance.GoToNext(NextType, Side);
    }

    // Где показать значок клавиши: над дверью
    public virtual Vector3 PromptPosition =>
        transform.position + Vector3.up * (Side == DoorSide.Down ? 1.2f : 1.75f);

    // ----- Анимация -----

    public void Open()
    {
        if (IsOpen || IsOpening) return;
        StartCoroutine(OpenRoutine());
    }

    IEnumerator OpenRoutine()
    {
        IsOpening = true;
        yield return PlayFrames(true);
        IsOpening = false;
        IsOpen = true;
    }

    // Проиграть кадры: вперёд (открытие) или назад (захлопывание)
    protected IEnumerator PlayFrames(bool forward)
    {
        int n = Frames != null ? Frames.Length : 0;
        for (int i = 0; i < n; i++)
        {
            SetFrame(forward ? i : n - 1 - i);
            yield return new WaitForSeconds(Style.frameTime);
        }
    }

    protected void SetFrame(int i)
    {
        if (Renderer == null || Frames == null || Frames.Length == 0) return;
        Renderer.sprite = Frames[Mathf.Clamp(i, 0, Frames.Length - 1)];
    }

    protected int LastFrame => Frames != null && Frames.Length > 0 ? Frames.Length - 1 : 0;

    // ----- Зона взаимодействия у основания двери (триггер, не твёрдая) -----
    // По ней PlayerInteractor находит дверь рядом с героем.

    void SetupTrigger()
    {
        Trigger = GetComponent<BoxCollider2D>();
        if (Trigger == null) Trigger = gameObject.AddComponent<BoxCollider2D>();
        Trigger.isTrigger = true;
        switch (Side)
        {
            case DoorSide.Up:
                Trigger.size = Style.upTriggerSize;
                Trigger.offset = new Vector2(0f, Style.upTriggerSize.y * 0.5f);
                break;
            case DoorSide.Down:
                Trigger.size = Style.downTriggerSize;
                Trigger.offset = new Vector2(0f, Style.downTriggerSize.y * 0.5f);
                break;
            default:
                Trigger.size = Style.sideTriggerSize;
                float x = Side == DoorSide.Right ? -0.1f : 0.1f;     // чуть в сторону комнаты
                Trigger.offset = new Vector2(x, Style.sideTriggerSize.y * 0.5f);
                break;
        }
    }
}
