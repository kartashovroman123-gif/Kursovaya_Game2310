using System;
using UnityEngine;

// Кадры двери для трёх видов: сверху (лицом к камере), сбоку (правая стена; для левой картинка зеркалится), снизу.
// Кадр 0 - закрыта, последний - открыта.
[Serializable]
public class DoorFrames
{
    public Sprite[] up;
    public Sprite[] side;
    public Sprite[] down;

    public Sprite[] Get(DoorSide s)
    {
        if (s == DoorSide.Up) return up;
        if (s == DoorSide.Down) return down;
        return side;
    }
}

[Serializable]
public class TypeDoorFrames
{
    public RoomType type;
    public DoorFrames frames;
}

// Внешний вид всех дверей: какие кадры у какого типа, скорость анимации, порядок отрисовки, зоны прохода.
// Один ассет на игру. Заполняется кнопкой «Заполнить из спрайтов» (по именам Door_Тип_Сторона_Кадр).
[CreateAssetMenu(fileName = "DoorStyle", menuName = "Game/Rooms/Door Style")]
public class DoorStyle : ScriptableObject
{
    [Header("Кадры")]
    public TypeDoorFrames[] doors;
    [Tooltip("Вход (дверь, через которую пришёл герой): нейтральная, захлопывается")]
    public DoorFrames entrance;
    [Tooltip("Замок двери на ключ: 0 - висит, 1 - открыт, 2 - падает")]
    public DoorFrames lockFrames;

    [Header("Анимация")]
    [Tooltip("Сколько секунд показывается один кадр открытия")]
    public float frameTime = 0.07f;
    [Tooltip("Через сколько секунд после входа захлопывается дверь за героем")]
    public float slamDelay = 0.45f;
    [Tooltip("Толчок камеры при захлопывании, в клетках")]
    public float slamShake = 0.12f;
    [Tooltip("Пыль при захлопывании (необязательно)")]
    public GameObject slamDustPrefab;

    [Header("Отрисовка")]
    [Tooltip("Order in Layer для дверей сверху и сбоку (позади героя)")]
    public int backSortingOrder = 3;
    [Tooltip("Order in Layer для нижней двери (перед героем)")]
    public int frontSortingOrder = 20;
    [Tooltip("Материал (пусто = стандартный)")]
    public Material material;

    [Header("Зона прохода (в клетках, от низа двери)")]
    public Vector2 upTriggerSize = new Vector2(1f, 0.6f);
    public Vector2 sideTriggerSize = new Vector2(0.6f, 1f);
    public Vector2 downTriggerSize = new Vector2(1f, 0.8f);

    public DoorFrames For(RoomType type)
    {
        if (doors != null)
            foreach (TypeDoorFrames d in doors)
                if (d != null && d.type == type) return d.frames;
        return null;
    }
}
