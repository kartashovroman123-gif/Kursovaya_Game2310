using System;

// Состояние текущего забега: номер уровня и ключи.
// Статический класс: на сцену ставить не нужно, данные доступны из любого скрипта (RunState.Keys).
// Сбрасывается в начале забега (RoomManager, уровень 1).
public static class RunState
{
    public static int Keys { get; private set; }
    public static int Level { get; private set; }

    // Срабатывает при любом изменении (для интерфейса)
    public static event Action Changed;
    // Начался новый забег: то, что живёт один забег, очищается (метательное оружие и т.п.)
    public static event Action RunReset;

    public static void ResetRun()
    {
        Keys = 0;
        Level = 0;
        RunReset?.Invoke();
        Changed?.Invoke();
    }

    public static void SetLevel(int level)
    {
        Level = level;
        Changed?.Invoke();
    }

    public static void AddKey(int amount = 1)
    {
        Keys += Math.Max(0, amount);
        Changed?.Invoke();
    }

    // Потратить ключ. false - ключей нет
    public static bool TryUseKey()
    {
        if (Keys <= 0) return false;
        Keys--;
        Changed?.Invoke();
        return true;
    }
}
