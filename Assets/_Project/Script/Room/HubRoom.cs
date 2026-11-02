// Хаб: отсюда начинается забег. Вход в хаб сбрасывает забег (ключи и счётчик комнат).
// Дверь к боссу здесь не появляется.
public class HubRoom : SafeRoom
{
    public override void OnLoaded() => RunState.ResetRun();   // не засчитывается как пройденная комната
    public override bool AllowsBossDoor => false;
}
