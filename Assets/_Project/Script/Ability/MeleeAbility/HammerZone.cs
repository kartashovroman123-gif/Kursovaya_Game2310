using System.Collections.Generic;
using UnityEngine;

// Зона удара молотом на земле: сектор/полукруг/круг с ярким краем.
// Картинку рисует код по цифрам из ассета, поэтому при изменении радиуса или угла
// перерисовывать ничего не нужно. Используется для прицела и для вспышки при ударе.
// На сцену ставить не нужно - объекты создаются сами.
[RequireComponent(typeof(SpriteRenderer))]
public class HammerZone : MonoBehaviour
{
    const float PixelsPerUnit = 64f;
    static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();

    SpriteRenderer sr;
    float fadeTime, t;
    bool fading;

    public static HammerZone Create(int sortingOrder)
    {
        var go = new GameObject("HammerZone");
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sortingOrder = sortingOrder;
        return go.AddComponent<HammerZone>();
    }

    void Awake() { sr = GetComponent<SpriteRenderer>(); }

    // center - центр зоны, dir - куда смотрит зона, alpha - общая прозрачность (бледнее на перезарядке)
    public void Show(Vector2 center, Vector2 dir, float radius, float arcAngle, float edgeWidth,
                     Color fill, Color edge, Color outline, float alpha = 1f)
    {
        if (sr == null) sr = GetComponent<SpriteRenderer>();
        sr.sprite = GetSprite(radius, arcAngle, edgeWidth, fill, edge, outline);
        sr.color = new Color(1f, 1f, 1f, alpha);
        transform.position = new Vector3(center.x, center.y, 0f);
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
        gameObject.SetActive(true);
    }

    public void Hide() { gameObject.SetActive(false); }

    public void FadeAndDestroy(float time)
    {
        fading = true;
        fadeTime = Mathf.Max(0.01f, time);
        t = 0f;
    }

    void Update()
    {
        if (!fading) return;
        t += Time.unscaledDeltaTime;               // гаснет и во время стоп-кадра
        Color c = sr.color;
        c.a = 1f - Mathf.Clamp01(t / fadeTime);
        sr.color = c;
        if (t >= fadeTime) Destroy(gameObject);
    }

    // ---------- Рисование картинки зоны ----------

    static Sprite GetSprite(float radius, float arcAngle, float edgeWidth, Color fill, Color edge, Color outline)
    {
        string key = $"{radius:0.00}_{arcAngle:0}_{edgeWidth:0.00}_{fill}_{edge}_{outline}";
        if (cache.TryGetValue(key, out Sprite s) && s != null) return s;
        s = Build(radius, arcAngle, edgeWidth, fill, edge, outline);
        cache[key] = s;
        return s;
    }

    // Зона смотрит вправо (+X), центр картинки = центр удара
    static Sprite Build(float radius, float arcAngle, float edgeWidth, Color fill, Color edge, Color outline)
    {
        int size = Mathf.CeilToInt(radius * 2f * PixelsPerUnit) + 4;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;

        bool full = arcAngle >= 359f;
        float half = arcAngle * 0.5f * Mathf.Deg2Rad;
        float inner = Mathf.Max(0f, radius - edgeWidth);
        float px = 1f / PixelsPerUnit;              // размер пикселя в клетках
        var pixels = new Color32[size * size];

        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float ux = (x + 0.5f - size * 0.5f) * px;
            float uy = (y + 0.5f - size * 0.5f) * px;
            float d = Mathf.Sqrt(ux * ux + uy * uy);

            // насколько пиксель внутри: по радиусу и по боковым сторонам сектора (в пикселях)
            float inR = (radius - d) / px;
            float inSide = float.MaxValue;
            if (!full)
            {
                float a = Mathf.Abs(Mathf.Atan2(uy, ux));
                inSide = (half - a) * d / px;
                if (a > half && d > px) inSide = -1f;
            }
            float inside = Mathf.Min(inR, inSide);
            if (inside <= -0.5f) { pixels[y * size + x] = new Color32(0, 0, 0, 0); continue; }

            Color c = d >= inner ? edge : fill;
            if (inside < 2.5f) c = outline;                                 // контур зоны
            else if (Mathf.Abs(d - inner) < 1.2f * px) c = outline * new Color(1f, 1f, 1f, 0.6f);   // граница края
            c.a *= Mathf.Clamp01(inside + 0.5f);                            // сглаживание
            pixels[y * size + x] = c;
        }

        tex.SetPixels32(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), PixelsPerUnit);
    }
}
