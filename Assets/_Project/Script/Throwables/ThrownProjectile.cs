using System.Collections;
using UnityEngine;

// БРОШЕННЫЙ СНАРЯД (верх дерева: ThrownProjectile -> KukriKnife / SpearProjectile).
// Создаётся предметом (ProjectileThrowable) при броске, на сцену и в префабы ставить не нужно.
// Летит по прямой, каждый кадр проверяя путь впереди (CircleCast), поэтому не пролетает сквозь тонкие стены.
// Во что попадает:
//   - враг (скрипт с IDamageable на нём или родителе) - урон, снаряд пропадает;
//   - твёрдый коллайдер (стена, колонна) - снаряд пропадает;
//   - триггеры без здоровья (зоны дверей, сундуков, предметы) и сам герой - пролетает мимо.
// Потомки меняют: как снаряд выглядит в полёте (OnFly), что при попадании во врага (OnHitTarget) и в стену (OnHitWall).
public class ThrownProjectile : MonoBehaviour
{
    protected ProjectileThrowable Data { get; private set; }
    protected Vector2 Direction { get; private set; }
    protected GameObject Owner { get; private set; }
    protected float Damage { get; private set; }
    protected SpriteRenderer Renderer { get; private set; }
    protected bool Stopped { get; private set; }

    float dieAt;
    static readonly RaycastHit2D[] results = new RaycastHit2D[16];

    public void Launch(ProjectileThrowable data, ThrowContext ctx, Vector2 dir)
    {
        Data = data;
        Owner = ctx.owner;
        Direction = dir.normalized;
        Damage = data.damage * ctx.damageMultiplier;
        Renderer = GetComponent<SpriteRenderer>();
        dieAt = Time.time + data.lifetime;

        float angle = Mathf.Atan2(Direction.y, Direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);    // картинка нарисована остриём вправо
        OnLaunch();
        Move(0f);    // враг вплотную к герою - попадание сразу
    }

    void Update()
    {
        if (Stopped || Data == null) return;
        if (Time.time >= dieAt) { Expire(); return; }
        Move(Data.speed * Time.deltaTime);
        if (!Stopped) OnFly();
    }

    // Сдвинуться на distance; если по пути что-то есть - попадание
    void Move(float distance)
    {
        if (CastFirst(transform.position, Data.radius, Direction, distance, Data.hitLayers, Owner,
                      out RaycastHit2D hit, out IDamageable target))
        {
            transform.position = hit.centroid;   // встаём в точку удара
            if (target != null)
            {
                target.TakeDamage(Damage, Direction, Owner);
                OnHitTarget(hit.collider, target);
            }
            else OnHitWall(hit);
            return;
        }
        transform.position += (Vector3)(Direction * distance);
    }

    // ----- Решают потомки -----
    protected virtual void OnLaunch() { }
    protected virtual void OnFly() { }

    protected virtual void OnHitTarget(Collider2D col, IDamageable target)
    {
        SpawnHitEffect();
        Finish();
    }

    protected virtual void OnHitWall(RaycastHit2D hit)
    {
        SpawnHitEffect();
        Finish();
    }

    // Время вышло, ни во что не попал
    protected virtual void Expire() => FadeOut(0.12f);

    // ----- Для потомков -----
    protected void Finish()
    {
        Stopped = true;
        Destroy(gameObject);
    }

    // Остановиться и плавно исчезнуть (копьё в стене, нож на излёте)
    protected void FadeOut(float time)
    {
        Stopped = true;
        StartCoroutine(FadeRoutine(time));
    }

    IEnumerator FadeRoutine(float time)
    {
        float t = 0f;
        Color c = Renderer != null ? Renderer.color : Color.white;
        while (t < time)
        {
            t += Time.deltaTime;
            if (Renderer != null) Renderer.color = new Color(c.r, c.g, c.b, c.a * (1f - t / time));
            yield return null;
        }
        Destroy(gameObject);
    }

    protected void SpawnHitEffect()
    {
        if (Data.hitEffect != null)
            Instantiate(Data.hitEffect, transform.position, Quaternion.identity, transform.parent);
    }

    // ----- Общая проверка пути (её же использует прицел) -----
    // true - по пути что-то есть: hit - ближайшее попадание, target - здоровье врага (null = стена)
    public static bool CastFirst(Vector2 origin, float radius, Vector2 dir, float distance, LayerMask mask,
                                 GameObject owner, out RaycastHit2D hit, out IDamageable target)
    {
        hit = default;
        target = null;
        var filter = new ContactFilter2D();
        filter.SetLayerMask(mask);
        filter.useTriggers = true;           // у врагов зона попаданий бывает триггером

        int n = Physics2D.CircleCast(origin, radius, dir, filter, results, distance);
        float best = float.MaxValue;
        bool found = false;
        for (int i = 0; i < n; i++)
        {
            Collider2D c = results[i].collider;
            if (c == null) continue;
            if (owner != null && c.transform.IsChildOf(owner.transform)) continue;   // сам герой
            IDamageable t = c.GetComponentInParent<IDamageable>();
            if (t == null && c.isTrigger) continue;                                  // зоны дверей, сундуков, предметы
            if (results[i].distance < best)
            {
                best = results[i].distance;
                hit = results[i];
                target = t;
                found = true;
            }
        }
        return found;
    }
}
