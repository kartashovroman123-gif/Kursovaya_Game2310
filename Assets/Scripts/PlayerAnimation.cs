using UnityEngine;

// Анимация героя: скорость для Animator, поворот влево/вправо (зеркало)
// и поворот лицом/спиной (вид 3/4 спереди или сзади).
// Лицом/спиной переключается заменой контроллера Animator:
// CharacterData.frontAnimator = вид спереди, CharacterData.backAnimator = вид сзади.
public class PlayerAnimation : MonoBehaviour
{
    [Tooltip("С какой скорости по вертикали герой поворачивается спиной/лицом. " +
             "Если идёт строго вбок - остаётся как был")]
    public float turnThreshold = 0.5f;

    Rigidbody2D rb;
    Animator anim;
    SpriteRenderer sr;
    PlayerAbilities abilities;
    PlayerStats stats;
    Vector2 lastPos;

    RuntimeAnimatorController frontController;
    bool facingBack;
    bool dataApplied;
    public bool FacingBack => facingBack;

    static readonly int SpeedHash = Animator.StringToHash("Speed");

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        sr = GetComponent<SpriteRenderer>();
        abilities = GetComponent<PlayerAbilities>();
        stats = GetComponent<PlayerStats>();
        lastPos = rb.position;
        frontController = anim.runtimeAnimatorController;   // запасной: тот, что стоит в префабе
    }

    // Данные героя приходят после создания (PlayerSpawner вызывает Init),
    // поэтому применяем их при первом шаге физики
    void ApplyData()
    {
        if (dataApplied || stats == null || stats.data == null) return;
        dataApplied = true;
        if (stats.data.frontAnimator != null)
        {
            frontController = stats.data.frontAnimator;
            anim.runtimeAnimatorController = frontController;
            facingBack = false;
        }
    }

    void FixedUpdate()
    {
        ApplyData();

        Vector2 pos = rb.position;
        Vector2 v = (pos - lastPos) / Time.fixedDeltaTime;
        lastPos = pos;
        anim.SetFloat(SpeedHash, v.magnitude);

        bool back = facingBack;
        if (abilities != null && abilities.IsAttacking)
        {
            // во время удара смотрим в угол удара
            sr.flipX = abilities.AttackSideX < 0;
            back = abilities.AttackUp;
        }
        else
        {
            if (v.x > 0.05f) sr.flipX = false;
            else if (v.x < -0.05f) sr.flipX = true;

            if (v.y > turnThreshold) back = true;
            else if (v.y < -turnThreshold) back = false;
        }
        SetBack(back);
    }

    void SetBack(bool back)
    {
        if (back == facingBack) return;
        facingBack = back;

        RuntimeAnimatorController target = frontController;
        if (back && stats != null && stats.data != null && stats.data.backAnimator != null)
            target = stats.data.backAnimator;
        if (target == null || anim.runtimeAnimatorController == target) return;

        // меняем контроллер, но продолжаем анимацию с того же места
        AnimatorStateInfo st = anim.GetCurrentAnimatorStateInfo(0);
        float speed = anim.GetFloat(SpeedHash);
        anim.runtimeAnimatorController = target;
        anim.SetFloat(SpeedHash, speed);
        anim.Play(st.fullPathHash, 0, st.normalizedTime % 1f);
    }
}