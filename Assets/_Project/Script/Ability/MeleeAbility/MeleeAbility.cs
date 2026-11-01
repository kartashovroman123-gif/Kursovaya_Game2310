using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

// ОБЩИЙ ШАБЛОН ЛЮБОГО УДАРА БЛИЖНЕГО БОЯ (средний уровень дерева: AbilityData -> MeleeAbility -> конкретный приём).
// Здесь всё, что одинаково у когтей, рапиры и молота: вспышка, поиск целей, фильтры, нанесение урона,
// стоп-кадр и толчок камеры. Потомки меняют только то, чем отличаются, через виртуальные методы:
//   IsInZone      - попала ли цель в зону удара (дуга, круг, узкая полоса...)
//   ComputeDamage - базовый урон этого удара
//   DamageFor     - урон по конкретной цели (например, край молота бьёт сильнее рукояти)
//   DealDamage    - единственное место, куда друг подключает здоровье
//   OnTargetHit   - что делать с каждой пораженной целью (отбрасывание и т.п.)
//   OnHitFeel     - стоп-кадр и толчок камеры, если хоть кого-то задели
public abstract class MeleeAbility : AbilityData
{
    [Header("Зона удара")]
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
        ctx.damage = ComputeDamage(ctx);

        Collider2D[] found = Physics2D.OverlapCircleAll(ctx.origin, ctx.range, targetMask);
        var already = new HashSet<GameObject>();   // чтобы одна цель не получила удар дважды

        foreach (Collider2D c in found)
        {
            if (c.isTrigger) continue;                                            // двери-триггеры и т.п.
            if (c is CompositeCollider2D || c.GetComponent<Tilemap>() != null) continue;   // стены и пол
            GameObject target = c.attachedRigidbody != null ? c.attachedRigidbody.gameObject : c.gameObject;
            if (ctx.owner != null && target == ctx.owner.gameObject) continue;    // не бьём себя
            if (already.Contains(target)) continue;

            // попала ли цель в зону: смотрим на ближайшую к герою точку цели
            Vector2 closest = c.ClosestPoint(ctx.origin);
            Vector2 toTarget = closest - ctx.origin;
            bool close = toTarget.magnitude <= innerRadius;
            if (!close && !IsInZone(ctx, toTarget)) continue;

            already.Add(target);
            DealDamage(target, ctx, DamageFor(ctx, c, toTarget));
            OnTargetHit(target, ctx, c);
        }

        if (already.Count > 0) OnHitFeel(ctx);

        DrawDebugZone(ctx);
    }

    // ---------- Что переопределяют потомки ----------

    // По умолчанию зона - дуга шириной arcAngle вокруг направления удара
    protected virtual bool IsInZone(AbilityContext ctx, Vector2 toTarget)
    {
        return Vector2.Angle(ctx.direction, toTarget) <= arcAngle * 0.5f;
    }

    // По умолчанию - урон из ассета (с учётом предметов, которые уже посчитал PlayerAbilities)
    protected virtual float ComputeDamage(AbilityContext ctx)
    {
        return ctx.damage;
    }

    // Урон по конкретной цели. По умолчанию одинаковый для всех
    protected virtual float DamageFor(AbilityContext ctx, Collider2D target, Vector2 toTarget)
    {
        return ctx.damage;
    }

    // ЕДИНСТВЕННОЕ место, куда друг подключает урон
    protected virtual void DealDamage(GameObject target, AbilityContext ctx, float damage)
    {
        Debug.Log($"[{displayName}] попал по '{target.name}' (урон {damage:0.#} пока не наносится)");

        // TODO (здоровье): var hp = target.GetComponent<IDamageable>(); if (hp != null) hp.TakeDamage(damage);
        // TODO (ультимейт): ctx.owner.RegisterHit();   когда появится заряд ультимейта
    }

    // Дополнительное действие с каждой пораженной целью (по умолчанию - ничего)
    protected virtual void OnTargetHit(GameObject target, AbilityContext ctx, Collider2D col) { }

    // Стоп-кадр и толчок камеры, если удар кого-то задел
    protected virtual void OnHitFeel(AbilityContext ctx)
    {
        float m = ctx.comboFinisher ? finisherFeelMultiplier : 1f;
        HitStop.Do(hitStop * m);
        CameraShake.Kick(ctx.direction * hitKick * m);
    }

    // ---------- Общее ----------

    protected virtual void SpawnEffect(AbilityContext ctx)
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
    protected virtual void DrawDebugZone(AbilityContext ctx)
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
