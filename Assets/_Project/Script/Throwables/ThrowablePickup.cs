using UnityEngine;

// Метательное оружие, лежащее на земле («связка»: тип + количество).
// Подбирается клавишей взаимодействия (F), а не касанием - чтобы в бою не путаться с метанием (E).
//   тот же тип, что в слоте - добирается до максимума, остаток остаётся лежать;
//   другой тип - заменяет оружие в слоте, старое ложится на это место.
// Префаб на каждый тип: Pickup_Kukri, Pickup_Spear... Кладётся в Contents сундука, позже - в выпадение с мобов и бочек.
// Нужен Collider2D с Is Trigger (по нему PlayerInteractor находит предмет рядом).
[RequireComponent(typeof(Collider2D))]
public class ThrowablePickup : MonoBehaviour, IInteractable
{
    [Tooltip("Какое оружие лежит")]
    public ThrowableData item;
    [Tooltip("Сколько штук")]
    [Min(1)] public int amount = 3;

    [Header("Вид")]
    [Tooltip("Картинка на земле (пусто = SpriteRenderer на этом объекте)")]
    public SpriteRenderer sr;
    [Tooltip("Высота покачивания, в клетках")]
    public float bobHeight = 0.05f;
    public float bobSpeed = 3f;
    [Tooltip("Высота значка клавиши над предметом, в клетках")]
    public float promptHeight = 0.7f;

    Vector3 spriteBase;
    float born;

    void Awake()
    {
        if (sr == null) sr = GetComponentInChildren<SpriteRenderer>();
        if (sr != null) spriteBase = sr.transform.localPosition;
        born = Time.time;
    }

    void Update()
    {
        // покачивается только картинка: коллайдер и значок стоят на месте
        if (sr != null && sr.transform != transform)
            sr.transform.localPosition = spriteBase + Vector3.up * (Mathf.Sin((Time.time - born) * bobSpeed) * bobHeight);
    }

    // ----- Взаимодействие (клавиша F) -----
    public bool CanInteract(GameObject player)
    {
        if (item == null || amount <= 0) return false;
        PlayerThrower thrower = player.GetComponent<PlayerThrower>();
        return thrower != null && thrower.CanTake(item);
    }

    public void Interact(GameObject player)
    {
        PlayerThrower thrower = player.GetComponent<PlayerThrower>();
        if (thrower == null || !CanInteract(player)) return;
        int left = thrower.Take(item, amount, transform.position);   // старое оружие ляжет сюда же
        if (left <= 0) Destroy(gameObject);
        else amount = left;
    }

    public Vector3 PromptPosition => transform.position + Vector3.up * promptHeight;

    // Положить связку на землю (в текущую комнату - удалится вместе с ней)
    public static ThrowablePickup Spawn(ThrowablePickup prefab, ThrowableData item, int amount, Vector3 position)
    {
        if (prefab == null) return null;
        Room room = RoomManager.Instance != null ? RoomManager.Instance.Current : null;
        position.z = 0f;
        ThrowablePickup p = Instantiate(prefab, position, Quaternion.identity, room != null ? room.transform : null);
        p.item = item;
        p.amount = amount;
        return p;
    }
}
