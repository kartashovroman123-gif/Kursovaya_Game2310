using UnityEngine;

public class PlayerSpawner : MonoBehaviour
{
    [SerializeField] CharacterData defaultCharacter;   // для тестов без лобби
    [SerializeField] Transform spawnPoint;

    void Start()
    {
        var data = GameSession.SelectedCharacter != null
            ? GameSession.SelectedCharacter
            : defaultCharacter;

        var player = Instantiate(data.prefab, spawnPoint.position, Quaternion.identity);
        if (player.TryGetComponent(out PlayerStats stats)) stats.Init(data);
    }
}