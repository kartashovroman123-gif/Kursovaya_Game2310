using UnityEngine;

// Короткий эффект из кадров: проигрывает кадры, может подниматься вверх, гаснет и удаляется.
// Используется для пламени рывка, извержений и трещин молота, пыли, огня.
// Pivot кадров - внизу по центру для пламени и в центре для трещин/вспышек.
[RequireComponent(typeof(SpriteRenderer))]
public class FlamePuff : MonoBehaviour
{
    [Tooltip("Кадры по порядку")]
    public Sprite[] frames;
    [Tooltip("Сколько секунд показывается один кадр")]
    public float frameTime = 0.055f;
    [Tooltip("С какой скоростью эффект поднимается вверх, клеток в секунду (0 = стоит на месте)")]
    public float riseSpeed = 0.35f;
    [Tooltip("Случайный размер: от X до Y")]
    public Vector2 randomScale = new Vector2(0.8f, 1.15f);
    [Tooltip("Случайно отражать по горизонтали")]
    public bool randomFlip = true;

    [Header("Одна случайная картинка вместо анимации (для трещин)")]
    [Tooltip("Взять один случайный кадр из Frames и держать его Lifetime секунд")]
    public bool randomSingleFrame = false;
    [Tooltip("Сколько живёт эффект в режиме одной картинки, сек")]
    public float lifetime = 0.7f;

    SpriteRenderer sr;
    Color baseColor;
    float t;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        baseColor = sr.color;
        if (frames != null && frames.Length > 0)
            sr.sprite = randomSingleFrame ? frames[Random.Range(0, frames.Length)] : frames[0];
        float s = Random.Range(randomScale.x, randomScale.y);
        bool flip = randomFlip && Random.value < 0.5f;
        transform.localScale = new Vector3(flip ? -s : s, s, 1f);
    }

    void Update()
    {
        t += Time.deltaTime;
        bool animated = !randomSingleFrame && frames != null && frames.Length > 0;
        float total = animated ? frames.Length * frameTime : Mathf.Max(0.01f, lifetime);

        if (animated)
            sr.sprite = frames[Mathf.Min(frames.Length - 1, (int)(t / frameTime))];

        if (riseSpeed != 0f) transform.position += Vector3.up * riseSpeed * Time.deltaTime;

        // вторая половина жизни - плавно гаснет
        Color c = baseColor;
        c.a = baseColor.a * Mathf.Clamp01(2f * (1f - t / total));
        sr.color = c;

        if (t >= total) Destroy(gameObject);
    }
}
