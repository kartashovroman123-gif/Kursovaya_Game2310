using UnityEngine;
using UnityEngine.UI;

// Счётчики забега на экране: ключи и (для теста) номер комнаты до босса.
// Вешается на объект внутри Canvas (HUD). Тексты - UI -> Legacy -> Text.
public class RunHUD : MonoBehaviour
{
    [Tooltip("Текст рядом с иконкой ключа")]
    public Text keysText;
    [Tooltip("Текст «Комната 3 / 8» (необязательно, удобно для теста)")]
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
            int total = RoomManager.Instance != null ? RoomManager.Instance.roomsBeforeBoss : 0;
            roomText.text = RunState.RoomsPassed == 0 ? "Хаб" : $"Комната {RunState.RoomsPassed} / {total}";
        }
    }
}
