using System.Collections;
using UnityEngine;

// ЗАПЕРТАЯ ДВЕРЬ (средний уровень: Door -> LockedDoor -> KeyDoor / BossDoor).
// Не открывается сама, когда комната пройдена. Герой должен подойти вплотную:
// если условие выполнено (CanUnlock) - дверь отпирается, если нет - дёргается.
// Запертая дверь подкрашена своим цветом, чтобы её было видно издалека.
// Триггер двери автоматически чуть увеличивается: стоя у твёрдой двери, герой его касается.
public abstract class LockedDoor : Door
{
    [Header("Запертая дверь")]
    [Tooltip("Подкраска двери, пока она заперта")]
    public Color lockedTint = Color.white;
    [Tooltip("На сколько клеток увеличить триггер, чтобы герой касался его, стоя у двери")]
    public float touchPadding = 0.3f;
    [Tooltip("Пауза после отпирания, прежде чем дверь пустит дальше, сек")]
    public float enterDelay = 0.35f;

    float unlockedAt;
    float lastFail = -10f;
    Vector3 basePos;
    bool baseSaved;

    protected override void Awake()
    {
        base.Awake();
        BoxCollider2D box = GetComponent<BoxCollider2D>();
        if (box != null && box.isTrigger) box.size += Vector2.one * touchPadding;
    }

    // ----- Что решает конкретная дверь -----
    protected abstract bool CanUnlock();      // можно ли отпереть сейчас
    protected virtual void OnUnlock() { }     // что происходит при отпирании (потратить ключ и т.п.)

    protected override void OnSetup() { Tint(lockedTint); }
    protected override void OnSealed() { Tint(Color.white); }

    // Комната пройдена - дверь становится «можно отпереть», но не открывается сама
    public override void OnRoomCleared()
    {
        RoomCleared = true;
    }

    protected override void OnPlayerTouch()
    {
        if (IsSealed) return;
        if (IsOpen)
        {
            if (Time.time - unlockedAt >= enterDelay) base.OnPlayerTouch();
            return;
        }
        if (!RoomCleared) return;                 // во время боя не открыть

        if (!CanUnlock())
        {
            if (Time.time - lastFail > 0.6f) { lastFail = Time.time; StartCoroutine(Wiggle()); }
            return;
        }

        OnUnlock();
        unlockedAt = Time.time;
        Tint(Color.white);
        SetOpen(true);
    }

    void Tint(Color c)
    {
        if (doorRenderer != null) doorRenderer.color = c;
    }

    // «Не получается открыть»: дверь дёргается из стороны в сторону
    IEnumerator Wiggle()
    {
        if (!baseSaved) { basePos = transform.localPosition; baseSaved = true; }
        for (int i = 0; i < 6; i++)
        {
            float dx = (i % 2 == 0 ? 1f : -1f) * 0.04f * (1f - i / 6f);
            transform.localPosition = basePos + new Vector3(dx, 0f, 0f);
            yield return new WaitForSeconds(0.03f);
        }
        transform.localPosition = basePos;
    }
}
