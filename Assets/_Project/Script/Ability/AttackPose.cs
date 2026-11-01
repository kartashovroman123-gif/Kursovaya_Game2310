using UnityEngine;

// Временные анимации-позы героя поверх Animator: рывок (полуприсед), тяжёлая атака и слабая атака.
// Кадры берутся из CharacterData. Вид спереди/сзади, влево/вправо выбирает PlayerAnimation.
// Работает в LateUpdate: Animator уже поставил кадр ходьбы/стойки, а мы его перекрываем.
[RequireComponent(typeof(SpriteRenderer))]
public class AttackPose : MonoBehaviour
{
    SpriteRenderer sr;
    PlayerAbilities abilities;
    PlayerStats stats;
    PlayerAnimation playerAnim;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        abilities = GetComponent<PlayerAbilities>();
        stats = GetComponent<PlayerStats>();
        playerAnim = GetComponent<PlayerAnimation>();
    }

    void LateUpdate()
    {
        if (abilities == null || stats == null || stats.data == null) return;

        Sprite[] frames = null;
        float progress = 0f;

        if (abilities.ShowDashPose)
        {
            bool back = playerAnim != null && playerAnim.FacingBack;
            frames = stats.data.GetDashFrames(back ? AttackFacing.Back : AttackFacing.Front);
            progress = abilities.DashProgress;
        }
        else if (abilities.CurrentPose != null)
        {
            frames = abilities.CurrentPose.Get(abilities.Facing);
            progress = abilities.AttackProgress;
        }
        else if (abilities.IsAttackAnimating)
        {
            frames = stats.data.GetLightAttackFrames(abilities.Facing);
            progress = abilities.AttackProgress;
        }

        if (frames == null || frames.Length == 0) return;   // кадров нет - работает обычный Animator

        int i = Mathf.Min(frames.Length - 1, (int)(progress * frames.Length));
        if (frames[i] != null) sr.sprite = frames[i];
    }
}
