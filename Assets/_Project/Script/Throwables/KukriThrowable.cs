using UnityEngine;

// КУКРИ (нижний уровень: GameItemData -> ThrowableData -> ProjectileThrowable -> KukriThrowable).
// Один бросок - веер из ножей: один прямо, остальные под углом в стороны.
// Вплотную все ножи попадают в одного врага (много урона), на средней дистанции - задевают нескольких,
// издалека веер расходится и в одну цель попадает только центральный.
// Прицел - пунктирные линии на каждый нож с остриём на конце.
[CreateAssetMenu(fileName = "Kukri", menuName = "Game/Throwables/Kukri")]
public class KukriThrowable : ProjectileThrowable
{
    [Header("Кукри")]
    [Tooltip("Сколько ножей за бросок")]
    [Min(1)] public int knives = 3;
    [Tooltip("Угол между соседними ножами, в градусах")]
    public float spreadAngle = 15f;
    [Tooltip("Скорость вращения ножа в полёте, градусов в секунду")]
    public float spinSpeed = 1080f;

    float[] angles;
    int cachedKnives = -1;
    float cachedSpread;

    protected override float[] Angles
    {
        get
        {
            if (angles == null || cachedKnives != knives || cachedSpread != spreadAngle)
            {
                cachedKnives = knives;
                cachedSpread = spreadAngle;
                angles = new float[Mathf.Max(1, knives)];
                float mid = (angles.Length - 1) * 0.5f;
                for (int i = 0; i < angles.Length; i++) angles[i] = (i - mid) * spreadAngle;   // 3 ножа: -15, 0, 15
            }
            return angles;
        }
    }

    protected override ThrownProjectile CreateProjectile(GameObject go) => go.AddComponent<KukriKnife>();

    // Значения по умолчанию для нового ассета (свои, не как у остальных метательных)
    public KukriThrowable()
    {
        damage = 6f;
        throwCooldown = 0.35f;
        speed = 15f;
        lifetime = 1.2f;
        radius = 0.12f;
        aimSpacing = 0.3f;
        aimColor = new Color(0.85f, 0.75f, 1f, 0.85f);
    }
}
