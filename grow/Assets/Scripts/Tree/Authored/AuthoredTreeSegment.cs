using System;
using UnityEngine;

/// One authored piece of a tree (trunk or branch). Starts fully hidden, then reveals upward over time.
/// ClipFromBottom keeps the sprite in place and only shows pixels from the root upward.
[DisallowMultipleComponent]
public class AuthoredTreeSegment : MonoBehaviour
{
    public enum RevealMode
    {
        ClipFromBottom,
        ScaleFromRoot,
        Fade
    }

    public enum SegmentState
    {
        Locked,
        Revealing,
        Complete,
        Pruning
    }

    [Header("Reveal")]
    [SerializeField] private RevealMode revealMode = RevealMode.ClipFromBottom;
    [SerializeField] private float revealDuration = 8f;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Transform revealTarget;
    [SerializeField] private Vector2 localTip = new Vector2(0f, 1f);

    [Header("Trigger (when to start growing)")]
    [SerializeField] private AuthoredTreeSegment parentSegment;
    [SerializeField] private bool startImmediatelyOnPlay;
    [SerializeField] private TreeRevealTrigger trigger;
    [SerializeField, Range(0f, 1f)] private float parentProgressThreshold = 0.5f;
    [SerializeField] private bool useTriggerPointDistance = true;
    [SerializeField] private float triggerDistance = 0.5f;

    [Header("Prune")]
    [SerializeField] private bool canBePruned = true;
    [SerializeField] private Collider2D pruneCollider;

    private Vector3 fullLocalScale;
    private Color fullColor = Color.white;
    private float revealProgress;
    private SegmentState state = SegmentState.Locked;

    private Vector3 fullLocalPosition;
    private float spriteHalfHeight;
    private bool useBottomAnchoredReveal;
    private Material revealMaterial;
    private float spriteRevealBottom;
    private float spriteRevealTop;
    private float spriteRevealCenterX;
    private float growthSpeedMultiplier = 1f;
    private bool occupiesBranchSlot;
    private bool wasPruned;

    private static readonly int RevealProgressId = Shader.PropertyToID("_RevealProgress");
    private static readonly int RevealBottomId = Shader.PropertyToID("_RevealBottom");
    private static readonly int RevealTopId = Shader.PropertyToID("_RevealTop");

    public AuthoredTreeSegment ParentSegment => parentSegment;
    public SegmentState State => state;
    public float RevealProgress => revealProgress;
    public bool CanBePruned => canBePruned && (state == SegmentState.Revealing || state == SegmentState.Complete);
    public bool IsPruning => state == SegmentState.Pruning;
    public bool OccupiesBranchSlot => occupiesBranchSlot;
    public bool WasPruned => wasPruned;
    public Vector3 TipWorldPosition => transform.TransformPoint(GetLocalTip());

    public event Action<AuthoredTreeSegment> RevealStarted;
    public event Action<AuthoredTreeSegment> RevealCompleted;
    public event Action<AuthoredTreeSegment> PruneCompleted;

    public void SetGrowthSpeedMultiplier(float multiplier)
    {
        growthSpeedMultiplier = Mathf.Clamp01(multiplier);
    }

    private void Awake()
    {
        CacheVisualReferences();
    }

    private void OnValidate()
    {
        CacheVisualReferences();
    }

    private void CacheVisualReferences()
    {
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        if (revealTarget == null)
        {
            if (spriteRenderer != null)
            {
                revealTarget = spriteRenderer.transform;
            }
            else
            {
                Transform visual = transform.Find("Visual") ?? transform.Find("Visual1");
                revealTarget = visual != null ? visual : transform;
            }
        }

        if (pruneCollider == null)
        {
            pruneCollider = GetComponent<Collider2D>();
            if (pruneCollider == null)
            {
                pruneCollider = GetComponentInChildren<Collider2D>();
            }
        }

        if (revealTarget != null)
        {
            fullLocalScale = revealTarget.localScale;
            fullLocalPosition = revealTarget.localPosition;
        }

        if (spriteRenderer != null && spriteRenderer.sprite != null)
        {
            fullColor = spriteRenderer.color;
            Sprite sprite = spriteRenderer.sprite;
            spriteHalfHeight = sprite.bounds.extents.y;
            spriteRevealBottom = sprite.bounds.min.y;
            spriteRevealTop = sprite.bounds.max.y;
            spriteRevealCenterX = sprite.bounds.center.x;

            float normalizedPivotY = sprite.pivot.y / sprite.rect.height;
            useBottomAnchoredReveal = revealTarget != transform
                && revealMode == RevealMode.ScaleFromRoot
                && normalizedPivotY > 0.25f;

            EnsureRevealMaterial();

            Vector3 tip = GetLocalTip();
            localTip = new Vector2(tip.x, tip.y);
        }
    }

    private void EnsureRevealMaterial()
    {
        if (!Application.isPlaying || revealMode != RevealMode.ClipFromBottom || spriteRenderer == null)
        {
            return;
        }

        Shader revealShader = Shader.Find("Sprites/AuthoredTreeReveal");
        if (revealShader == null)
        {
            Debug.LogWarning($"{name}: AuthoredTreeReveal shader not found. Falling back to scale reveal.", this);
            revealMode = RevealMode.ScaleFromRoot;
            return;
        }

        if (revealMaterial == null || revealMaterial.shader != revealShader)
        {
            revealMaterial = new Material(revealShader);
            revealMaterial.mainTexture = spriteRenderer.sprite.texture;
            spriteRenderer.material = revealMaterial;
        }

        revealMaterial.SetFloat(RevealBottomId, spriteRevealBottom);
        revealMaterial.SetFloat(RevealTopId, spriteRevealTop);
    }

    private Vector3 GetLocalTip()
    {
        if (spriteRenderer != null && spriteRenderer.sprite != null)
        {
            if (revealMode == RevealMode.ClipFromBottom)
            {
                float progress = state == SegmentState.Locked ? 0f : revealProgress;
                float tipY = Mathf.Lerp(spriteRevealBottom, spriteRevealTop, progress);
                Vector3 tipInRendererLocal = new Vector3(spriteRevealCenterX, tipY, 0f);
                return transform.InverseTransformPoint(spriteRenderer.transform.TransformPoint(tipInRendererLocal));
            }

            Bounds bounds = spriteRenderer.bounds;
            Vector3 boundsTipWorld = new Vector3(bounds.center.x, bounds.max.y, bounds.center.z);
            return transform.InverseTransformPoint(boundsTipWorld);
        }

        return new Vector3(localTip.x, localTip.y, 0f);
    }

    private void Start()
    {
        CacheVisualReferences();
        HideInstant();
        if (startImmediatelyOnPlay && GetComponentInParent<AuthoredTreeController>() == null)
        {
            BeginReveal();
        }
    }

    private void Update()
    {
        if (state == SegmentState.Pruning)
        {
            return;
        }

        if (state != SegmentState.Revealing || growthSpeedMultiplier <= 0f)
        {
            return;
        }

        float step = revealDuration <= 0f ? 1f : Time.deltaTime / revealDuration;
        revealProgress = Mathf.Clamp01(revealProgress + step * growthSpeedMultiplier);
        ApplyRevealVisual();

        if (revealProgress >= 1f)
        {
            state = SegmentState.Complete;
            RevealCompleted?.Invoke(this);
        }
    }

    public float GetRevealProgressForWorldPoint(Vector3 worldPoint)
    {
        if (spriteRenderer == null || spriteRenderer.sprite == null)
        {
            return 1f;
        }

        Vector3 bottomWorld = spriteRenderer.transform.TransformPoint(
            new Vector3(spriteRevealCenterX, spriteRevealBottom, 0f));
        Vector3 topWorld = spriteRenderer.transform.TransformPoint(
            new Vector3(spriteRevealCenterX, spriteRevealTop, 0f));
        float bottomLocalY = transform.InverseTransformPoint(bottomWorld).y;
        float topLocalY = transform.InverseTransformPoint(topWorld).y;
        float targetLocalY = transform.InverseTransformPoint(worldPoint).y;

        if (Mathf.Approximately(bottomLocalY, topLocalY))
        {
            return 1f;
        }

        return Mathf.Clamp01(Mathf.InverseLerp(bottomLocalY, topLocalY, targetLocalY));
    }

    public bool IsTriggerSatisfied()
    {
        if (state != SegmentState.Locked)
        {
            return false;
        }

        if (wasPruned || !gameObject.activeInHierarchy)
        {
            return false;
        }

        if (startImmediatelyOnPlay)
        {
            return true;
        }

        if (parentSegment == null)
        {
            return false;
        }

        if (parentSegment.State == SegmentState.Locked)
        {
            return false;
        }

        if (trigger != null)
        {
            return HasParentTipReachedJunction();
        }

        return parentSegment.RevealProgress >= parentProgressThreshold;
    }

    private bool HasParentTipReachedJunction()
    {
        if (parentSegment.RevealProgress <= 0.001f)
        {
            return false;
        }

        Vector3 tipLocal = parentSegment.transform.InverseTransformPoint(parentSegment.TipWorldPosition);
        Vector3 triggerLocal = parentSegment.transform.InverseTransformPoint(trigger.WorldPosition);

        if (Vector2.Distance(tipLocal, triggerLocal) <= triggerDistance)
        {
            return true;
        }

        float requiredProgress = parentSegment.GetRevealProgressForWorldPoint(trigger.WorldPosition);
        return parentSegment.RevealProgress >= requiredProgress;
    }

    public void BeginReveal()
    {
        if (state != SegmentState.Locked)
        {
            return;
        }

        if (wasPruned || !gameObject.activeInHierarchy)
        {
            return;
        }

        state = SegmentState.Revealing;
        occupiesBranchSlot = true;
        revealProgress = 0f;
        ApplyRevealVisual();
        SetPruneColliderEnabled(true);
        RevealStarted?.Invoke(this);
    }

    public void HideInstant()
    {
        state = SegmentState.Locked;
        occupiesBranchSlot = false;
        wasPruned = false;
        revealProgress = 0f;
        growthSpeedMultiplier = 1f;
        if (spriteRenderer != null)
        {
            spriteRenderer.color = fullColor;
        }

        if (revealMode == RevealMode.ScaleFromRoot && revealTarget != null)
        {
            revealTarget.localScale = fullLocalScale;
            revealTarget.localPosition = fullLocalPosition;
        }
        ApplyRevealVisual();
        SetPruneColliderEnabled(false);
    }

    private void OnDestroy()
    {
        if (revealMaterial != null)
        {
            Destroy(revealMaterial);
        }
    }

    public void Prune()
    {
        PruneWithFade(0.5f);
    }

    public void PruneWithFade(float fadeDuration)
    {
        if (!CanBePruned && state != SegmentState.Complete)
        {
            return;
        }

        AuthoredTreeController controller = GetComponentInParent<AuthoredTreeController>();
        AuthoredTreeSegment[] fadeTargets = CollectPruneTargets(controller);
        bool invokePruneCompleted = true;
        for (int i = 0; i < fadeTargets.Length; i++)
        {
            if (!fadeTargets[i].gameObject.activeInHierarchy)
            {
                continue;
            }

            fadeTargets[i].BeginPruneFade(fadeDuration, invokePruneCompleted);
            invokePruneCompleted = false;
        }
    }

    private AuthoredTreeSegment[] CollectPruneTargets(AuthoredTreeController controller)
    {
        if (controller == null)
        {
            return new[] { this };
        }

        AuthoredTreeSegment[] allSegments = controller.GetComponentsInChildren<AuthoredTreeSegment>(true);
        int count = 0;
        for (int i = 0; i < allSegments.Length; i++)
        {
            AuthoredTreeSegment segment = allSegments[i];
            if (!segment.gameObject.activeInHierarchy)
            {
                continue;
            }

            if (segment == this || segment.IsDescendantOf(this))
            {
                count++;
            }
        }

        AuthoredTreeSegment[] targets = new AuthoredTreeSegment[count];
        int index = 0;
        for (int i = 0; i < allSegments.Length; i++)
        {
            AuthoredTreeSegment segment = allSegments[i];
            if (!segment.gameObject.activeInHierarchy)
            {
                continue;
            }

            if (segment == this || segment.IsDescendantOf(this))
            {
                targets[index++] = segment;
            }
        }

        return targets;
    }

    private void BeginPruneFade(float fadeDuration, bool invokePruneCompleted)
    {
        if (state == SegmentState.Pruning || !gameObject.activeInHierarchy)
        {
            return;
        }

        state = SegmentState.Pruning;
        occupiesBranchSlot = false;
        wasPruned = true;
        SetPruneColliderEnabled(false);
        StartCoroutine(PruneFadeRoutine(fadeDuration, invokePruneCompleted));
    }

    private System.Collections.IEnumerator PruneFadeRoutine(float fadeDuration, bool invokePruneCompleted)
    {
        float elapsed = 0f;
        float startAlpha = fullColor.a;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(startAlpha, 0f, elapsed / fadeDuration);
            SetVisualAlpha(alpha);
            yield return null;
        }

        SetVisualAlpha(0f);
        gameObject.SetActive(false);
        state = SegmentState.Locked;

        if (invokePruneCompleted)
        {
            PruneCompleted?.Invoke(this);
        }
    }

    private void SetVisualAlpha(float alpha)
    {
        if (spriteRenderer == null)
        {
            return;
        }

        Color color = fullColor;
        color.a = alpha;
        spriteRenderer.color = color;
    }

    public bool IsDescendantOf(AuthoredTreeSegment ancestor)
    {
        AuthoredTreeSegment current = parentSegment;
        while (current != null)
        {
            if (current == ancestor)
            {
                return true;
            }

            current = current.ParentSegment;
        }

        return false;
    }

    private void ApplyRevealVisual()
    {
        switch (revealMode)
        {
            case RevealMode.ClipFromBottom:
                if (revealMaterial != null)
                {
                    revealMaterial.SetFloat(RevealProgressId, revealProgress);
                }
                break;

            case RevealMode.ScaleFromRoot:
                float scaleY = Mathf.Max(0.001f, fullLocalScale.y * revealProgress);
                revealTarget.localScale = new Vector3(fullLocalScale.x, scaleY, fullLocalScale.z);

                if (useBottomAnchoredReveal)
                {
                    float anchoredY = fullLocalPosition.y + (spriteHalfHeight * (scaleY - 1f));
                    revealTarget.localPosition = new Vector3(fullLocalPosition.x, anchoredY, fullLocalPosition.z);
                }
                break;

            case RevealMode.Fade:
                if (spriteRenderer != null)
                {
                    Color color = fullColor;
                    color.a = fullColor.a * revealProgress;
                    spriteRenderer.color = color;
                }
                break;
        }
    }

    private void SetPruneColliderEnabled(bool enabled)
    {
        if (pruneCollider != null)
        {
            pruneCollider.enabled = enabled && canBePruned;
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (trigger != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(trigger.WorldPosition, triggerDistance);
        }

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(TipWorldPosition, 0.05f);
        Gizmos.DrawLine(transform.position, TipWorldPosition);
    }
}
