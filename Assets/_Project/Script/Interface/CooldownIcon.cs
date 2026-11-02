using UnityEngine;
using UnityEngine.UI;

// Общая иконка умения с перезарядкой. Сама по себе не используется - от неё наследуются
// иконки конкретных умений (HammerIcon). Наследник говорит только «есть ли умение» и «сколько осталось отката».
//
// Как устроено: снизу серая картинка, сверху цветная с Image Type = Filled, Radial 360.
// Во время отката цветная часть заполняется по часовой стрелке от 12 часов.
// Когда откат закончился - короткая белая вспышка и «толчок» размера.
// Анимации идут по реальному времени, поэтому стоп-кадр удара (HitStop) их не замораживает.
public abstract class CooldownIcon : MonoBehaviour
{
    [Header("Картинки иконки")]
    [Tooltip("Цветная картинка умения. Image Type = Filled, Fill Method = Radial 360, Origin = Top, Clockwise")]
    public Image colorImage;
    [Tooltip("Серая картинка под цветной (видна, пока идёт откат)")]
    public Image greyImage;
    [Tooltip("Белый силуэт для вспышки готовности (можно не ставить)")]
    public Image flashImage;
    [Tooltip("Что прятать, когда умения нет (обычно сам этот объект с CanvasGroup). Пусто = этот объект")]
    public CanvasGroup group;

    [Header("Вспышка готовности")]
    [Tooltip("Сколько длится вспышка, сек")]
    public float flashTime = 0.3f;
    [Tooltip("Насколько иконка увеличивается в момент готовности")]
    public float punchScale = 1.15f;

    float flashT = 999f;
    bool wasReady = true;
    Vector3 baseScale;

    // ----- Это решает наследник -----
    // Есть ли сейчас умение (нет героя или у героя нет этого умения - иконка спрячется)
    protected abstract bool HasAbility { get; }
    // Доля оставшегося отката: 1 = только начался, 0 = готово
    protected abstract float CooldownFraction { get; }
    // Дополнительные детали иконки (цифры, точки и т.д.)
    protected virtual void UpdateExtras() { }
    // Найти источник данных (героя), если ещё не найден
    protected virtual void FindSource() { }

    protected virtual void Awake()
    {
        baseScale = transform.localScale;
        if (group == null) group = GetComponent<CanvasGroup>();
        if (group == null) group = gameObject.AddComponent<CanvasGroup>();
        if (flashImage != null) SetAlpha(flashImage, 0f);
    }

    protected virtual void Update()
    {
        FindSource();
        bool has = HasAbility;
        group.alpha = has ? 1f : 0f;
        if (!has) return;

        float left = CooldownFraction;
        bool ready = left <= 0f;
        if (colorImage != null) colorImage.fillAmount = 1f - left;   // заполняется по мере отката
        if (greyImage != null) greyImage.enabled = !ready;

        if (ready && !wasReady) flashT = 0f;    // только что стало готово
        wasReady = ready;
        UpdateFlash();
        UpdateExtras();
    }

    void UpdateFlash()
    {
        flashT += Time.unscaledDeltaTime;
        float k = Mathf.Clamp01(flashT / Mathf.Max(0.01f, flashTime));   // 0 -> 1 за время вспышки
        float fade = 1f - k;
        if (flashImage != null) SetAlpha(flashImage, fade * 0.9f);
        transform.localScale = baseScale * Mathf.Lerp(1f, punchScale, fade * fade);
    }

    protected static void SetAlpha(Graphic g, float a)
    {
        Color c = g.color; c.a = a; g.color = c;
    }
}
