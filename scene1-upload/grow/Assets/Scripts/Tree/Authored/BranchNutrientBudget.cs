using System;
using UnityEngine;

/// <summary>
/// Tracks available nutrients for starting new branches. Each trigger consumes 1;
/// pruning restores nutrients based on how many branch slots were freed.
/// </summary>
public class BranchNutrientBudget : MonoBehaviour
{
    [SerializeField] private AuthoredTreeController tree;
    [SerializeField] private int maxConcurrentBranches = 8;
    [SerializeField] private StarProtectionSystem starProtection;
    [SerializeField] private int maxIncreaseOnFirstStar = 0;

    private int baselineMaxConcurrentBranches;
    private bool firstStarBonusApplied;

    public int MaxConcurrentBranches => maxConcurrentBranches;
    public int ActiveBranchCount => CountActiveBranches();
    public int AvailableNutrients => Mathf.Max(0, maxConcurrentBranches - ActiveBranchCount);
    public int RemainingSlots => AvailableNutrients;
    public bool HasAvailableSlot => AvailableNutrients > 0;

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

        baselineMaxConcurrentBranches = maxConcurrentBranches;
    }

    private void OnEnable()
    {
        if (starProtection != null && maxIncreaseOnFirstStar > 0)
        {
            starProtection.StarCollected += OnStarCollected;
        }
    }

    private void OnDisable()
    {
        if (starProtection != null)
        {
            starProtection.StarCollected -= OnStarCollected;
        }
    }

    public void ResetNutrients()
    {
        maxConcurrentBranches = baselineMaxConcurrentBranches;
        firstStarBonusApplied = false;
        RaiseSlotsChanged();
    }

    public bool CanStartNewBranch()
    {
        return AvailableNutrients > 0;
    }

    public bool CanPrune(AuthoredTreeSegment segment)
    {
        return segment != null && segment.CanBePruned;
    }

    public void RefreshSlots()
    {
        RaiseSlotsChanged();
    }

    public int CountActiveBranches()
    {
        if (tree == null)
        {
            return 0;
        }

        AuthoredTreeSegment[] segments = tree.GetSegments();
        int count = 0;
        for (int i = 0; i < segments.Length; i++)
        {
            AuthoredTreeSegment segment = segments[i];
            if (segment != null && segment.OccupiesBranchSlot)
            {
                count++;
            }
        }

        return count;
    }

    private void OnStarCollected(int collectedCount)
    {
        if (firstStarBonusApplied || maxIncreaseOnFirstStar <= 0 || collectedCount < 1)
        {
            return;
        }

        firstStarBonusApplied = true;
        maxConcurrentBranches = baselineMaxConcurrentBranches + maxIncreaseOnFirstStar;
        RefreshSlots();
    }

    private void RaiseSlotsChanged()
    {
        SlotsChanged?.Invoke(ActiveBranchCount, AvailableNutrients);
    }
}
