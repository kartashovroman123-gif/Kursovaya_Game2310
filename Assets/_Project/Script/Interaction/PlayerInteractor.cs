using UnityEngine;

// Взаимодействие героя с предметами. Вешается на префаб героя.
// Каждый кадр ищет рядом ближайший объект, с которым сейчас можно взаимодействовать (IInteractable),
// показывает над ним значок клавиши и по нажатию (GameKeys.Interact, по умолчанию F) вызывает действие.
public class PlayerInteractor : MonoBehaviour
{
    [Tooltip("Радиус поиска вокруг героя, в клетках")]
    public float radius = 1.4f;
    [Tooltip("Картинка значка клавиши (Prompt_F)")]
    public Sprite promptSprite;
    [Tooltip("Order in Layer значка (поверх всего)")]
    public int promptSortingOrder = 50;
    [Tooltip("Размер значка")]
    public float promptScale = 1f;

    IInteractable current;
    SpriteRenderer prompt;
    PlayerAbilities abilities;

    public IInteractable Current => current;

    void Awake()
    {
        abilities = GetComponent<PlayerAbilities>();
        var go = new GameObject("InteractPrompt");
        prompt = go.AddComponent<SpriteRenderer>();
        prompt.sprite = promptSprite;
        prompt.sortingOrder = promptSortingOrder;
        go.transform.localScale = Vector3.one * promptScale;
        go.SetActive(false);
    }

    void OnDestroy()
    {
        if (prompt != null) Destroy(prompt.gameObject);
    }

    void Update()
    {
        bool locked = abilities != null && abilities.ControlLocked;
        current = locked ? null : FindNearest();

        if (current == null)
        {
            prompt.gameObject.SetActive(false);
            return;
        }

        prompt.gameObject.SetActive(true);
        float bob = Mathf.Sin(Time.time * 4f) * 0.05f;
        prompt.transform.position = current.PromptPosition + Vector3.up * bob;

        if (Input.GetKeyDown(GameKeys.Interact))
            current.Interact(gameObject);
    }

    IInteractable FindNearest()
    {
        Collider2D[] found = Physics2D.OverlapCircleAll(transform.position, radius);
        IInteractable best = null;
        float bestDist = float.MaxValue;
        for (int i = 0; i < found.Length; i++)
        {
            IInteractable it = found[i].GetComponentInParent<IInteractable>();
            if (it == null || !it.CanInteract(gameObject)) continue;
            float d = ((Vector2)found[i].bounds.center - (Vector2)transform.position).sqrMagnitude;
            if (d < bestDist) { bestDist = d; best = it; }
        }
        return best;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 1f, 1f, 0.5f);
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}
