using UnityEngine;

// Один язычок пламени: проигрывает кадры, слегка поднимается вверх, гаснет и удаляется.
// Pivot кадров - внизу по центру (основание пламени).
[RequireComponent(typeof(SpriteRenderer))]
public class FlamePuff : MonoBehaviour
{
    [Tooltip("Кадры пламени по порядку (Violet_Flame_0..5)")]
    public Sprite[] frames;
    [Tooltip("Сколько секунд показывается один кадр")]
    public float frameTime = 0.055f;
    [Tooltip("С какой скоростью язычок поднимается вверх, клеток в секунду")]
    public float riseSpeed = 0.35f;
    [Tooltip("Случайный размер: от X до Y")]
    public Vector2 randomScale = new Vector2(0.8f, 1.15f);

    SpriteRenderer sr;
    Color baseColor;
    float t;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        baseColor = sr.color;
        if (frames != null && frames.Length > 0) sr.sprite = frames[0];
        float s = Random.Range(randomScale.x, randomScale.y);
        transform.localScale = new Vector3(Random.value < 0.5f ? -s : s, s, 1f);   // случайно отражаем
    }

    void Update()
    {
        t += Time.deltaTime;
        int count = (frames != null && frames.Length > 0) ? frames.Length : 1;
        float total = count * frameTime;

        if (frames != null && frames.Length > 0)
            sr.sprite = frames[Mathf.Min(frames.Length - 1, (int)(t / frameTime))];

        transform.position += Vector3.up * riseSpeed * Time.deltaTime;

        // вторая половина жизни - плавно гаснет
        Color c = baseColor;
        c.a = baseColor.a * Mathf.Clamp01(2f * (1f - t / total));
        sr.color = c;

        if (t >= total) Destroy(gameObject);
    }
}
