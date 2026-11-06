using System.Collections;
using UnityEngine;

// Вход: дверь, через которую герой пришёл (Door -> EntranceDoor).
// Стоит на стороне, противоположной выходу из прошлой комнаты. Сначала открыта,
// через мгновение после входа захлопывается (пыль, толчок камеры) и больше не открывается - назад дороги нет.
public class EntranceDoor : Door
{
    protected override Sprite[] GetFrames() => Style != null && Style.entrance != null ? Style.entrance.Get(Side) : null;

    protected override void OnInit()
    {
        if (Trigger != null) Trigger.enabled = false;     // через вход не пройти
        SetFrame(LastFrame);                               // герой только что вошёл - дверь открыта
        StartCoroutine(Slam());
    }

    IEnumerator Slam()
    {
        yield return new WaitForSeconds(Style.slamDelay);
        yield return PlayFrames(false);                    // кадры в обратном порядке: захлопывается
        CameraShake.Kick(Side.Inward() * Style.slamShake);
        if (Style.slamDustPrefab != null)
            Instantiate(Style.slamDustPrefab, transform.position, Quaternion.identity, transform.parent);
    }

    public override void OnRoomCleared() { }               // не открывается
    public override bool CanInteract(GameObject player) => false;   // назад дороги нет
}
