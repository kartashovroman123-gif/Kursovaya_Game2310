using System.Collections.Generic;
using UnityEngine;

// Рисует прицел метательного оружия картинками (точки, черты, остриё). Создаётся PlayerThrower сам.
// Каждый кадр: Begin -> несколько Draw/DrawLine -> End. Картинки берутся из запаса и переиспользуются.
public class ThrowAim : MonoBehaviour
{
    public int sortingLayerID;
    public int sortingOrder;

    readonly List<SpriteRenderer> pool = new List<SpriteRenderer>();
    int used;

    public void Begin() => used = 0;

    public void End()
    {
        for (int i = used; i < pool.Count; i++)
            if (pool[i].enabled) pool[i].enabled = false;
    }

    public void Hide()
    {
        Begin();
        End();
    }

    // Одна картинка в точке pos, повёрнутая по направлению dir
    public SpriteRenderer Draw(Sprite sprite, Vector2 pos, Vector2 dir, Color color, float scale = 1f)
    {
        if (sprite == null) return null;
        SpriteRenderer r = Next();
        r.sprite = sprite;
        r.color = color;
        r.transform.position = pos;
        r.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);
        r.transform.localScale = Vector3.one * scale;
        return r;
    }

    // Линия из картинок от from в сторону dir длиной length, через каждые spacing клеток.
    // К концу линия немного бледнеет
    public void DrawLine(Sprite sprite, Vector2 from, Vector2 dir, float length, float spacing, Color color, float scale = 1f)
    {
        if (sprite == null || length <= 0f || spacing <= 0.01f) return;
        int n = Mathf.FloorToInt(length / spacing);
        for (int i = 0; i <= n; i++)
        {
            float k = n > 0 ? (float)i / n : 0f;
            Color c = color;
            c.a *= Mathf.Lerp(1f, 0.45f, k);
            Draw(sprite, from + dir * (i * spacing), dir, c, scale);
        }
    }

    SpriteRenderer Next()
    {
        if (used >= pool.Count)
        {
            var go = new GameObject("AimPart");
            go.transform.SetParent(transform, false);
            pool.Add(go.AddComponent<SpriteRenderer>());
        }
        SpriteRenderer r = pool[used++];
        r.enabled = true;
        r.sortingLayerID = sortingLayerID;
        r.sortingOrder = sortingOrder;
        return r;
    }
}
