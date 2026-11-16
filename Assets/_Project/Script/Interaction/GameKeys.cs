using UnityEngine;

// Клавиши управления, которые игрок сможет поменять в будущем меню настроек.
// Хранятся в PlayerPrefs (настройки игрока на компьютере), поэтому сохраняются между запусками.
// Пример для меню настроек: GameKeys.Throw = KeyCode.G;
public static class GameKeys
{
    const string InteractKey = "keys.interact";
    const string ThrowKey = "keys.throw";
    static KeyCode? interact;
    static KeyCode? throwKey;

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

    // Метательное оружие: нажал - бросок, удержал - прицел. По умолчанию E
    public static KeyCode Throw
    {
        get
        {
            if (!throwKey.HasValue) throwKey = (KeyCode)PlayerPrefs.GetInt(ThrowKey, (int)KeyCode.E);
            return throwKey.Value;
        }
        set
        {
            throwKey = value;
            PlayerPrefs.SetInt(ThrowKey, (int)value);
            PlayerPrefs.Save();
        }
    }

    // Вернуть клавиши по умолчанию
    public static void ResetToDefaults()
    {
        Interact = KeyCode.F;
        Throw = KeyCode.E;
    }
}
