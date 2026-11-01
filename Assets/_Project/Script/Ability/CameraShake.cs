using UnityEngine;
using UnityEngine.Rendering;

// Короткий толчок камеры при ударе. Вешается на Main Camera рядом с CameraFollow.
// Смещение добавляется только на время отрисовки кадра и сразу убирается,
// поэтому CameraFollow о нём не знает и ничего не ломается.
[RequireComponent(typeof(Camera))]
public class CameraShake : MonoBehaviour
{
    [Tooltip("Жёсткость пружины: чем больше, тем быстрее камера возвращается")]
    public float stiffness = 350f;
    [Tooltip("Гашение: чем больше, тем меньше камера «дрожит» после толчка")]
    public float damping = 24f;
    [Tooltip("Самое большое смещение, в клетках")]
    public float maxOffset = 0.35f;
    [Tooltip("Общий множитель силы толчков (0 = выключить)")]
    public float strength = 1f;

    static CameraShake instance;
    Camera cam;
    Vector2 offset, velocity;
    Vector3 applied;

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

    // push - направление и сила толчка в клетках (например dir * 0.1f)
    public static void Kick(Vector2 push)
    {
        if (instance == null) return;
        instance.offset = Vector2.ClampMagnitude(instance.offset + push * instance.strength, instance.maxOffset);
    }

    void Update()
    {
        float dt = Mathf.Min(Time.unscaledDeltaTime, 1f / 30f);   // работает и во время стоп-кадра
        Vector2 acc = -stiffness * offset - damping * velocity;
        velocity += acc * dt;
        offset += velocity * dt;
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
