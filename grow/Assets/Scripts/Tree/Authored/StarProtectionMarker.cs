using UnityEngine;

/// <summary>
/// Star contact zone. When this trigger/collider overlaps a branch prune collider,
/// that branch and its parent chain become protected from pruning.
/// Move the star freely; the yellow wire sphere follows the object.
/// </summary>
[RequireComponent(typeof(CircleCollider2D))]
public class StarProtectionMarker : MonoBehaviour
{
    [SerializeField] private float activationRadius = 0.35f;
    [SerializeField] private int nutrientBonusOnCollect = 0;
    [SerializeField] private AuthoredTreeSegment[] requiredTouchingSegments;
    [SerializeField] private Color gizmoColor = new Color(1f, 0.92f, 0.2f, 0.95f);

    private CircleCollider2D contactCollider;

    public bool IsCollected { get; private set; }
    public float ActivationRadius => activationRadius;
    public int NutrientBonusOnCollect => nutrientBonusOnCollect;

    public bool AllowsCollectingSegment(AuthoredTreeSegment segment)
    {
        if (requiredTouchingSegments == null || requiredTouchingSegments.Length == 0)
        {
            return true;
        }

        for (int i = 0; i < requiredTouchingSegments.Length; i++)
        {
            if (requiredTouchingSegments[i] == segment)
            {
                return true;
            }
        }

        return false;
    }
    public Collider2D ContactCollider
    {
        get
        {
            EnsureContactCollider();
            return contactCollider;
        }
    }

    private void Awake()
    {
        EnsureContactCollider();
    }

    private void OnValidate()
    {
        EnsureContactCollider();
    }

    private void EnsureContactCollider()
    {
        if (contactCollider == null)
        {
            contactCollider = GetComponent<CircleCollider2D>();
        }

        if (contactCollider == null)
        {
            contactCollider = gameObject.AddComponent<CircleCollider2D>();
        }

        contactCollider.isTrigger = true;

        float maxScale = Mathf.Max(
            Mathf.Abs(transform.lossyScale.x),
            Mathf.Abs(transform.lossyScale.y));
        contactCollider.radius = activationRadius / Mathf.Max(maxScale, 0.0001f);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = gizmoColor;
        Gizmos.DrawWireSphere(transform.position, activationRadius);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(gizmoColor.r, gizmoColor.g, gizmoColor.b, 0.35f);
        Gizmos.DrawSphere(transform.position, activationRadius);
    }

    public void MarkCollected()
    {
        IsCollected = true;

        StarCollectSfx collectSfx = GetComponent<StarCollectSfx>();
        if (collectSfx != null)
            collectSfx.Play();

        gameObject.SetActive(false);
    }

    public void ResetCollected()
    {
        IsCollected = false;
        gameObject.SetActive(true);
    }
}
