using UnityEngine;

// Оставляет за героем язычки пламени, пока идёт рывок.
// Вешать вручную не нужно: ShadowDash сам добавит этот компонент герою при первом рывке.
public class DashTrail : MonoBehaviour
{
    FlamePuff prefab;
    float interval;
    float until;
    float timer;
    Vector2 offset;

    public void Run(FlamePuff flamePrefab, float spawnInterval, float duration, Vector2 spawnOffset)
    {
        prefab = flamePrefab;
        interval = Mathf.Max(0.01f, spawnInterval);
        until = Time.time + duration;
        offset = spawnOffset;
        timer = 0f;
        Spawn();                    // первый язычок сразу, на месте старта
    }

    void Update()
    {
        if (prefab == null || Time.time >= until) return;
        timer += Time.deltaTime;
        while (timer >= interval)
        {
            timer -= interval;
            Spawn();
        }
    }

    void Spawn()
    {
        Vector2 jitter = Random.insideUnitCircle * 0.08f;   // чуть разбросать, чтобы след был живой
        Vector3 pos = transform.position + (Vector3)(offset + jitter);
        Instantiate(prefab, pos, Quaternion.identity);
    }
}
