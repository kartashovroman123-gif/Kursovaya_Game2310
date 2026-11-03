using UnityEngine;

// Клавиши управления, которые игрок сможет поменять в будущем меню настроек.
// Хранятся в PlayerPrefs (настройки игрока на компьютере), поэтому сохраняются между запусками.
// Пример для меню настроек: GameKeys.Interact = KeyCode.E;
public static class GameKeys
{
    const string InteractKey = "keys.interact";
    static KeyCode? interact;

    // Клавиша взаимодействия (двери на ключ, сундуки, позже торговец и т.д.). По умолчанию F
    public static KeyCode Interact
    {
        get
        {
            if (!interact.HasValue) interact = (KeyCode)PlayerPrefs.GetInt(InteractKey, (int)KeyCode.F);
            return interact.Value;
        }
        set
        {
            interact = value;
            PlayerPrefs.SetInt(InteractKey, (int)value);
            PlayerPrefs.Save();
        }
    }

    // Вернуть клавиши по умолчанию
    public static void ResetToDefaults()
    {
        Interact = KeyCode.F;
    }
}
