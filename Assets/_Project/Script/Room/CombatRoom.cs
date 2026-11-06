using System.Collections;
using UnityEngine;

// БОЕВАЯ КОМНАТА (средний уровень: Room -> CombatRoom -> EliteRoom / VaultRoom / BossRoom).
// При входе двери закрыты. Враги появляются в точках EnemySpawnPoint внутри префаба комнаты.
// Комната сама следит за врагами: когда последний враг уничтожен - двери открываются и появляется награда.
// Награда - сундук; его уровень (обычный, элитный, хранилище, боссовый) задаёт класс комнаты (RewardTier).
// Друзьям ничего вызывать не нужно: достаточно, чтобы враг при смерти удалялся (Destroy).
// Если враг не удаляется, а, например, выключается - пусть вызовет GetComponent<RoomEnemy>().ReportDeath().
// Пока врагов нет (в точках не стоят префабы), комната проходится клавишей K.
public class CombatRoom : Room
{
    [Header("Награда после зачистки")]
    [Tooltip("Сундук (префаб с RewardChest), появляется, когда комната пройдена. Можно оставить пустым")]
    public GameObject rewardPrefab;
    [Tooltip("Где. Пусто = центр комнаты")]
    public Transform rewardPoint;

    // Уровень сундука-награды. Потомки меняют: элитная - Elite, за ключом - Vault, босс - Boss
    public virtual ChestTier RewardTier => ChestTier.Normal;

    int alive;          // живых врагов
    int pending;        // точек, где враг ещё не появился
    bool spawnedAny;

    protected override void OnEnter()
    {
        alive = 0;
        pending = 0;
        spawnedAny = false;
        foreach (EnemySpawnPoint point in GetComponentsInChildren<EnemySpawnPoint>())
        {
            pending++;
            StartCoroutine(SpawnAt(point));
        }
    }

    IEnumerator SpawnAt(EnemySpawnPoint point)
    {
        if (point.delay > 0f) yield return new WaitForSeconds(point.delay);
        pending--;

        GameObject prefab = point.PickPrefab();
        if (prefab != null)
        {
            if (point.spawnEffect != null)
                Instantiate(point.spawnEffect, point.transform.position, Quaternion.identity, transform);
            GameObject enemy = Instantiate(prefab, point.transform.position, Quaternion.identity, transform);
            RegisterEnemy(enemy);
        }
        CheckCleared();
    }

    // ---------- Учёт врагов ----------

    // Подписать врага на эту комнату: когда он будет уничтожен, комната узнает сама
    public void RegisterEnemy(GameObject enemy)
    {
        if (enemy == null) return;
        RoomEnemy tag = enemy.GetComponent<RoomEnemy>();
        if (tag == null) tag = enemy.AddComponent<RoomEnemy>();
        tag.room = this;
        alive++;
        spawnedAny = true;
    }

    // Вызывается из RoomEnemy, когда враг погиб
    public void EnemyDied()
    {
        alive = Mathf.Max(0, alive - 1);
        CheckCleared();
    }

    void CheckCleared()
    {
        // проходим, только если враги были и все погибли (пустая комната ждёт клавишу K)
        if (spawnedAny && alive == 0 && pending == 0) Clear();
    }

    // ---------- Награда ----------

    protected override void OnCleared()
    {
        GameObject reward = Spawn(rewardPrefab, rewardPoint);
        RewardChest chest = reward != null ? reward.GetComponent<RewardChest>() : null;
        if (chest != null) chest.SetTier(RewardTier);
    }
}
