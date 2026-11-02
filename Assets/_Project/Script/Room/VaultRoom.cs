using UnityEngine;

// Сокровищница: особая комната за дверью на ключ. Врагов нет, стоят несколько сундуков в ряд.
public class VaultRoom : SafeRoom
{
    [Header("Сундуки сокровищницы")]
    public RewardChest chestPrefab;
    public int chestCount = 2;
    [Tooltip("Расстояние между сундуками, в клетках")]
    public float chestSpacing = 1.5f;

    protected override void SpawnContents()
    {
        base.SpawnContents();
        if (chestPrefab == null) return;
        float start = -(chestCount - 1) * chestSpacing * 0.5f;    // ряд по центру
        for (int i = 0; i < chestCount; i++)
            Spawn(chestPrefab.gameObject, contentPoint, new Vector2(start + i * chestSpacing, 0f));
    }
}
