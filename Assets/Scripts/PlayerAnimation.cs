using UnityEngine;

public class PlayerAnimation : MonoBehaviour
{
    Rigidbody2D rb;
    Animator anim;
    SpriteRenderer sr;
    Vector2 lastPos;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        sr = GetComponent<SpriteRenderer>();
        lastPos = rb.position;
    }

    void FixedUpdate()
    {
        Vector2 pos = rb.position;
        Vector2 v = (pos - lastPos) / Time.fixedDeltaTime;
        lastPos = pos;

        anim.SetFloat("Speed", v.magnitude);

        if (v.x > 0.05f) sr.flipX = false;
        else if (v.x < -0.05f) sr.flipX = true;
    }
}