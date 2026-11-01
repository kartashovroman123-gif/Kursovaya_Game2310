// Все параметры героя, которые могут менять пассивки, предметы и усиления.
// ВАЖНО: числа у существующих значений не меняй, иначе собьются уже настроенные ассеты.
// Новые параметры добавляй с новыми, ещё не занятыми числами.
public enum StatType
{
    // Общие
    MaxHealth = 0,
    MoveSpeed = 1,
    InvulnerabilityTime = 2,   // неуязвимость после получения урона, сек
    AllDamage = 3,             // общий множитель урона всех приёмов (база 1 = 100%)

    // Слабая атака
    LightDamage = 10,
    LightCooldown = 11,
    LightRange = 12,

    // Сильная атака
    HeavyDamage = 20,
    HeavyCooldown = 21,
    HeavyRange = 22,

    // Ультимейт
    UltimateDamage = 30,
    UltimateCooldown = 31,     // не используется: ультимейт работает от заряда
    UltimateRange = 32,
    UltimateChargePerSecond = 35,
    UltimateChargePerHit = 36,

    // Приём передвижения
    MovementDamage = 40,
    MovementCooldown = 41,
    MovementRange = 42,        // для рывка это дистанция

    // Активный предмет
    ActiveItemCooldown = 50,
}
