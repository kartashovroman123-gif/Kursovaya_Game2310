using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class LobbyUI : MonoBehaviour
{
    [SerializeField] CharacterData[] characters;
    [SerializeField] Transform cardsParent;
    [SerializeField] HeroCard cardPrefab;
    [SerializeField] TMP_Text infoText;
    [SerializeField] Button startButton;
    [SerializeField] string gameSceneName = "Game";

    void Start()
    {
        startButton.interactable = false;
        startButton.onClick.AddListener(() => SceneManager.LoadScene(gameSceneName));

        foreach (var c in characters)
            Instantiate(cardPrefab, cardsParent).Setup(c, Select);
    }

    void Select(CharacterData data)
    {
        GameSession.SelectedCharacter = data;
        infoText.text = $"{data.displayName}\nHP {data.maxHealth}   Speed {data.moveSpeed}   Dmg {data.damage}";
        startButton.interactable = true;
    }
}