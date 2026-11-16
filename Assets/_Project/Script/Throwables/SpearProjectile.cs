using UnityEngine;

// Копьё в полёте (ThrownProjectile -> SpearProjectile): летит остриём вперёд.
// Во врага - урон, отбрасывание, стоп-кадр, толчок камеры. В стену - втыкается и через мгновение исчезает.
public class SpearProjectile : ThrownProjectile
{
    SpearThrowable Spear => Data as SpearThrowable;

    protected override void OnHitTarget(Collider2D col, IDamageable target)
    {
        SpearThrowable s = Spear;
        if (s != null)
        {
            Rigidbody2D body = col.attachedRigidbody;
            if (body != null && body.bodyType == RigidbodyType2D.Dynamic)
                body.AddForce(Direction * s.knockback, ForceMode2D.Impulse);
            if (s.hitStop > 0f) HitStop.Do(s.hitStop);
        }
        CameraShake.Kick(Direction * 0.08f);
        base.OnHitTarget(col, target);
    }

    protected override void OnHitWall(RaycastHit2D hit)
    {
        SpawnHitEffect();
        CameraShake.Kick(Direction * 0.04f);
        transform.position = hit.centroid + Direction * 0.15f;   // наконечник чуть входит в стену
        SpearThrowable s = Spear;
        FadeOut(s != null ? s.stuckTime : 0.4f);
    }
}
