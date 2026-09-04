using UnityEngine;

/// <summary>
/// Scales the background sprite to cover the entire camera view (Play window).
/// When Auto Fit is on, Transform position/scale are driven by this script each frame.
/// Use Position Offset / Scale Multiplier for fine-tuning.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(SpriteRenderer))]
public class SpriteBackgroundFitter : MonoBehaviour
{
    [SerializeField] private Camera targetCamera;
    [SerializeField] private bool autoFit = true;
    [SerializeField] private float depthOffset = 15f;
    [SerializeField] private Vector2 positionOffset;
    [SerializeField] private float scaleMultiplier = 1f;

    private SpriteRenderer spriteRenderer;
    private float lastCameraAspect;
    private float lastOrthographicSize;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void OnEnable()
    {
        ResolveCamera();
        if (autoFit)
        {
            FitToCamera();
        }
    }

    private void LateUpdate()
    {
        if (!autoFit)
        {
            return;
        }

        ResolveCamera();
        if (targetCamera == null)
        {
            return;
        }

        bool cameraChanged = !Mathf.Approximately(lastCameraAspect, targetCamera.aspect)
            || !Mathf.Approximately(lastOrthographicSize, targetCamera.orthographicSize);

        if (cameraChanged || Application.isPlaying)
        {
            FitToCamera();
        }
    }

    public void FitToCamera()
    {
        ResolveCamera();

        if (targetCamera == null || !targetCamera.orthographic || spriteRenderer == null || spriteRenderer.sprite == null)
        {
            return;
        }

        Sprite sprite = spriteRenderer.sprite;
        float spriteWidth = sprite.rect.width / sprite.pixelsPerUnit;
        float spriteHeight = sprite.rect.height / sprite.pixelsPerUnit;
        if (spriteWidth <= 0f || spriteHeight <= 0f)
        {
            return;
        }

        float cameraHeight = targetCamera.orthographicSize * 2f;
        float cameraWidth = cameraHeight * targetCamera.aspect;

        // Cover: fill the whole view, crop edges if aspect ratios differ.
        float scale = Mathf.Max(cameraWidth / spriteWidth, cameraHeight / spriteHeight) * scaleMultiplier;
        transform.localScale = new Vector3(scale, scale, 1f);

        Vector3 cameraPosition = targetCamera.transform.position;
        transform.position = new Vector3(
            cameraPosition.x + positionOffset.x,
            cameraPosition.y + positionOffset.y,
            cameraPosition.z + depthOffset);

        lastCameraAspect = targetCamera.aspect;
        lastOrthographicSize = targetCamera.orthographicSize;
    }

    private void ResolveCamera()
    {
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }
    }
}

#if UNITY_EDITOR
[UnityEditor.CustomEditor(typeof(SpriteBackgroundFitter))]
public class SpriteBackgroundFitterEditor : UnityEditor.Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        SpriteBackgroundFitter fitter = (SpriteBackgroundFitter)target;
        UnityEditor.EditorGUILayout.Space();
        UnityEditor.EditorGUILayout.HelpBox(
            "Auto Fit 开启时：背景会自动铺满 Game 窗口，并随窗口大小变化。\n" +
            "微调请用 Position Offset / Scale Multiplier，不要直接改 Transform。",
            UnityEditor.MessageType.Info);

        if (GUILayout.Button("立即适配相机 (Fit To Camera)"))
        {
            fitter.FitToCamera();
            UnityEditor.EditorUtility.SetDirty(fitter.transform);
        }
    }
}
#endif
