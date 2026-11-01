using UnityEngine;

// Кнопки приёмов. Направление удара — к курсору мыши,
// направление рывка — по нажатым стрелкам/WASD (если не нажаты — к курсору).
[RequireComponent(typeof(PlayerStats))]
public class PlayerAbilityInput : MonoBehaviour
{
    public KeyCode lightKey = KeyCode.Mouse0;
    public KeyCode heavyKey = KeyCode.Mouse1;
    public KeyCode ultimateKey = KeyCode.Q;
    public KeyCode movementKey = KeyCode.Space;
    public KeyCode activeItemKey = KeyCode.F;

    PlayerStats stats;

    void Awake()
    {
        stats = GetComponent<PlayerStats>();
    }

    void Update()
    {
        if (Input.GetKeyDown(lightKey)) stats.TryUse(AbilitySlot.Light, AimDirection());
        if (Input.GetKeyDown(heavyKey)) stats.TryUse(AbilitySlot.Heavy, AimDirection());
        if (Input.GetKeyDown(ultimateKey)) stats.TryUse(AbilitySlot.Ultimate, AimDirection());
        if (Input.GetKeyDown(movementKey)) stats.TryUse(AbilitySlot.Movement, MoveDirection());
        if (Input.GetKeyDown(activeItemKey)) stats.TryUseActiveItem(AimDirection());
    }

    Vector2 AimDirection()
    {
        Camera cam = Camera.main;
        if (cam == null) return Vector2.right;
        Vector3 mouse = cam.ScreenToWorldPoint(Input.mousePosition);
        Vector2 d = (Vector2)(mouse - transform.position);
        return d.sqrMagnitude < 0.0001f ? Vector2.right : d.normalized;
    }

    Vector2 MoveDirection()
    {
        var d = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        return d.sqrMagnitude > 0.01f ? d.normalized : AimDirection();
    }
}
