using System.Collections.Generic;
using UnityEngine;

public class SwipePruner : MonoBehaviour
{
    [SerializeField] private Camera targetCamera;
    [SerializeField] private TreeGrowthController tree;
    [SerializeField] private LayerMask branchLayerMask;
    [SerializeField] private float sampleRadius = 0.12f;
    [SerializeField] private float minSampleDistance = 0.08f;

    private readonly Collider2D[] overlapResults = new Collider2D[16];
    private readonly HashSet<BranchNode> prunedThisDrag = new HashSet<BranchNode>();
    private Vector2 previousWorldPoint;
    private bool hasPreviousPoint;

    private void Awake()
    {
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        if (tree == null)
        {
            tree = FindObjectOfType<TreeGrowthController>();
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
        int hitCount = Physics2D.OverlapCircleNonAlloc(point, sampleRadius, overlapResults, branchLayerMask);
        for (int i = 0; i < hitCount; i++)
        {
            BranchNode branch = overlapResults[i].GetComponentInParent<BranchNode>();
            if (branch == null || prunedThisDrag.Contains(branch))
            {
                continue;
            }

            prunedThisDrag.Add(branch);
            tree.Prune(branch);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, sampleRadius);
    }
}
