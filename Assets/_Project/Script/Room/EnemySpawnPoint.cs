using UnityEngine;

// Точка появления врага. Ставится пустым объектом внутри префаба боевой комнаты.
// Когда герой входит, боевая комната создаёт здесь врага (случайного из списка).
// В окне Scene точка видна красным кружком.
public class EnemySpawnPoint : MonoBehaviour
{
    [Tooltip("Префабы врагов: появится один случайный. Пусто = в этой точке никого")]
    public GameObject[] enemyPrefabs;
    [Tooltip("Через сколько секунд после входа появится враг")]
    public float delay = 0.5f;
    [Tooltip("Эффект появления (необязательно)")]
    public GameObject spawnEffect;

    public GameObject PickPrefab()
    {
        if (enemyPrefabs == null || enemyPrefabs.Length == 0) return null;
        return enemyPrefabs[Random.Range(0, enemyPrefabs.Length)];
    }

    void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 0.25f, 0.25f, 0.9f);
        Gizmos.DrawWireSphere(transform.position, 0.35f);
        Gizmos.DrawLine(transform.position + Vector3.left * 0.2f, transform.position + Vector3.right * 0.2f);
        Gizmos.DrawLine(transform.position + Vector3.down * 0.2f, transform.position + Vector3.up * 0.2f);
    }
}
