using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Swipe to prune authored tree segments (each segment is a pre-placed sprite piece).
/// </summary>
public class AuthoredSwipePruner : MonoBehaviour
{
    [SerializeField] private Camera targetCamera;
    [SerializeField] private AuthoredTreeController tree;
    [SerializeField] private LayerMask segmentLayerMask;
    [SerializeField] private float sampleRadius = 0.12f;
    [SerializeField] private float minSampleDistance = 0.08f;

    private readonly Collider2D[] overlapResults = new Collider2D[16];
    private readonly HashSet<AuthoredTreeSegment> prunedThisDrag = new HashSet<AuthoredTreeSegment>();
    private Vector2 previousWorldPoint;
    private bool hasPreviousPoint;
    private BranchChoiceGate choiceGate;

    private void Awake()
    {
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        if (tree == null)
        {
            tree = FindObjectOfType<AuthoredTreeController>();
        }

        if (tree != null)
        {
            choiceGate = tree.GetComponent<BranchChoiceGate>();
        }
    }

    private void Update()
    {
        if (targetCamera == null || tree == null)
        {
            return;
        }

        if (Input.GetMouseButtonDown(0))
        {
            prunedThisDrag.Clear();
            hasPreviousPoint = false;
            SampleAtMouse();
            return;
        }

        if (Input.GetMouseButton(0))
        {
            SampleAtMouse();
            return;
        }

        if (Input.GetMouseButtonUp(0))
        {
            prunedThisDrag.Clear();
            hasPreviousPoint = false;
        }
    }

    private void SampleAtMouse()
    {
        Vector2 currentWorldPoint = targetCamera.ScreenToWorldPoint(Input.mousePosition);

        if (!hasPreviousPoint)
        {
            PruneAtPoint(currentWorldPoint);
            previousWorldPoint = currentWorldPoint;
            hasPreviousPoint = true;
            return;
        }

        float distance = Vector2.Distance(previousWorldPoint, currentWorldPoint);
        int steps = Mathf.Max(1, Mathf.CeilToInt(distance / minSampleDistance));

        for (int i = 1; i <= steps; i++)
        {
            Vector2 point = Vector2.Lerp(previousWorldPoint, currentWorldPoint, i / (float)steps);
            PruneAtPoint(point);
        }

        previousWorldPoint = currentWorldPoint;
    }

    private void PruneAtPoint(Vector2 point)
    {
        int hitCount = Physics2D.OverlapCircleNonAlloc(point, sampleRadius, overlapResults, segmentLayerMask);
        for (int i = 0; i < hitCount; i++)
        {
            AuthoredTreeSegment segment = overlapResults[i].GetComponentInParent<AuthoredTreeSegment>();
            if (segment == null || prunedThisDrag.Contains(segment))
            {
                continue;
            }

            if (choiceGate != null && !choiceGate.CanPrune(segment))
            {
                continue;
            }

            if (!segment.CanBePruned)
            {
                continue;
            }

            prunedThisDrag.Add(segment);
            tree.PruneSegment(segment);
        }
    }
}
