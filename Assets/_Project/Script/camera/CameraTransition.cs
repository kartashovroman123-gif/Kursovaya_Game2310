using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;

// Сдвиг камеры при переходе между комнатами: камера уезжает в сторону выхода, пока экран темнеет,
// и приезжает в новую комнату с противоположной стороны, пока светлеет.
// Вешается на Main Camera рядом с CameraFollow и CameraShake. Как и CameraShake, сдвигает камеру только
// на время отрисовки кадра - CameraFollow о нём не знает и ничего не ломается.
[RequireComponent(typeof(Camera))]
public class CameraTransition : MonoBehaviour
{
    [Tooltip("На сколько клеток уезжает камера")]
    public float distance = 3f;
    [Tooltip("Время отъезда (пока экран темнеет), сек")]
    public float outTime = 0.2f;
    [Tooltip("Время въезда (пока экран светлеет), сек")]
    public float inTime = 0.25f;

    static CameraTransition instance;
    Camera cam;
    Vector3 offset, applied;

    void Awake() { cam = GetComponent<Camera>(); }

    void OnEnable()
    {
        instance = this;
        RenderPipelineManager.beginCameraRendering += OnBeginRender;
        RenderPipelineManager.endCameraRendering += OnEndRender;
    }

    void OnDisable()
    {
        RenderPipelineManager.beginCameraRendering -= OnBeginRender;
        RenderPipelineManager.endCameraRendering -= OnEndRender;
        if (instance == this) instance = null;
    }

    // Уехать в сторону dir (разгоняясь)
    public static IEnumerator SlideOut(Vector2 dir)
    {
        if (instance == null) yield break;
        CameraTransition c = instance;
        for (float t = 0f; t < c.outTime; t += Time.unscaledDeltaTime)
        {
            float k = t / c.outTime;
            c.offset = (Vector3)(dir * c.distance * k * k);
            yield return null;
        }
        c.offset = (Vector3)(dir * c.distance);
    }

    // Приехать с противоположной стороны (тормозя)
    public static IEnumerator SlideIn(Vector2 dir)
    {
        if (instance == null) yield break;
        CameraTransition c = instance;
        for (float t = 0f; t < c.inTime; t += Time.unscaledDeltaTime)
        {
            float k = 1f - t / c.inTime;
            c.offset = (Vector3)(-dir * c.distance * k * k);
            yield return null;
        }
        c.offset = Vector3.zero;
    }

    void OnBeginRender(ScriptableRenderContext ctx, Camera c)
    {
        if (c != cam) return;
        applied = offset;
        transform.position += applied;
    }

    void OnEndRender(ScriptableRenderContext ctx, Camera c)
    {
        if (c != cam) return;
        transform.position -= applied;
        applied = Vector3.zero;
    }
}
