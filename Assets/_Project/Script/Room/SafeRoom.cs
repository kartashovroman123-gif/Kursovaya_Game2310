using UnityEngine;

// БЕЗОПАСНАЯ КОМНАТА (средний уровень: Room -> SafeRoom -> HubRoom / ShopRoom / ItemRoom / UpgradeRoom / VaultRoom).
// Врагов нет: двери открываются сразу при входе. В комнате может стоять «содержимое»
// (торговец, пьедестал с предметом, алтарь улучшений) - пока это просто префаб-заглушка.
public abstract class SafeRoom : Room
{
    [Header("Что стоит в комнате (заглушка)")]
    [Tooltip("Префаб содержимого: торговец, пьедестал, алтарь. Можно оставить пустым")]
    public GameObject contentPrefab;
    [Tooltip("Где поставить. Пусто = центр комнаты")]
    public Transform contentPoint;

    protected override void OnEnter()
    {
        Clear();
        SpawnContents();
    }

    // Потомки могут поставить что-то своё
    protected virtual void SpawnContents()
    {
        Spawn(contentPrefab, contentPoint);
    }
}
