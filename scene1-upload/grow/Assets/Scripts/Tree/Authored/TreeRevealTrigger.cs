using UnityEngine;

/// <summary>
/// Designer-placed marker on the parent segment where a child branch should start growing.
/// </summary>
public class TreeRevealTrigger : MonoBehaviour
{
    [SerializeField] private AuthoredTreeSegment ownerSegment;
    [SerializeField] private Color gizmoColor = new Color(1f, 0.85f, 0.2f, 0.9f);
    [SerializeField] private float gizmoRadius = 0.12f;

    public Vector3 WorldPosition => transform.position;
    public float TouchRadius => gizmoRadius;
    public AuthoredTreeSegment OwnerSegment => ownerSegment;

    private void OnDrawGizmos()
    {
        Gizmos.color = gizmoColor;
        Gizmos.DrawWireSphere(transform.position, gizmoRadius);

        if (ownerSegment != null)
        {
            Gizmos.color = new Color(gizmoColor.r, gizmoColor.g, gizmoColor.b, 0.35f);
            Gizmos.DrawLine(ownerSegment.TipWorldPosition, transform.position);
        }
    }
}
