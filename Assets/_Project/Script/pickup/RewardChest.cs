using System;
using UnityEngine;

// Настройки одного уровня сундука: как выглядит и что выпадает
[Serializable]
public class ChestTierSettings
{
    public ChestTier tier;
    public Sprite closedSprite;
    public Sprite openSprite;
    [Tooltip("Что выпадает всегда (пока заглушка: позже предметы, монеты)")]
    public GameObject[] contents;
    [Tooltip("Шанс, что выпадет ключ (0..1)")]
    [Range(0f, 1f)] public float keyChance;
}

// Сундук-награда. Один префаб на все уровни: обычный, элитный, хранилище, боссовый.
// Уровень ставит комната, когда создаёт сундук (CombatRoom.RewardTier -> SetTier),
// а если сундук поставлен в комнату руками - берётся поле Tier.
// Герой подходит, над сундуком появляется значок клавиши (F) - нажал, сундук открывается и выбрасывает содержимое.
// Сундук может быть ЗАПЕРТ (второй сундук элитной комнаты): тогда над ним висит замок, а открыть его
// можно, только если у героя есть ключ (ключ тратится).
// Нужен Collider2D с Is Trigger. Если сундук должен быть твёрдым - добавь дочерний объект с обычным коллайдером.
[RequireComponent(typeof(Collider2D))]
public class RewardChest : MonoBehaviour, IInteractable
{
    public SpriteRenderer sr;
    [Tooltip("Уровень, если сундук поставлен руками. Сундук-награду комната переставит сама")]
    public ChestTier tier = ChestTier.Normal;

    [Header("Уровни")]
    public ChestTierSettings[] tiers =
    {
        new ChestTierSettings { tier = ChestTier.Normal, keyChance = 0.3f },
        new ChestTierSettings { tier = ChestTier.Elite,  keyChance = 0.6f },
        new ChestTierSettings { tier = ChestTier.Vault,  keyChance = 0.2f },
        new ChestTierSettings { tier = ChestTier.Boss,   keyChance = 0f },
    };

    [Header("Общее")]
    [Tooltip("Префаб ключа")]
    public KeyPickup keyPrefab;
    [Tooltip("На каком расстоянии от сундука появляется содержимое, в клетках")]
    public float dropDistance = 0.8f;
    [Tooltip("Высота значка клавиши над сундуком, в клетках")]
    public float promptHeight = 0.8f;

    [Header("Замок (запертый сундук)")]
    [Tooltip("Картинка замка над запертым сундуком (Chest_Lock)")]
    public Sprite lockSprite;
    [Tooltip("Где висит замок относительно сундука, в клетках")]
    public Vector2 lockOffset = new Vector2(0f, 0.15f);

    bool opened;
    bool needsKey;
    SpriteRenderer lockIcon;

    public ChestTier Tier => tier;
    public bool NeedsKey => needsKey;

    // Запереть / отпереть (вызывает комната сразу после создания)
    public void SetNeedsKey(bool value)
    {
        needsKey = value;
        if (needsKey && lockIcon == null && lockSprite != null)
        {
            var go = new GameObject("Lock");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = lockOffset;
            lockIcon = go.AddComponent<SpriteRenderer>();
            lockIcon.sprite = lockSprite;
            if (sr != null)
            {
                lockIcon.sortingLayerID = sr.sortingLayerID;
                lockIcon.sortingOrder = sr.sortingOrder + 1;
            }
        }
        if (lockIcon != null) lockIcon.gameObject.SetActive(needsKey);
    }

    void Awake()
    {
        if (sr == null) sr = GetComponent<SpriteRenderer>();
        ApplyLook();
    }

    // Вызывает комната сразу после создания сундука
    public void SetTier(ChestTier newTier)
    {
        tier = newTier;
        ApplyLook();
    }

    ChestTierSettings Settings
    {
        get
        {
            if (tiers != null)
                foreach (ChestTierSettings t in tiers)
                    if (t != null && t.tier == tier) return t;
            return null;
        }
    }

    void ApplyLook()
    {
        ChestTierSettings s = Settings;
        if (sr == null || s == null) return;
        Sprite sprite = opened ? s.openSprite : s.closedSprite;
        if (sprite != null) sr.sprite = sprite;
    }

    // ----- Взаимодействие (клавиша F) -----
    // Запертый - только если есть ключ (без ключа значок F не появится, замок остаётся висеть)
    public bool CanInteract(GameObject player) => !opened && (!needsKey || RunState.Keys > 0);

    public void Interact(GameObject player)
    {
        if (!CanInteract(player)) return;
        if (needsKey)
        {
            if (!RunState.TryUseKey()) return;
            SetNeedsKey(false);
        }
        Open();
    }
    public Vector3 PromptPosition => transform.position + Vector3.up * promptHeight;

    void Open()
    {
        opened = true;
        ApplyLook();
        CameraShake.Kick(Vector2.up * (tier == ChestTier.Normal ? 0.05f : 0.1f));

        ChestTierSettings s = Settings;
        int n = 0;
        if (s != null)
        {
            if (s.contents != null)
                foreach (GameObject c in s.contents) Drop(c, n++);
            if (keyPrefab != null && UnityEngine.Random.value < s.keyChance) Drop(keyPrefab.gameObject, n++);
        }
        if (n == 0) Debug.Log($"Сундук ({tier}) открыт: награды пока нет (заглушка)");
    }

    // Раскладываем выпавшее веером под сундуком
    void Drop(GameObject prefab, int index)
    {
        if (prefab == null) return;
        float angle = -90f + (index % 2 == 0 ? 1 : -1) * 35f * ((index + 1) / 2);
        Vector2 dir = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));
        Vector3 pos = transform.position + (Vector3)(dir * dropDistance);
        Instantiate(prefab, pos, Quaternion.identity, transform.parent);   // в комнату: удалится вместе с ней
    }
}
