using UnityEngine;

public class Door : MonoBehaviour
{
    public SpriteRenderer doorRenderer;
    public Sprite closedSprite;
    public Sprite openSprite;
    public SpriteRenderer iconRenderer;   // дочерний Icon
    public Collider2D blocker;            // дочерний Blocker (твёрдый)

    public RoomType NextType { get; private set; }
    public bool IsOpen { get; private set; }

    public void Setup(RoomType type, Sprite icon)
    {
        NextType = type;
        if (iconRenderer != null)
        {
            iconRenderer.sprite = icon;
            iconRenderer.enabled = icon != null;
        }
        SetOpen(false);
    }

    public void SetOpen(bool open)
    {
        IsOpen = open;
        if (blocker != null) blocker.enabled = !open;
        if (doorRenderer != null)
        {
            if (open && openSprite != null) doorRenderer.sprite = openSprite;
            else if (!open && closedSprite != null) doorRenderer.sprite = closedSprite;
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsOpen || !other.CompareTag("Player")) return;
        RoomManager.Instance.GoToNext(NextType);
    }
}