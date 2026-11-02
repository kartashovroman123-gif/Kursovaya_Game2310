using UnityEngine;

// ОБЩАЯ ОСНОВА ПОДБИРАЕМЫХ ВЕЩЕЙ (Pickup -> KeyPickup, позже монеты, сердечки...).
// Покачивается вверх-вниз; когда герой касается - срабатывает Collect и вещь исчезает.
// Нужен Collider2D с галочкой Is Trigger на этом же объекте.
[RequireComponent(typeof(Collider2D))]
public abstract class Pickup : MonoBehaviour
{
    [Tooltip("Высота покачивания, в клетках")]
    public float bobHeight = 0.06f;
    [Tooltip("Скорость покачивания")]
    public float bobSpeed = 3f;
    [Tooltip("Сколько секунд после появления нельзя подобрать (чтобы было видно, что выпало)")]
    public float pickupDelay = 0.4f;

    Vector3 basePos;
    float born;

    protected virtual void Awake()
    {
        basePos = transform.position;
        born = Time.time;
    }

    void Update()
    {
        transform.position = basePos + Vector3.up * (Mathf.Sin((Time.time - born) * bobSpeed) * bobHeight);
    }

    // Что даёт вещь. true - подобрана (исчезает)
    protected abstract bool Collect(GameObject player);

    void OnTriggerEnter2D(Collider2D other) => TryCollect(other);
    void OnTriggerStay2D(Collider2D other) => TryCollect(other);

    void TryCollect(Collider2D other)
    {
        if (Time.time - born < pickupDelay || !other.CompareTag("Player")) return;
        if (Collect(other.gameObject)) Destroy(gameObject);
    }
}
