using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Иконка молота (ПКМ) в правом нижнем углу экрана. Наследник CooldownIcon.
// Сверх обычной перезарядки показывает:
//  - цифру следующей ступени 1 / 2 / 3 (у каждой свой цвет);
//  - точки взмахов когтями, пока идёт набор на следующую ступень (3 или 5 точек);
//  - кольцо вокруг иконки, пока открыта ступень 2 или 3: убывает до сброса на ступень 1.
// Героя находит сама (работает и после смены персонажа). Вешается на объект иконки внутри Canvas.
public class HammerIcon : CooldownIcon
{
    [Header("Герой (можно не ставить - найдётся сам)")]
    public PlayerHeavyAttack source;

    [Header("Цифра ступени")]
    public Image digitImage;
    [Tooltip("Картинки цифр: 1, 2, 3")]
    public Sprite[] digitSprites = new Sprite[3];
    [Tooltip("Цвет цифры для ступени 1, 2, 3")]
    public Color[] stageColors =
    {
        new Color(0.92f, 0.88f, 1f),
        new Color(0.78f, 0.55f, 1f),
        new Color(1f, 0.47f, 0.9f)
    };

    [Header("Точки взмахов когтями")]
    [Tooltip("Пустой объект под иконкой, внутри него появятся точки")]
    public RectTransform dotsParent;
    public Sprite dotOn;
    public Sprite dotOff;
    [Tooltip("Размер точки на экране")]
    public float dotSize = 20f;
    [Tooltip("Расстояние между центрами точек")]
    public float dotSpacing = 22f;

    [Header("Кольцо до сброса ступени")]
    [Tooltip("Картинка кольца. Image Type = Filled, Radial 360, Origin = Top, Clockwise")]
    public Image resetRing;
    public Color ringColor = new Color(0.75f, 0.47f, 1f, 1f);

    readonly List<Image> dots = new List<Image>();
    float searchTimer;

    protected override bool HasAbility => source != null && source.isActiveAndEnabled && source.HasHammer;
    protected override float CooldownFraction => source.CooldownFraction;

    protected override void FindSource()
    {
        if (source != null && source.isActiveAndEnabled) return;
        searchTimer -= Time.unscaledDeltaTime;
        if (searchTimer > 0f) return;
        searchTimer = 0.5f;                                   // искать не каждый кадр
        source = FindFirstObjectByType<PlayerHeavyAttack>();
    }

    protected override void UpdateExtras()
    {
        // цифра ступени
        int stage = Mathf.Clamp(source.NextStageNumber, 1, 3) - 1;
        if (digitImage != null)
        {
            if (digitSprites != null && stage < digitSprites.Length && digitSprites[stage] != null)
                digitImage.sprite = digitSprites[stage];
            if (stageColors != null && stage < stageColors.Length)
                digitImage.color = stageColors[stage];
        }

        // точки взмахов
        bool counting = source.IsCountingHits && source.ClawHitsNeeded > 0;
        ShowDots(counting ? source.ClawHitsNeeded : 0, source.ClawHits);

        // кольцо до сброса
        if (resetRing != null)
        {
            bool unlocked = source.IsStageUnlocked;
            resetRing.enabled = unlocked;
            if (unlocked)
            {
                resetRing.fillAmount = source.UnlockedFraction;
                resetRing.color = ringColor;
            }
        }
    }

    void ShowDots(int need, int got)
    {
        if (dotsParent == null) return;

        // создать недостающие точки
        while (dots.Count < need)
        {
            var go = new GameObject("Dot", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(dotsParent, false);
            var img = go.GetComponent<Image>();
            img.raycastTarget = false;
            dots.Add(img);
        }

        float startX = -(need - 1) * dotSpacing * 0.5f;      // точки по центру под иконкой
        for (int i = 0; i < dots.Count; i++)
        {
            bool visible = i < need;
            dots[i].gameObject.SetActive(visible);
            if (!visible) continue;
            dots[i].sprite = i < got ? dotOn : dotOff;
            var rt = dots[i].rectTransform;
            rt.sizeDelta = new Vector2(dotSize, dotSize);
            rt.anchoredPosition = new Vector2(startX + i * dotSpacing, 0f);
        }
    }
}
