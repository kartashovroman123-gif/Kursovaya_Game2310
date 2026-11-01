using UnityEngine;

// Временная анимация атаки: на время удара подменяет спрайт героя кадрами атаки
// (вид спереди или сзади, влево/вправо зеркалит PlayerAnimation).
// Работает в LateUpdate: Animator уже поставил кадр ходьбы/стойки, а мы его перекрываем.
[RequireComponent(typeof(SpriteRenderer))]
public class AttackPose : MonoBehaviour
{
    SpriteRenderer sr;
    PlayerAbilities abilities;
    PlayerStats stats;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        abilities = GetComponent<PlayerAbilities>();
        stats = GetComponent<PlayerStats>();
    }

    void LateUpdate()
    {
        if (abilities == null || stats == null || stats.data == null) return;
        if (!abilities.IsAttackAnimating) return;                     // удара нет - работает обычный Animator

        Sprite[] frames = stats.data.GetLightAttackFrames(abilities.Facing);
        if (frames == null || frames.Length == 0) return;             // у героя нет кадров - ничего не делаем

        int i = Mathf.Min(frames.Length - 1, (int)(abilities.AttackProgress * frames.Length));
        if (frames[i] != null) sr.sprite = frames[i];
    }
}
