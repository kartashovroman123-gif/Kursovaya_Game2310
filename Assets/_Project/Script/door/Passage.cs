using System.Collections;
using UnityEngine;

// ПРОХОД В СОСЕДНЮЮ КОМНАТУ (RoomExit -> Passage). Создаётся RoomManager'ом сам в месте DoorSlot,
// если по схеме уровня с этой стороны есть сосед. Кнопку нажимать не нужно: герой зашёл в зону - переход.
// Пока боевая комната не зачищена - проход перекрыт решёткой (Bars из DoorSlot):
// при входе в бой решётка падает сверху, после зачистки поднимается и исчезает.
public class Passage : RoomExit
{
    const float AnimTime = 0.18f;
    const float Lift = 0.8f;          // на сколько клеток решётка поднята в начале падения

    GameObject bars;
    Vector3 barsBase;
    SpriteRenderer[] barsSprites;
    Coroutine anim;

    public override void Init(Room owner, DoorSlot slot)
    {
        base.Init(owner, slot);

        // зона перехода - уходит в стену за проёмом
        transform.position = (Vector2)slot.transform.position + slot.Outward * slot.triggerDepth * 0.5f;
        var zone = gameObject.AddComponent<BoxCollider2D>();
        zone.isTrigger = true;
        zone.size = slot.BoxSize(slot.triggerDepth);

        // решётка: своя из префаба или невидимая преграда
        bars = slot.bars != null ? slot.bars : slot.CreateInvisibleWall("AutoBars", 0.4f);
        barsBase = bars.transform.localPosition;
        barsSprites = bars.GetComponentsInChildren<SpriteRenderer>(true);
        bars.SetActive(false);
    }

    protected override void OnLockChanged(bool locked, bool animate)
    {
        if (anim != null) StopCoroutine(anim);
        anim = null;
        if (animate && isActiveAndEnabled) anim = StartCoroutine(locked ? Drop() : Raise());
        else
        {
            bars.transform.localPosition = barsBase;
            SetAlpha(1f);
            bars.SetActive(locked);
        }
    }

    // Решётка падает сверху и с лязгом встаёт на место
    IEnumerator Drop()
    {
        bars.SetActive(true);
        for (float t = 0f; t < AnimTime; t += Time.deltaTime)
        {
            float k = t / AnimTime;
            bars.transform.localPosition = barsBase + Vector3.up * Lift * (1f - k * k);
            SetAlpha(Mathf.Clamp01(k * 2f));
            yield return null;
        }
        bars.transform.localPosition = barsBase;
        SetAlpha(1f);
        CameraShake.Kick(Vector2.down * 0.06f);
        anim = null;
    }

    // Решётка поднимается и растворяется
    IEnumerator Raise()
    {
        for (float t = 0f; t < AnimTime * 1.5f; t += Time.deltaTime)
        {
            float k = t / (AnimTime * 1.5f);
            bars.transform.localPosition = barsBase + Vector3.up * Lift * k * k;
            SetAlpha(1f - k);
            yield return null;
        }
        bars.transform.localPosition = barsBase;
        SetAlpha(1f);
        bars.SetActive(false);
        anim = null;
    }

    void SetAlpha(float a)
    {
        foreach (SpriteRenderer r in barsSprites)
        {
            if (r == null) continue;
            Color c = r.color; c.a = a; r.color = c;
        }
    }

    // ----- Переход -----
    void OnTriggerEnter2D(Collider2D other) => TryPass(other);
    void OnTriggerStay2D(Collider2D other) => TryPass(other);

    void TryPass(Collider2D other)
    {
        if (IsLocked || !other.CompareTag("Player")) return;
        RoomManager rm = RoomManager.Instance;
        if (rm != null && !rm.Busy && rm.Current == Owner) rm.GoThrough(this);
    }
}
