using UnityEngine;

// Нож кукри в полёте (ThrownProjectile -> KukriKnife): крутится, попав - пропадает,
// о стену - звякает (толчок камеры) и быстро исчезает.
public class KukriKnife : ThrownProjectile
{
    float spin;

    protected override void OnLaunch()
    {
        var kukri = Data as KukriThrowable;
        spin = kukri != null ? kukri.spinSpeed : 900f;
    }

    protected override void OnFly()
    {
        transform.Rotate(0f, 0f, -spin * Time.deltaTime);
    }

    protected override void OnHitTarget(Collider2D col, IDamageable target)
    {
        CameraShake.Kick(Direction * 0.03f);
        base.OnHitTarget(col, target);
    }

    protected override void OnHitWall(RaycastHit2D hit)
    {
        SpawnHitEffect();
        FadeOut(0.1f);
    }
}
