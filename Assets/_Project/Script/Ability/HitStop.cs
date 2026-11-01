using UnityEngine;

// «Стоп-кадр» при попадании: игра на долю секунды почти замирает, удар ощущается тяжелее.
// Вызывать: HitStop.Do(0.06f). На сцену ставить не нужно - объект создаётся сам.
public class HitStop : MonoBehaviour
{
    const float SlowScale = 0.02f;   // почти стоп, но не 0 (0 у нас означает паузу)

    static HitStop instance;
    float until;
    float savedScale = 1f;
    bool active;

    public static void Do(float duration)
    {
        if (duration <= 0f || Time.timeScale == 0f) return;   // на паузе не трогаем
        if (instance == null)
        {
            var go = new GameObject("HitStop");
            DontDestroyOnLoad(go);
            instance = go.AddComponent<HitStop>();
        }
        instance.Begin(duration);
    }

    void Begin(float duration)
    {
        if (!active)
        {
            savedScale = Time.timeScale;
            Time.timeScale = SlowScale;
            active = true;
        }
        until = Mathf.Max(until, Time.unscaledTime + duration);
    }

    void Update()
    {
        if (!active || Time.unscaledTime < until) return;
        active = false;
        // если за это время кто-то поставил паузу (timeScale = 0), не снимаем её
        if (Mathf.Approximately(Time.timeScale, SlowScale)) Time.timeScale = savedScale;
    }
}
