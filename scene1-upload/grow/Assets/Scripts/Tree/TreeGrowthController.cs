using System.Collections.Generic;
using UnityEngine;

public class TreeGrowthController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private BranchNode branchPrefab;
    [SerializeField] private Transform branchContainer;

    [Header("Shape - based on the CheapDevotion recursive tree gist")]
    [SerializeField] private int randomSeed = 2026;
    [SerializeField] private int maxDepth = 7;
    [SerializeField] private float rootAngleDegrees = 0f;
    [SerializeField] private float branchAngle = 22f;
    [SerializeField] private float angleRandom = 7f;
    [SerializeField] private float rootLength = 1.25f;
    [SerializeField] private float lengthScalePerDepth = 0.86f;
    [SerializeField] private float lengthRandom = 0.12f;
    [SerializeField] private float widthScalePerDepth = 0.82f;

    [Header("Growth")]
    [SerializeField] private float nutrients = 4f;
    [SerializeField] private float maxNutrients = 8f;
    [SerializeField] private float nutrientRegenPerSecond = 0.6f;
    [SerializeField] private float growthLengthPerNutrient = 0.55f;
    [SerializeField] private float maxNutrientSpendPerTipPerSecond = 0.9f;
    [SerializeField] private float splitCost = 0.12f;
    [SerializeField] private float pruneRefundRatio = 0.45f;
    [SerializeField] private int maxActiveTips = 48;
    [SerializeField] private float restartAfterRootCutDelay = 0.4f;

    [Header("Branching")]
    [SerializeField] private float oneBranchChance = 0.12f;
    [SerializeField] private float noBranchChanceAtFinalDepth = 1f;

    [Header("Environment")]
    [SerializeField] private LayerMask lightLayerMask;
    [SerializeField] private LayerMask hazardLayerMask;
    [SerializeField] private float tipCheckRadius = 0.12f;

    private readonly List<BranchNode> allBranches = new List<BranchNode>();
    private readonly List<BranchNode> activeTips = new List<BranchNode>();
    private readonly List<BranchNode> flowers = new List<BranchNode>();
    private readonly Collider2D[] environmentHits = new Collider2D[8];

    private System.Random rng;
    private float restartTimer = -1f;

    public float Nutrients => nutrients;
    public float MaxNutrients => maxNutrients;
    public int BranchCount => allBranches.Count;
    public int ActiveTipCount => activeTips.Count;
    public int FlowerCount => flowers.Count;

    private void Start()
    {
        ResetTree();
    }

    private void Update()
    {
        if (restartTimer >= 0f)
        {
            restartTimer -= Time.deltaTime;
            if (restartTimer <= 0f)
            {
                ResetTree();
            }
        }

        TickNutrients(Time.deltaTime);
        TickGrowth(Time.deltaTime);

        if (Input.GetKeyDown(KeyCode.R))
        {
            ResetTree();
        }
    }

    public void ResetTree()
    {
        rng = new System.Random(randomSeed);
        restartTimer = -1f;
        nutrients = Mathf.Clamp(nutrients, 0f, maxNutrients);

        for (int i = allBranches.Count - 1; i >= 0; i--)
        {
            if (allBranches[i] != null)
            {
                Destroy(allBranches[i].gameObject);
            }
        }

        allBranches.Clear();
        activeTips.Clear();
        flowers.Clear();

        SpawnBranch(null, transform.position, rootAngleDegrees, 0, rootLength, 1f);
    }

    public void NotifyNodeReachedTarget(BranchNode node)
    {
        activeTips.Remove(node);

        if (node.Depth >= maxDepth)
        {
            if (Random01() <= noBranchChanceAtFinalDepth)
            {
                node.MarkAsTerminalTip();
            }
            return;
        }

        if (allBranches.Count >= maxActiveTips)
        {
            node.MarkAsTerminalTip();
            return;
        }

        if (nutrients >= splitCost)
        {
            nutrients -= splitCost;
        }

        SpawnChildren(node);
    }

    public void NotifyBranchFlowered(BranchNode node)
    {
        activeTips.Remove(node);
        if (!flowers.Contains(node))
        {
            flowers.Add(node);
        }
    }

    public void Prune(BranchNode node)
    {
        if (node == null || !allBranches.Contains(node))
        {
            return;
        }

        float refund = node.GetSubtreeLength() * pruneRefundRatio;
        nutrients = Mathf.Min(maxNutrients, nutrients + refund);

        List<BranchNode> subtree = new List<BranchNode>();
        node.CollectSubtree(subtree);
        node.DetachFromParent();

        for (int i = 0; i < subtree.Count; i++)
        {
            BranchNode branch = subtree[i];
            activeTips.Remove(branch);
            flowers.Remove(branch);
            allBranches.Remove(branch);
        }

        for (int i = subtree.Count - 1; i >= 0; i--)
        {
            if (subtree[i] != null)
            {
                Destroy(subtree[i].gameObject);
            }
        }

        if (allBranches.Count == 0)
        {
            restartTimer = restartAfterRootCutDelay;
        }
    }

    public void HandleHazardHit(BranchNode branch, TreeHazard hazard)
    {
        if (hazard.PruneOnTouch)
        {
            Prune(branch);
        }
    }

    public bool CheckEnvironmentAtTip(BranchNode branch)
    {
        if (branch == null || !branch.IsTip)
        {
            return false;
        }

        int lightHits = Physics2D.OverlapCircleNonAlloc(branch.TipPosition, tipCheckRadius, environmentHits, lightLayerMask);
        if (lightHits > 0)
        {
            branch.Bloom();
            return false;
        }

        int hazardHits = Physics2D.OverlapCircleNonAlloc(branch.TipPosition, tipCheckRadius, environmentHits, hazardLayerMask);
        for (int i = 0; i < hazardHits; i++)
        {
            TreeHazard hazard = environmentHits[i].GetComponent<TreeHazard>();
            if (hazard != null)
            {
                HandleHazardHit(branch, hazard);
                return false;
            }
        }

        return true;
    }

    private void TickNutrients(float deltaTime)
    {
        nutrients = Mathf.Min(maxNutrients, nutrients + nutrientRegenPerSecond * deltaTime);
    }

    private void TickGrowth(float deltaTime)
    {
        if (activeTips.Count == 0 || nutrients <= 0f)
        {
            return;
        }

        BranchNode[] tipsSnapshot = activeTips.ToArray();
        float spendBudget = nutrients;
        float spendPerTip = Mathf.Min(maxNutrientSpendPerTipPerSecond * deltaTime, spendBudget / tipsSnapshot.Length);

        for (int i = 0; i < tipsSnapshot.Length; i++)
        {
            BranchNode tip = tipsSnapshot[i];
            if (tip == null || !tip.IsTip || !activeTips.Contains(tip))
            {
                continue;
            }

            if (nutrients <= 0f)
            {
                break;
            }

            float spend = Mathf.Min(spendPerTip, nutrients);
            nutrients -= spend;
            tip.Grow(spend * growthLengthPerNutrient);
        }
    }

    private void SpawnChildren(BranchNode parent)
    {
        int childCount = Random01() < oneBranchChance ? 1 : 2;
        Vector3 start = parent.TipPosition;
        float childLength = Mathf.Max(0.25f, parent.TargetLength * lengthScalePerDepth * RandomRange(1f - lengthRandom, 1f + lengthRandom));
        float childWidth = Mathf.Max(0.12f, parent.WidthScale * widthScalePerDepth);

        if (childCount == 1)
        {
            float sign = Random01() < 0.5f ? -1f : 1f;
            SpawnBranch(parent, start, parent.AngleDegrees + sign * branchAngle + RandomRange(-angleRandom, angleRandom), parent.Depth + 1, childLength, childWidth);
            return;
        }

        SpawnBranch(parent, start, parent.AngleDegrees - branchAngle + RandomRange(-angleRandom, angleRandom), parent.Depth + 1, childLength, childWidth);
        SpawnBranch(parent, start, parent.AngleDegrees + branchAngle + RandomRange(-angleRandom, angleRandom), parent.Depth + 1, childLength, childWidth);
    }

    private BranchNode SpawnBranch(BranchNode parent, Vector3 start, float angleDegrees, int depth, float targetLength, float widthScale)
    {
        if (branchPrefab == null)
        {
            Debug.LogError("TreeGrowthController needs a BranchNode prefab assigned.", this);
            enabled = false;
            return null;
        }

        Transform container = branchContainer != null ? branchContainer : transform;
        BranchNode branch = Instantiate(branchPrefab, start, Quaternion.identity, container);
        branch.Initialize(this, parent, start, angleDegrees, depth, targetLength, widthScale);

        allBranches.Add(branch);
        activeTips.Add(branch);
        return branch;
    }

    private float Random01()
    {
        return (float)rng.NextDouble();
    }

    private float RandomRange(float min, float max)
    {
        return Mathf.Lerp(min, max, Random01());
    }
}
