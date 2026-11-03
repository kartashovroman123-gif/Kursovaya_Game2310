// Хаб: отсюда начинается забег. Вход в хаб сбрасывает забег (ключи и счётчик комнат).
// Входной двери нет - герой появляется в Player Spawn. Дверь к боссу здесь не появляется.
public class HubRoom : SafeRoom
{
    public override void OnLoaded() => RunState.ResetRun();   // не засчитывается как пройденная комната
    public override bool HasEntrance => false;
    public override bool AllowsBossDoor => false;
}
