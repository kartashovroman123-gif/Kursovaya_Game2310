using System.Collections;
using UnityEngine;

[RequireComponent(typeof(CanvasGroup))]
public class ScreenFader : MonoBehaviour
{
    public float fadeOutTime = 0.25f;   // затемнение
    public float fadeInTime = 0.30f;    // осветление
    public bool startBlack = true;      // в начале игры экран чёрный, пока не создан хаб

    CanvasGroup group;

    void Awake()
    {
        group = GetComponent<CanvasGroup>();
        group.interactable = false;
        group.blocksRaycasts = false;
        group.alpha = startBlack ? 1f : 0f;
    }

    public IEnumerator FadeOut() { yield return Fade(1f, fadeOutTime); }
    public IEnumerator FadeIn() { yield return Fade(0f, fadeInTime); }

    IEnumerator Fade(float target, float time)
    {
        float start = group.alpha;
        float t = 0f;
        while (t < time)
        {
            t += Time.unscaledDeltaTime;
            group.alpha = Mathf.Lerp(start, target, Mathf.SmoothStep(0f, 1f, t / time));
            yield return null;
        }
        group.alpha = target;
    }
}