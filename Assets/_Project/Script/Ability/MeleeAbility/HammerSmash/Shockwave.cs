using System.Collections.Generic;
using UnityEngine;

// Удар молотом по земле: цепочка вспышек. Создаётся сама (HammerCombo.Execute), на сцену ставить не нужно.
// Зона делится на «шаги» от героя наружу. Шаги вспыхивают по очереди за waveTime секунд:
// в каждом шаге в земле появляются трещины и из них вырывается фиолетовое пламя.
// Вспышки не двигаются - движение создаёт очерёдность. На опасном краю трещины крупнее, пламя сильнее.
// Урон: цель получает удар, когда вспыхнул шаг, до которого она дотягивается (каждая цель - один раз).
// Вспышки за стеной не появляются, и урон через стену не проходит.
public class Shockwave : MonoBehaviour
{
    struct Burst
    {
        public Vector2 pos;
        public bool edge;
        public bool blocked;   // за стеной
    }

    HammerCombo ability;
    HammerStage st;
    AbilityContext ctx;
    Vector2 origin, fwd, side;
    float t;
    int nextStep;          // какой шаг вспыхнет следующим
    float firedU = -1f;    // до какой позиции пути уже дошла цепочка (для урона)
    readonly List<List<Burst>> rows = new List<List<Burst>>();
    readonly List<float> rowU = new List<float>();
    readonly HashSet<GameObject> hit = new HashSet<GameObject>();
    bool anyHit;

    public static Shockwave Launch(HammerCombo ability, HammerStage stage, AbilityContext ctx)
    {
        var go = new GameObject("HammerShockwave");
        var w = go.AddComponent<Shockwave>();
        w.Init(ability, stage, ctx);
        return w;
    }

    void Init(HammerCombo a, HammerStage stage, AbilityContext c)
    {
        ability = a; st = stage; ctx = c;
        origin = c.origin;
        fwd = c.direction.sqrMagnitude > 0.0001f ? c.direction.normalized : Vector2.right;
        side = new Vector2(-fwd.y, fwd.x);
        BuildRows();
    }

    // ---------- Раскладка вспышек ----------

    void BuildRows()
    {
        int n = Mathf.Max(1, st.steps);
        float inner = st.innerFraction;
        float spacing = Mathf.Max(0.2f, ability.burstSpacing);

        for (int k = 0; k < n; k++)
        {
            float u = inner + (1f - inner) * (k + 1f) / n * 0.97f;   // последний шаг - почти у самого конца зоны
            bool edge = u >= 1f - st.edgeFraction;
            var row = new List<Burst>();
            bool shift = k % 2 == 1;                            // соседние шаги сдвинуты - «ёлочка», ритмичнее

            if (st.shape == HammerShape.Lane)
            {
                int m = Mathf.Max(1, Mathf.RoundToInt(st.width / spacing * 1.5f));   // на узкой дорожке - по 2 в ряд
                for (int j = 0; j < m; j++)
                {
                    float s = -st.width * 0.5f + st.width * (j + 0.5f) / m;
                    if (m == 1) s += (shift ? 0.18f : -0.18f) * st.width;
                    AddBurst(row, origin + side * s, fwd * (u * st.length), edge);
                }
            }
            else if (st.shape == HammerShape.Cone)
            {
                // конус: на расстоянии r вспышки ставятся по дуге шириной в конус на этом расстоянии.
                // У героя дуга короткая (1 вспышка), к круглому концу - всё шире
                float r = st.length * u;
                float halfAng = HammerCombo.ConeHalfAngleAt(st, r);
                if (halfAng > 0f)
                {
                    float arc = 2f * halfAng * r;
                    int m = Mathf.Max(1, Mathf.RoundToInt(arc / spacing));
                    for (int j = 0; j < m; j++)
                    {
                        float frac = m == 1 ? 0.5f : (j + 0.5f + (shift ? 0.2f : -0.2f)) / m;
                        float ang = Mathf.Lerp(-halfAng, halfAng, Mathf.Clamp01(frac)) * 0.92f;   // чуть внутрь от края
                        AddBurst(row, origin, fwd * (r * Mathf.Cos(ang)) + side * (r * Mathf.Sin(ang)), edge);
                    }
                }
            }
            else
            {
                bool half = st.shape == HammerShape.HalfEllipse;
                float F = st.length * u, S = (half ? st.width * 0.5f : st.length) * u;
                float from = half ? -90f : 0f, to = half ? 90f : 360f;
                float perimeter = Mathf.PI * (3f * (F + S) - Mathf.Sqrt((3f * F + S) * (F + 3f * S)));
                float arc = perimeter * (to - from) / 360f;
                int m = Mathf.Max(1, Mathf.RoundToInt(arc / spacing));
                for (int j = 0; j < m; j++)
                {
                    // дуга: вспышки равномерно, соседние шаги сдвинуты на четверть промежутка; круг: на половину
                    float frac = half
                        ? (m == 1 ? 0.5f : (j + 0.5f + (shift ? 0.25f : -0.25f)) / m)
                        : (j + (shift ? 0.5f : 0f)) / m;
                    float ang = Mathf.Lerp(from, to, Mathf.Clamp01(frac)) * Mathf.Deg2Rad;
                    Vector2 p = fwd * (F * Mathf.Cos(ang)) + side * (S * Mathf.Sin(ang));
                    AddBurst(row, origin, p, edge);
                }
            }
            rows.Add(row);
            rowU.Add(u);
        }
    }

    void AddBurst(List<Burst> row, Vector2 start, Vector2 path, bool edge)
    {
        Vector2 pos = start + path + Random.insideUnitCircle * ability.burstJitter;
        Vector2 ray = pos - start;
        float len = ray.magnitude;
        bool blocked = len > 0.01f && MeleeAbility.WallDistance(start, ray, len) < len - 0.05f;
        row.Add(new Burst { pos = pos, edge = edge, blocked = blocked });
    }

    // ---------- Каждый кадр ----------

    void Update()
    {
        t += Time.deltaTime;
        int n = rows.Count;
        float stepTime = n > 1 ? Mathf.Max(0.01f, st.waveTime) / (n - 1) : 0f;

        // вспыхнуть всем шагам, чьё время пришло
        while (nextStep < n && t >= nextStep * stepTime)
        {
            FireRow(nextStep);
            float halfStep = (1f - st.innerFraction) / Mathf.Max(1, n) * 0.5f;
            firedU = rowU[nextStep] + halfStep;
            nextStep++;
        }

        HitTargets();

        if (nextStep >= n) Destroy(gameObject, 0.05f);
    }

    void FireRow(int k)
    {
        foreach (Burst b in rows[k])
        {
            if (b.blocked) continue;
            Spawn(b.edge && ability.edgeCrackPrefab != null ? ability.edgeCrackPrefab : ability.crackPrefab, b.pos);
            Spawn(b.edge && ability.edgeEruptionPrefab != null ? ability.edgeEruptionPrefab : ability.eruptionPrefab, b.pos);
            if (ability.dustPrefab != null && Random.value < 0.6f)
                Spawn(ability.dustPrefab, b.pos + Random.insideUnitCircle * 0.25f);
        }
        if (k == rows.Count - 1 || rows[k].Count > 0 && rows[k][0].edge)
            CameraShake.Kick(fwd * 0.03f);   // лёгкий толчок на сильных вспышках
    }

    void Spawn(FlamePuff prefab, Vector2 pos)
    {
        if (prefab == null) return;
        FlamePuff fx = Instantiate(prefab, pos, Quaternion.identity);
        if (!Mathf.Approximately(ability.burstScale, 1f)) fx.transform.localScale *= ability.burstScale;
    }

    // ---------- Урон ----------

    void HitTargets()
    {
        if (firedU < 0f) return;
        float reach = HammerCombo.MaxReach(st) * Mathf.Min(1f, firedU) + 0.5f;
        Collider2D[] found = Physics2D.OverlapCircleAll(origin, reach, ability.targetMask);
        float margin = (1f - st.innerFraction) / Mathf.Max(1, rows.Count) * 0.5f;

        foreach (Collider2D c in found)
        {
            GameObject target = MeleeAbility.GetTarget(c, ctx);
            if (target == null || hit.Contains(target)) continue;

            // насколько далеко по пути цель (берём ближнюю и центральную точку - большая цель задевается краем)
            Vector2 closest = c.ClosestPoint(origin);
            float uClose = HammerCombo.PathPosition(st, ToLocal(closest));
            float uCenter = HammerCombo.PathPosition(st, ToLocal(c.bounds.center));
            float uMin = Mathf.Min(uClose, uCenter);
            float uMax = Mathf.Max(float.IsInfinity(uClose) ? -1f : uClose, float.IsInfinity(uCenter) ? -1f : uCenter);

            if (float.IsInfinity(uMin) || uMin > 1f) continue;                 // вне зоны
            if (uMax < st.innerFraction - margin) continue;                    // в пустой части у героя
            if (uMin > firedU) continue;                                       // цепочка ещё не дошла

            // стена между ударом и целью
            Vector2 local = ToLocal(closest);
            Vector2 rayStart = st.shape == HammerShape.Lane ? origin + side * local.y : origin;
            Vector2 toTarget = closest - rayStart;
            if (MeleeAbility.WallDistance(rayStart, toTarget, toTarget.magnitude) < toTarget.magnitude - 0.05f) continue;

            bool edge = uMax >= 1f - st.edgeFraction;
            Vector2 push = st.shape == HammerShape.Lane ? fwd : (Vector2)c.bounds.center - origin;

            hit.Add(target);
            ability.HitByWave(target, c, ctx, edge, push);
            if (!anyHit) { anyHit = true; ability.WaveFirstHit(ctx); }
        }
    }

    Vector2 ToLocal(Vector2 world)
    {
        Vector2 d = world - origin;
        return new Vector2(Vector2.Dot(d, fwd), Vector2.Dot(d, side));
    }
}
