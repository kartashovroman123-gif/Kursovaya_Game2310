using UnityEngine;

// Всё, что нужно предмету для броска: откуда, куда, кто бросил
public struct ThrowContext
{
    public GameObject owner;         // герой (в него снаряды не попадают)
    public Vector2 origin;           // откуда летит (грудь героя)
    public Vector2 direction;        // куда (на курсор), длина 1
    public float damageMultiplier;   // общий множитель урона героя (AllDamage)
    public int sortingLayerID;       // слой отрисовки снарядов (как у героя)
    public int sortingOrder;
    public Transform parent;         // текущая комната: снаряды удалятся вместе с ней
}

// МЕТАТЕЛЬНОЕ ОРУЖИЕ (средний уровень: GameItemData -> ThrowableData -> ProjectileThrowable -> ...).
// Лежит в отдельном слоте героя (PlayerThrower), не связанном с активным и пассивным предметом.
// В слоте - один тип и его количество. Каждый бросок тратит одну штуку.
// Здесь общее для любого метательного: сколько можно нести, урон, пауза между бросками, цвет прицела.
// Как именно бросается и как выглядит прицел - решают потомки (Throw, DrawAim).
public abstract class ThrowableData : GameItemData
{
    [Header("Запас")]
    [Tooltip("Сколько штук можно нести (баффы прибавляют параметром ThrowableCapacity)")]
    [Min(1)] public int maxCount = 8;
    [Tooltip("Как предмет лежит на земле: префаб с ThrowablePickup (нужен, чтобы выбросить старое оружие при замене)")]
    public ThrowablePickup pickupPrefab;

    [Header("Бросок")]
    [Tooltip("Урон одного снаряда")]
    public float damage = 10f;
    [Tooltip("Пауза между бросками, сек")]
    public float throwCooldown = 0.35f;
    [Tooltip("Толчок камеры при броске, в клетках")]
    public float throwKick = 0.04f;

    [Header("Прицел")]
    public Color aimColor = new Color(1f, 1f, 1f, 0.85f);
    [Tooltip("Длина прицела, в клетках (дальше - если ни во что не упрётся)")]
    public float aimLength = 6f;

    // Бросить (одна штука из запаса - её списывает PlayerThrower)
    public abstract void Throw(ThrowContext ctx);

    // Нарисовать прицел этого оружия. Вызывается каждый кадр, пока игрок держит кнопку броска
    public abstract void DrawAim(ThrowAim aim, Vector2 origin, Vector2 direction, GameObject owner);
}
