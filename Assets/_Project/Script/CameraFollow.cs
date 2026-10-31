using UnityEngine;
using UnityEngine.Tilemaps;

[RequireComponent(typeof(Camera))]
public class CameraFollow : MonoBehaviour
{
    [Header("Пружина (инерция)")]
    [Tooltip("Скорость реакции камеры. 1.5 = ленивая, 3 = резвая")]
    public float frequency = 2f;
    [Tooltip("1 = без перелёта; 0.5–0.7 = лёгкий перелёт и возврат; меньше = сильная раскачка")]
    [Range(0.1f, 1.5f)] public float damping = 0.6f;

    [Header("Взгляд вперёд по движению")]
    [Tooltip("На сколько секунд вперёд по движению героя смотрит камера")]
    public float lookAhead = 0.3f;
    [Tooltip("Максимальное смещение вперёд, в клетках")]
    public float maxLookAhead = 2f;
    [Tooltip("Плавность смены направления взгляда (сек)")]
    public float lookAheadSmooth = 0.25f;

    public Vector2 offset;

    [Header("Границы комнаты")]
    public bool useBounds = true;
    [Tooltip("Tilemap ПОЛА. Если задан, границы берутся из него автоматически")]
    public Tilemap boundsFromTilemap;
    [Tooltip("Запасной вариант: границы из BoxCollider2D")]
    public BoxCollider2D boundsCollider;
    [Tooltip("Запасной вариант: ручные границы")]
    public Vector2 boundsMin = new Vector2(-10.5f, -6.5f);
    public Vector2 boundsMax = new Vector2(10.5f, 6.5f);
    [Tooltip("На сколько камера может выйти за границу (плавно замедляясь). 0 = жёсткий упор")]
    public float softMargin = 1f;

    Transform target;
    Camera cam;

    Vector2 pos, vel;              // позиция и скорость самой камеры
    Vector2 lastTargetPos, targetVel;
    Vector2 look, lookVel;

    void Awake()
    {
        cam = GetComponent<Camera>();
    }

    void LateUpdate()
    {
        if (target == null)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p == null) return;
            target = p.transform;
            Snap();
            return;
        }

        float dt = Mathf.Min(Time.deltaTime, 0.1f);   // защита от рывка после лага
        if (dt <= 0f) return;

        // скорость героя (считаем сами, т.к. его скрипт не задаёт velocity)
        Vector2 tp = target.position;
        Vector2 rawVel = (tp - lastTargetPos) / dt;
        lastTargetPos = tp;
        targetVel = Vector2.Lerp(targetVel, rawVel, 1f - Mathf.Exp(-dt * 10f));

        // взгляд вперёд
        Vector2 wantLook = Vector2.ClampMagnitude(targetVel * lookAhead, maxLookAhead);
        look = Vector2.SmoothDamp(look, wantLook, ref lookVel, lookAheadSmooth, Mathf.Infinity, dt);

        Vector2 goal = tp + offset + look;
        if (useBounds) goal = ClampSoft(goal);

        // пружина: несколько маленьких шагов, чтобы не зависеть от FPS
        float w = 2f * Mathf.PI * frequency;
        int steps = Mathf.Max(1, Mathf.CeilToInt(dt / 0.004f));
        float h = dt / steps;
        for (int i = 0; i < steps; i++)
        {
            Vector2 acc = w * w * (goal - pos) - 2f * damping * w * vel;
            vel += acc * h;
            pos += vel * h;
        }

        if (useBounds) ClampHard();
        transform.position = new Vector3(pos.x, pos.y, -10f);
    }

    // Мгновенно поставить камеру на героя (вызывать при телепорте в новую комнату)
    public void Snap()
    {
        if (target == null) return;
        lastTargetPos = target.position;
        targetVel = vel = look = lookVel = Vector2.zero;
        Vector2 goal = (Vector2)target.position + offset;
        pos = useBounds ? ClampSoft(goal) : goal;
        transform.position = new Vector3(pos.x, pos.y, -10f);
    }

    public void SetBoundsFromTilemap(Tilemap floor)
    {
        boundsFromTilemap = floor;
        useBounds = true;
    }

    // ---------- границы ----------
    void GetBounds(out Vector2 min, out Vector2 max)
    {
        if (boundsFromTilemap != null)
        {
            boundsFromTilemap.CompressBounds();
            BoundsInt cb = boundsFromTilemap.cellBounds;
            min = boundsFromTilemap.CellToWorld(cb.min);
            max = boundsFromTilemap.CellToWorld(cb.max);
        }
        else if (boundsCollider != null)
        {
            Bounds b = boundsCollider.bounds;
            min = b.min; max = b.max;
        }
        else
        {
            min = boundsMin; max = boundsMax;
        }
    }

    void GetCameraLimits(out Vector2 lo, out Vector2 hi)
    {
        GetBounds(out Vector2 min, out Vector2 max);
        float halfH = cam.orthographicSize;
        float halfW = halfH * cam.aspect;
        lo = new Vector2(min.x + halfW, min.y + halfH);
        hi = new Vector2(max.x - halfW, max.y - halfH);
    }

    Vector2 ClampSoft(Vector2 p)
    {
        GetCameraLimits(out Vector2 lo, out Vector2 hi);
        p.x = SoftAxis(p.x, lo.x, hi.x);
        p.y = SoftAxis(p.y, lo.y, hi.y);
        return p;
    }

    // перелёт пружины тоже не даём уносить камеру дальше мягкого предела
    void ClampHard()
    {
        GetCameraLimits(out Vector2 lo, out Vector2 hi);
        HardAxis(ref pos.x, ref vel.x, lo.x, hi.x);
        HardAxis(ref pos.y, ref vel.y, lo.y, hi.y);
    }

    void HardAxis(ref float p, ref float v, float lo, float hi)
    {
        if (lo > hi) { p = (lo + hi) * 0.5f; v = 0f; return; }
        float a = lo - softMargin, b = hi + softMargin;
        if (p < a) { p = a; if (v < 0f) v = 0f; }
        if (p > b) { p = b; if (v > 0f) v = 0f; }
    }

    float SoftAxis(float v, float lo, float hi)
    {
        if (lo > hi) return (lo + hi) * 0.5f;
        if (v > hi) return hi + Soft(v - hi);
        if (v < lo) return lo - Soft(lo - v);
        return v;
    }

    float Soft(float d)
    {
        if (softMargin <= 0.0001f) return 0f;
        return softMargin * (1f - Mathf.Exp(-d / softMargin));
    }

    void OnDrawGizmosSelected()
    {
        if (!useBounds) return;
        GetBounds(out Vector2 min, out Vector2 max);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube((min + max) * 0.5f, max - min);
    }
}