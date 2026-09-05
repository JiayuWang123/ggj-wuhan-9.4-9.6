using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Drives a fully authored tree: trunk and branches reveal on designer-defined triggers.
/// </summary>
public class AuthoredTreeController : MonoBehaviour
{
    [SerializeField] private AuthoredTreeSegment trunkSegment;
    [SerializeField] private bool autoStartOnPlay = true;
    [SerializeField] private bool collectSegmentsFromChildren = true;
    [SerializeField] private List<AuthoredTreeSegment> segments = new List<AuthoredTreeSegment>();
    [SerializeField] private float defaultPruneFadeDuration = 0.8f;

    private BranchNutrientBudget nutrientBudget;
    private BranchRevealSequence revealSequence;
    private StarProtectionSystem starProtection;

    private void Awake()
    {
        nutrientBudget = GetComponent<BranchNutrientBudget>();
        revealSequence = GetComponent<BranchRevealSequence>();
        starProtection = GetComponent<StarProtectionSystem>();

        if (collectSegmentsFromChildren)
        {
            segments.Clear();
            segments.AddRange(GetComponentsInChildren<AuthoredTreeSegment>(true));
        }
    }

    private void Start()
    {
        if (autoStartOnPlay)
        {
            StartGrowth();
        }
    }

    private void Update()
    {
        TryStartWaitingSegments();

        if (Input.GetKeyDown(KeyCode.R))
        {
            RestartTree();
        }
    }

    private void TryStartWaitingSegments()
    {
        if (revealSequence != null && revealSequence.HasOrderedSequence)
        {
            while (true)
            {
                AuthoredTreeSegment nextSegment = revealSequence.GetNextSegmentReadyToStart();
                if (nextSegment == null)
                {
                    break;
                }

                if (nutrientBudget != null && !nutrientBudget.CanStartNewBranch())
                {
                    break;
                }

                nextSegment.BeginReveal();
                nutrientBudget?.TryConsumeNutrient();
            }

            return;
        }

        for (int i = 0; i < segments.Count; i++)
        {
            AuthoredTreeSegment segment = segments[i];
            if (segment == null || segment.State != AuthoredTreeSegment.SegmentState.Locked)
            {
                continue;
            }

            if (!segment.IsTriggerSatisfied())
            {
                continue;
            }

            if (nutrientBudget != null && !nutrientBudget.CanStartNewBranch())
            {
                continue;
            }

            segment.BeginReveal();
            nutrientBudget?.TryConsumeNutrient();
        }
    }

    public AuthoredTreeSegment[] GetSegments()
    {
        return segments.ToArray();
    }

    public int RemainingBranchSlots => nutrientBudget != null ? nutrientBudget.AvailableNutrients : int.MaxValue;

    public void StartGrowth()
    {
        if (trunkSegment == null)
        {
            return;
        }

        if (trunkSegment.State != AuthoredTreeSegment.SegmentState.Locked)
        {
            return;
        }

        if (nutrientBudget != null && !nutrientBudget.CanStartNewBranch())
        {
            return;
        }

        trunkSegment.BeginReveal();
        nutrientBudget?.TryConsumeNutrient();
    }

    public void RestartTree()
    {
        for (int i = 0; i < segments.Count; i++)
        {
            if (segments[i] == null)
            {
                continue;
            }

            segments[i].gameObject.SetActive(true);
            segments[i].HideInstant();
        }

        nutrientBudget?.ResetNutrients();
        starProtection?.ResetProtectionState();

        if (autoStartOnPlay)
        {
            StartGrowth();
        }
        else
        {
            nutrientBudget?.RefreshSlots();
        }
    }

    public void PruneSegment(AuthoredTreeSegment segment)
    {
        if (segment == null)
        {
            return;
        }

        if (nutrientBudget != null && !nutrientBudget.CanPrune(segment))
        {
            return;
        }

        if (!segment.CanBePruned)
        {
            return;
        }

        nutrientBudget?.RefundPrunedSubtree(segment);
        segment.PruneWithFade(defaultPruneFadeDuration);
    }
}
