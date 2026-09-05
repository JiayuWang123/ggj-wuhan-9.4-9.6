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
    }

    public void ResetNutrients()
    {
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

    private void RaiseSlotsChanged()
    {
        SlotsChanged?.Invoke(ActiveBranchCount, AvailableNutrients);
    }
}
