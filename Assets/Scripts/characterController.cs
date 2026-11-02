using UnityEngine;

public class characterController : MonoBehaviour
{
    [Tooltip("Запасная скорость, если у героя нет PlayerStats или Char Data (например, тестовая сцена)")]
    [SerializeField] float fallbackSpeed = 5f;

    Rigidbody2D rb;
    PlayerStats stats;
    Vector2 input;

    // скорость каждый кадр берётся из PlayerStats: база из Char Data + все модификаторы
    float Speed => stats != null && stats.data != null ? stats.moveSpeed : fallbackSpeed;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        stats = GetComponent<PlayerStats>();
    }

    void Update()
    {
        input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        if (input.sqrMagnitude > 1f) input.Normalize();
    }

    void FixedUpdate()
    {
        rb.linearVelocity = input * Speed;
    }

    void OnDisable()
    {
        input = Vector2.zero;
        if (rb != null) rb.linearVelocity = Vector2.zero;
    }
}