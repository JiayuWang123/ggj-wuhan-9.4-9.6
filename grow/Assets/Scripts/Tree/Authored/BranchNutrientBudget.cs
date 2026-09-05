using System;
using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// Pool-based branch nutrients. Starting a branch spends 1; pruning refunds the
/// pruned segment plus every descendant in its subtree. Stars can grant extra nutrients.
/// </summary>
public class BranchNutrientBudget : MonoBehaviour
{
    [SerializeField] private AuthoredTreeController tree;
    [FormerlySerializedAs("maxConcurrentBranches")]
    [SerializeField] private int startingNutrients = 5;
    [SerializeField] private StarProtectionSystem starProtection;

    private int baselineStartingNutrients;
    private int availableNutrients;

    public int AvailableNutrients => availableNutrients;
    public int RemainingSlots => availableNutrients;
    public bool HasAvailableSlot => availableNutrients > 0;

    public event Action<int, int> SlotsChanged;

    private void Awake()
    {
        if (tree == null)
        {
            tree = GetComponent<AuthoredTreeController>();
        }

        if (starProtection == null)
        {
            starProtection = GetComponent<StarProtectionSystem>();
        }

        baselineStartingNutrients = startingNutrients;
        availableNutrients = startingNutrients;
    }

    private void OnEnable()
    {
        if (starProtection != null)
        {
            starProtection.StarNutrientBonusGranted += OnStarNutrientBonusGranted;
        }
    }

    private void OnDisable()
    {
        if (starProtection != null)
        {
            starProtection.StarNutrientBonusGranted -= OnStarNutrientBonusGranted;
        }
    }

    public void ResetNutrients()
    {
        startingNutrients = baselineStartingNutrients;
        availableNutrients = startingNutrients;
        RaiseSlotsChanged();
    }

    public bool CanStartNewBranch()
    {
        return availableNutrients > 0;
    }

    public bool TryConsumeNutrient()
    {
        if (availableNutrients <= 0)
        {
            return false;
        }

        availableNutrients--;
        RaiseSlotsChanged();
        return true;
    }

    public bool CanPrune(AuthoredTreeSegment segment)
    {
        return segment != null && segment.CanBePruned;
    }

    public void RefundPrunedSubtree(AuthoredTreeSegment root)
    {
        if (root == null)
        {
            return;
        }

        int refundAmount = CountSubtreeSegments(root);
        if (refundAmount <= 0)
        {
            return;
        }

        availableNutrients += refundAmount;
        RaiseSlotsChanged();
    }

    public void RefreshSlots()
    {
        RaiseSlotsChanged();
    }

    public int CountSubtreeSegments(AuthoredTreeSegment root)
    {
        if (root == null || !root.gameObject.activeInHierarchy)
        {
            return 0;
        }

        AuthoredTreeSegment[] segments = tree != null
            ? tree.GetSegments()
            : root.GetComponentsInChildren<AuthoredTreeSegment>(true);

        int count = 0;
        for (int i = 0; i < segments.Length; i++)
        {
            AuthoredTreeSegment segment = segments[i];
            if (segment == null || !segment.gameObject.activeInHierarchy)
            {
                continue;
            }

            if (segment == root || segment.IsDescendantOf(root))
            {
                count++;
            }
        }

        return count;
    }

    private void OnStarNutrientBonusGranted(StarProtectionMarker star, int bonusAmount)
    {
        if (bonusAmount <= 0)
        {
            return;
        }

        availableNutrients += bonusAmount;
        RaiseSlotsChanged();
    }

    private void RaiseSlotsChanged()
    {
        SlotsChanged?.Invoke(0, availableNutrients);
    }
}
