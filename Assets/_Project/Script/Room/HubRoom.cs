using UnityEngine;

// Хаб: отсюда начинается забег. Вход в хаб сбрасывает забег (ключи и счётчик комнат).
// Входной двери нет - герой появляется в Player Spawn. Дверь к боссу здесь не появляется.
// Из хаба выходы ведут только в обычную боевую комнату (мягкий старт).
public class HubRoom : SafeRoom
{
    static readonly RoomType[] firstRoom = { RoomType.Combat };

    public override void OnLoaded() => RunState.ResetRun();   // не засчитывается как пройденная комната
    public override bool HasEntrance => false;
    public override bool AllowsBossDoor => false;
    public override RoomType[] FixedNext => firstRoom;
}
