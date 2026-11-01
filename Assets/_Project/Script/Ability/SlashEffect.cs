using UnityEngine;

// Вспышка удара: проигрывает кадры взмаха и сама удаляется.
// Кадры нарисованы для удара ВНИЗ-ВПРАВО (дуга от правой стороны через угол к низу),
// pivot - в центре картинки (это точка, откуда бьёт герой).
// Остальные 3 угла и «обратный» взмах получаются отражением, перерисовывать не нужно.
[RequireComponent(typeof(SpriteRenderer))]
public class SlashEffect : MonoBehaviour
{
    [Tooltip("Кадры взмаха по порядку. Пусто - используется спрайт из SpriteRenderer")]
    public Sprite[] frames;
    [Tooltip("Сколько секунд показывается один кадр")]
    public float frameTime = 0.04f;
    [Tooltip("Радиус внешнего когтя на картинке, в клетках (для Claw_Slash_0..3 = 1)")]
    public float spriteRadius = 1f;
    [Tooltip("Order in Layer, когда удар вверх и вспышка должна быть за героем (у героя 5)")]
    public int behindOrder = 4;

    SpriteRenderer sr;
    Color baseColor;
    float baseScale = 1f;
    float ySign = 1f;
    float t;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        baseColor = sr.color;
        if (frames != null && frames.Length > 0) sr.sprite = frames[0];
    }

    // range   - на какое расстояние бьёт приём, в клетках
    // sideX   - 1 вправо, -1 влево;  up - удар вверх
    // reverse - взмах в обратную сторону (чередуется в серии)
    // size    - множитель размера (у последнего удара серии больше)
    // behind  - нарисовать за героем (удар вверх)
    public void Play(float range, int sideX, bool up, bool reverse, float size = 1f, bool behind = false)
    {
        // Матрица отражения: Q - выбор угла, D - отражение по диагонали (обратный взмах)
        float qx = sideX >= 0 ? 1f : -1f;
        float qy = up ? -1f : 1f;
        float m00 = qx, m01 = 0f, m10 = 0f, m11 = qy;
        if (reverse) { m00 = 0f; m01 = -qx; m10 = -qy; m11 = 0f; }

        // Любая такая матрица = поворот * (отражение по Y или нет)
        float angle = Mathf.Atan2(m10, m00) * Mathf.Rad2Deg;
        ySign = (m00 * m11 - m01 * m10) < 0f ? -1f : 1f;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);

        baseScale = range / Mathf.Max(0.01f, spriteRadius) * size;
        transform.localScale = new Vector3(baseScale, baseScale * ySign, 1f);

        if (behind) sr.sortingOrder = behindOrder;
    }

    void Update()
    {
        t += Time.deltaTime;
        int count = (frames != null && frames.Length > 0) ? frames.Length : 1;
        float total = count * frameTime;

        if (frames != null && frames.Length > 0)
            sr.sprite = frames[Mathf.Min(frames.Length - 1, (int)(t / frameTime))];

        // последний кадр плавно гаснет
        float fadeStart = total - frameTime;
        Color c = baseColor;
        c.a = t < fadeStart ? 1f : 1f - Mathf.Clamp01((t - fadeStart) / frameTime);
        sr.color = c;

        if (t >= total) Destroy(gameObject);
    }
}
