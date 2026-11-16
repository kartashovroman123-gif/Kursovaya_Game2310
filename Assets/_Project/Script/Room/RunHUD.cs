using UnityEngine;
using UnityEngine.UI;

// Счётчики забега на экране: ключи и номер уровня.
// Вешается на объект внутри Canvas (HUD). Тексты - UI -> Legacy -> Text.
public class RunHUD : MonoBehaviour
{
    [Tooltip("Текст рядом с иконкой ключа")]
    public Text keysText;
    [Tooltip("Текст «Уровень 2 / 5» (необязательно)")]
    public Text roomText;

    void OnEnable()
    {
        RunState.Changed += Refresh;
        Refresh();
    }

    void OnDisable() { RunState.Changed -= Refresh; }

    void Refresh()
    {
        if (keysText != null) keysText.text = "x" + RunState.Keys;
        if (roomText != null)
        {
            RunSettings s = RoomManager.Instance != null ? RoomManager.Instance.settings : null;
            int total = s != null ? s.BossLevelNumber : 0;
            roomText.text = s != null && s.IsBossLevel(RunState.Level) ? "Уровень босса" : $"Уровень {RunState.Level} / {total}";
        }
    }
}
