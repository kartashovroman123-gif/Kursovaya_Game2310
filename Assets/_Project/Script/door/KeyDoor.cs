using System.Collections;
using UnityEngine;

// Дверь на ключ (нижний уровень: Door -> LockedDoor -> KeyDoor).
// Ведёт в сокровищницу (Vault). На створке висит замок. Появляется, только если у героя есть ключ (решает RoomManager).
// После зачистки: подойти и нажать F - ключ тратится, замок открывается и падает, дверь открывается.
public class KeyDoor : LockedDoor
{
    SpriteRenderer lockRenderer;
    Sprite[] lockFrames;

    protected override void OnInit()
    {
        lockFrames = Style.lockFrames != null ? Style.lockFrames.Get(Side) : null;
        if (lockFrames == null || lockFrames.Length == 0) return;

        var go = new GameObject("Lock");
        go.transform.SetParent(transform, false);
        lockRenderer = go.AddComponent<SpriteRenderer>();
        lockRenderer.sprite = lockFrames[0];
        lockRenderer.flipX = Renderer.flipX;
        lockRenderer.sortingOrder = Renderer.sortingOrder + 1;
        if (Renderer.sharedMaterial != null) lockRenderer.sharedMaterial = Renderer.sharedMaterial;
    }

    protected override bool CanUnlock(GameObject player) => RunState.Keys > 0;

    protected override void OnUnlock(GameObject player)
    {
        RunState.TryUseKey();
        CameraShake.Kick(Side.Inward() * 0.05f);
    }

    protected override IEnumerator UnlockEffect()
    {
        if (lockRenderer == null) yield break;
        for (int i = 1; i < lockFrames.Length; i++)
        {
            lockRenderer.sprite = lockFrames[i];
            yield return new WaitForSeconds(0.12f);
        }
        lockRenderer.enabled = false;
    }
}
