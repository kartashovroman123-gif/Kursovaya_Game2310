using UnityEngine;

// Ключ: подобрал - +1 ключ в забеге (RunState.Keys). Ключами отпираются двери KeyDoor.
public class KeyPickup : Pickup
{
    public int amount = 1;

    protected override bool Collect(GameObject player)
    {
        RunState.AddKey(amount);
        return true;
    }
}
