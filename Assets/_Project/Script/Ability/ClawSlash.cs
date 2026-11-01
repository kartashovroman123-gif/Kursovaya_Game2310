using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

// Слабая атака когтями: удар в один из 4 углов. Дуга захватывает угол и обе соседние стороны.
// Урон ПОКА НЕ НАНОСИТСЯ: найденные цели только пишутся в Console.
// Когда у друга будет здоровье, править нужно только метод DealDamage.
[CreateAssetMenu(fileName = "ClawSlash", menuName = "Game/Abilities/Claw Slash")]
public class ClawSlash : AbilityData
{
    [Header("Дуга удара")]
    [Tooltip("Ширина дуги в градусах. 90 = ровно угол, 150 = угол и почти целиком обе стороны")]
    [Range(10f, 360f)] public float arcAngle = 150f;
    [Tooltip("Враги ближе этого расстояния задеваются с любой стороны (чтобы не промахнуться по тем, кто вплотную)")]
    public float innerRadius = 0.4f;
    [Tooltip("Какие слои считать целями. Для врагов создай слой Enemy и выбери только его")]
    public LayerMask targetMask = ~0;

    [Header("Эффект")]
    public SlashEffect effectPrefab;
    [Tooltip("Во сколько раз больше вспышка у последнего удара серии")]
    public float finisherEffectSize = 1.25f;

    [Header("Ощущение попадания")]
    [Tooltip("Стоп-кадр при попадании, сек (0 = выключить)")]
    public float hitStop = 0.05f;
    [Tooltip("Толчок камеры при попадании, в клетках")]
    public float hitKick = 0.1f;
    [Tooltip("Во сколько раз сильнее стоп-кадр и толчок у последнего удара серии")]
    public float finisherFeelMultiplier = 1.6f;

    public override void Execute(AbilityContext ctx)
    {
        SpawnEffect(ctx);

        Collider2D[] found = Physics2D.OverlapCircleAll(ctx.origin, ctx.range, targetMask);
        var already = new HashSet<GameObject>();   // чтобы одна цель не получила удар дважды
        float half = arcAngle * 0.5f;

        foreach (Collider2D c in found)
        {
            if (c.isTrigger) continue;                                            // двери-триггеры и т.п.
            if (c is CompositeCollider2D || c.GetComponent<Tilemap>() != null) continue;   // стены и пол
            GameObject target = c.attachedRigidbody != null ? c.attachedRigidbody.gameObject : c.gameObject;
            if (ctx.owner != null && target == ctx.owner.gameObject) continue;    // не бьём себя
            if (already.Contains(target)) continue;

            // попадает ли цель в дугу: угол между направлением удара и ближайшей точкой цели
            Vector2 closest = c.ClosestPoint(ctx.origin);
            Vector2 toTarget = closest - ctx.origin;
            bool close = toTarget.magnitude <= innerRadius;
            if (!close && Vector2.Angle(ctx.direction, toTarget) > half) continue;

            already.Add(target);
            DealDamage(target, ctx);
        }

        if (already.Count > 0)
        {
            float m = ctx.comboFinisher ? finisherFeelMultiplier : 1f;
            HitStop.Do(hitStop * m);
            CameraShake.Kick(ctx.direction * hitKick * m);
        }

        DrawDebugArc(ctx);
    }

    // ЕДИНСТВЕННОЕ место, куда друг подключает урон
    protected virtual void DealDamage(GameObject target, AbilityContext ctx)
    {
        Debug.Log($"[{displayName}] попал по '{target.name}' (урон {ctx.damage:0.#} пока не наносится)");

        // TODO (здоровье): var hp = target.GetComponent<IDamageable>(); if (hp != null) hp.TakeDamage(ctx.damage);
        // TODO (ультимейт): ctx.owner.RegisterHit();   когда появится заряд ультимейта
    }

    void SpawnEffect(AbilityContext ctx)
    {
        if (effectPrefab == null) return;
        SlashEffect fx = Object.Instantiate(effectPrefab, ctx.origin, Quaternion.identity);
        fx.Play(ctx.range,
                ctx.direction.x >= 0f ? 1 : -1,          // вправо / влево
                ctx.direction.y > 0f,                    // вверх / вниз
                ctx.comboStep % 2 == 1,                  // каждый второй взмах - в обратную сторону
                ctx.comboFinisher ? finisherEffectSize : 1f,
                ctx.effectBehind);
    }

    // Красные линии границ дуги в окне Scene (видны 0.3 с)
    void DrawDebugArc(AbilityContext ctx)
    {
        float half = arcAngle * 0.5f;
        Vector3 a = Quaternion.Euler(0f, 0f, half) * (Vector3)ctx.direction * ctx.range;
        Vector3 b = Quaternion.Euler(0f, 0f, -half) * (Vector3)ctx.direction * ctx.range;
        Vector3 o = ctx.origin;
        Debug.DrawLine(o, o + a, Color.red, 0.3f);
        Debug.DrawLine(o, o + b, Color.red, 0.3f);
        Debug.DrawLine(o + a, o + b, Color.red, 0.3f);
    }
}
