using UnityEngine;
using UnityEngine.UI;

// Иконка слота метательного оружия (CooldownIcon -> ThrowableIcon), справа снизу рядом с молотом.
// Слот виден всегда: пустой - только рамка; с оружием - картинка оружия (из ассета, поле Icon) и количество.
// Между бросками картинка заполняется по часовой стрелке, как у молота.
// Источник (PlayerThrower на герое) находится сам.
public class ThrowableIcon : CooldownIcon
{
    [Tooltip("Слот героя (пусто = найдётся сам)")]
    public PlayerThrower source;

    [Header("Количество")]
    [Tooltip("Цифра десятков (видна от 10 штук)")]
    public Image tensImage;
    [Tooltip("Цифра единиц")]
    public Image onesImage;
    [Tooltip("Картинки цифр 0..9 по порядку (Icon_Num_0 ... Icon_Num_9)")]
    public Sprite[] numberSprites = new Sprite[10];
    [Tooltip("Цвет цифры, когда запас полный")]
    public Color fullColor = new Color(0.75f, 0.6f, 1f);
    [Tooltip("Цвет цифры, когда осталась последняя штука")]
    public Color lastColor = new Color(1f, 0.45f, 0.45f);

    [Header("Пустой слот")]
    [Tooltip("Цвет серой картинки (во время паузы между бросками)")]
    public Color greyTint = new Color(0.32f, 0.3f, 0.38f, 1f);
    [Tooltip("Что показывать только когда в слоте есть оружие (например, значок клавиши E). Можно оставить пустым")]
    public GameObject[] showWhenFilled;

    ThrowableData shown;
    bool first = true;

    protected override void Awake()
    {
        base.Awake();
        if (colorImage != null)
        {
            colorImage.type = Image.Type.Filled;
            colorImage.fillMethod = Image.FillMethod.Radial360;
            colorImage.fillOrigin = (int)Image.Origin360.Top;
            colorImage.fillClockwise = true;
        }
    }

    protected override void FindSource()
    {
        if (source == null) source = FindFirstObjectByType<PlayerThrower>();
    }

    // Слот виден всегда, пока есть герой (пустой - просто рамка)
    protected override bool HasAbility => source != null;

    protected override float CooldownFraction =>
        source != null && source.Current != null ? source.CooldownFraction : 0f;

    protected override void UpdateExtras()
    {
        ThrowableData item = source.Current;
        if (item != shown || first)
        {
            first = false;
            shown = item;
            Sprite s = item != null ? item.icon : null;
            if (colorImage != null) colorImage.sprite = s;
            if (greyImage != null) { greyImage.sprite = s; greyImage.color = greyTint; }
            if (showWhenFilled != null)
                foreach (GameObject g in showWhenFilled) if (g != null) g.SetActive(item != null);
        }
        if (colorImage != null) colorImage.enabled = item != null;
        if (greyImage != null && item == null) greyImage.enabled = false;

        UpdateCount(item != null ? source.Count : -1, item != null ? source.Capacity : 0);
    }

    void UpdateCount(int count, int capacity)
    {
        bool show = count >= 0;
        Color c = count >= capacity ? fullColor : count == 1 ? lastColor : Color.white;
        SetDigit(onesImage, show ? count % 10 : -1, c);
        SetDigit(tensImage, show && count >= 10 ? (count / 10) % 10 : -1, c);
    }

    void SetDigit(Image img, int digit, Color color)
    {
        if (img == null) return;
        bool on = digit >= 0 && numberSprites != null && digit < numberSprites.Length && numberSprites[digit] != null;
        img.enabled = on;
        if (!on) return;
        img.sprite = numberSprites[digit];
        img.color = color;
    }
}
