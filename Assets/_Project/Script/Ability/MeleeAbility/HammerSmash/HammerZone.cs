using System.Collections.Generic;
using UnityEngine;

// Зона удара молотом на земле (прицел): дорожка / дуга / круг, фиолетовый край ярче.
// Если у ступени есть пустая часть у героя (Inner Fraction), она не закрашивается.
// Картинку рисует код по цифрам ступени, поэтому при изменении длины или ширины
// перерисовывать ничего не нужно. На сцену ставить не нужно - объект создаётся сам.
[RequireComponent(typeof(SpriteRenderer))]
public class HammerZone : MonoBehaviour
{
    const float PixelsPerUnit = 48f;
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

    // center - откуда идёт волна, dir - куда, alpha - общая прозрачность (бледнее на перезарядке)
    public void Show(Vector2 center, Vector2 dir, HammerStage st, Color fill, Color edge, Color outline, float alpha = 1f)
    {
        if (sr == null) sr = GetComponent<SpriteRenderer>();
        sr.sprite = GetSprite(st, fill, edge, outline);
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
        t += Time.unscaledDeltaTime;
        Color c = sr.color;
        c.a = 1f - Mathf.Clamp01(t / fadeTime);
        sr.color = c;
        if (t >= fadeTime) Destroy(gameObject);
    }

    // ---------- Рисование картинки зоны ----------

    static Sprite GetSprite(HammerStage st, Color fill, Color edge, Color outline)
    {
        string key = $"{st.shape}_{st.length:0.00}_{st.width:0.00}_{st.baseWidth:0.00}_{st.innerFraction:0.00}_{st.edgeFraction:0.00}_{fill}_{edge}_{outline}";
        if (cache.TryGetValue(key, out Sprite s) && s != null) return s;
        s = Build(st, fill, edge, outline);
        cache[key] = s;
        return s;
    }

    // Зона смотрит вправо (+X), центр картинки = точка, откуда идёт волна
    static Sprite Build(HammerStage st, Color fill, Color edge, Color outline)
    {
        float reach = HammerCombo.MaxReach(st);
        int size = Mathf.CeilToInt(reach * 2f * PixelsPerUnit) + 4;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;

        float px = 1f / PixelsPerUnit;              // размер пикселя в клетках
        float edgeStart = 1f - st.edgeFraction;
        // во сколько клеток переводится 1 единица «пути» (для толщины линий)
        float pathScale = st.shape == HammerShape.HalfEllipse ? Mathf.Min(st.length, st.width * 0.5f) : st.length;
        var pixels = new Color32[size * size];
        Color divider = outline; divider.a *= 0.6f;

        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            Vector2 p = new Vector2((x + 0.5f - size * 0.5f) * px, (y + 0.5f - size * 0.5f) * px);
            float u = HammerCombo.PathPosition(st, p);

            // насколько пиксель внутри зоны, в пикселях (для контура и сглаживания)
            float inside;
            if (st.shape == HammerShape.Lane)
                inside = Mathf.Min(st.length - p.x, p.x, st.width * 0.5f - Mathf.Abs(p.y)) / px;
            else if (st.shape == HammerShape.HalfEllipse)
                inside = Mathf.Min((1f - u) * pathScale, p.x) / px;
            else if (st.shape == HammerShape.Cone)
                inside = HammerCombo.ConeDistance(st, p) / px;
            else
                inside = (1f - u) * pathScale / px;
            if (st.innerFraction > 0f)                                   // пустая часть у героя
                inside = Mathf.Min(inside, (u - st.innerFraction) * pathScale / px);

            if (float.IsInfinity(u) || inside <= -0.5f) { pixels[y * size + x] = new Color32(0, 0, 0, 0); continue; }

            Color c = u >= edgeStart ? edge : fill;
            if (inside < 2.5f) c = outline;                                              // контур зоны
            else if (Mathf.Abs(u - edgeStart) * pathScale < 1.2f * px) c = divider;      // начало края
            c.a *= Mathf.Clamp01(inside + 0.5f);                                         // сглаживание
            pixels[y * size + x] = c;
        }

        tex.SetPixels32(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), PixelsPerUnit);
    }
}
