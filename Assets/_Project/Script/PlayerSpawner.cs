using UnityEngine;

public class PlayerSpawner : MonoBehaviour
{
    public CharacterData defaultCharacter;
    public Transform spawnPoint;   // необязательно: если пусто, герой создаётся в (0,0,0)

    void Start()
    {
        CharacterData data = GameSession.SelectedCharacter != null
            ? GameSession.SelectedCharacter
            : defaultCharacter;

        if (data == null || data.prefab == null)
        {
            Debug.LogError("PlayerSpawner: не задан персонаж или его префаб");
            return;
        }

        Vector3 pos = spawnPoint != null ? spawnPoint.position : Vector3.zero;
        GameObject hero = Instantiate(data.prefab, pos, Quaternion.identity);

        var stats = hero.GetComponent<PlayerStats>();
        if (stats != null) stats.Init(data);
    }
}