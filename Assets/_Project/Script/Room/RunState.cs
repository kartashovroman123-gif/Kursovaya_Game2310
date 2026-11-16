using System;

// Состояние текущего забега: ключи и сколько комнат пройдено после хаба.
// Статический класс: на сцену ставить не нужно, данные доступны из любого скрипта (RunState.Keys).
// Сбрасывается, когда герой попадает в хаб (HubRoom).
public static class RunState
{
    public static int Keys { get; private set; }
    public static int RoomsPassed { get; private set; }

    // Срабатывает при любом изменении (для интерфейса)
    public static event Action Changed;
    // Начался новый забег (герой в хабе): то, что живёт один забег, очищается (метательное оружие и т.п.)
    public static event Action RunReset;

    public static void ResetRun()
    {
        Keys = 0;
        RoomsPassed = 0;
        RunReset?.Invoke();
        Changed?.Invoke();
    }

    public static void RoomPassed()
    {
        RoomsPassed++;
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
