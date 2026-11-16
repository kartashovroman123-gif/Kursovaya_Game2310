using UnityEngine;

// БЕЗОПАСНАЯ КОМНАТА (средний уровень: Room -> SafeRoom -> StartRoom / ShopRoom / ItemRoom / UpgradeRoom / ExitRoom / HubRoom).
// Врагов нет: проходы открыты сразу. В комнате может стоять «содержимое»
// (торговец, пьедестал с предметом, алтарь улучшений) - пока это просто префаб-заглушка.
// Содержимое появляется один раз - при первом входе - и остаётся, если вернуться.
public abstract class SafeRoom : Room
{
    [Header("Что стоит в комнате (заглушка)")]
    [Tooltip("Префаб содержимого: торговец, пьедестал, алтарь. Можно оставить пустым")]
    public GameObject contentPrefab;
    [Tooltip("Где поставить. Пусто = центр комнаты")]
    public Transform contentPoint;

    protected override void OnFirstEnter()
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
