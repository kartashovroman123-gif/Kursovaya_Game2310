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

    public static void ResetRun()
    {
        Keys = 0;
        RoomsPassed = 0;
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
