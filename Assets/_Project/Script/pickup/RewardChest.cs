using UnityEngine;

// Сундук-награда (заглушка). Герой подходит, над сундуком появляется значок клавиши (F) - нажал,
// сундук открывается и выбрасывает рядом то, что в нём лежит.
// Пока содержимое - любые префабы (например, ключ). Позже сюда добавятся предметы и монеты.
// Нужен Collider2D с Is Trigger. Если сундук должен быть твёрдым - добавь дочерний объект с обычным коллайдером.
[RequireComponent(typeof(Collider2D))]
public class RewardChest : MonoBehaviour, IInteractable
{
    public SpriteRenderer sr;
    public Sprite closedSprite;
    public Sprite openSprite;

    [Header("Содержимое")]
    [Tooltip("Что выпадает всегда")]
    public GameObject[] contents;
    [Tooltip("Префаб ключа")]
    public KeyPickup keyPrefab;
    [Tooltip("Шанс, что выпадет ключ (0..1)")]
    [Range(0f, 1f)] public float keyChance = 0.5f;
    [Tooltip("На каком расстоянии от сундука появляется содержимое, в клетках")]
    public float dropDistance = 0.8f;

    bool opened;

    void Awake()
    {
        if (sr == null) sr = GetComponent<SpriteRenderer>();
        if (sr != null && closedSprite != null) sr.sprite = closedSprite;
    }

    // ----- Взаимодействие (клавиша F) -----
    public bool CanInteract(GameObject player) => !opened;
    public void Interact(GameObject player) { if (!opened) Open(); }
    [Tooltip("Высота значка клавиши над сундуком, в клетках")]
    public float promptHeight = 0.8f;
    public Vector3 PromptPosition => transform.position + Vector3.up * promptHeight;

    void Open()
    {
        opened = true;
        if (sr != null && openSprite != null) sr.sprite = openSprite;
        CameraShake.Kick(Vector2.up * 0.05f);

        int n = 0;
        if (contents != null)
            foreach (GameObject c in contents) Drop(c, n++);
        if (keyPrefab != null && Random.value < keyChance) Drop(keyPrefab.gameObject, n++);
        if (n == 0) Debug.Log("Сундук открыт: награды пока нет (заглушка)");
    }

    // Раскладываем выпавшее веером под сундуком
    void Drop(GameObject prefab, int index)
    {
        if (prefab == null) return;
        float angle = -90f + (index % 2 == 0 ? 1 : -1) * 35f * ((index + 1) / 2);
        Vector2 dir = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));
        Vector3 pos = transform.position + (Vector3)(dir * dropDistance);
        Instantiate(prefab, pos, Quaternion.identity, transform.parent);   // в комнату: удалится вместе с ней
    }
}
