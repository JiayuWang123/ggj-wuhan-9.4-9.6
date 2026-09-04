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

    private BranchChoiceGate choiceGate;

    private void Awake()
    {
        choiceGate = GetComponent<BranchChoiceGate>();

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
        for (int i = 0; i < segments.Count; i++)
        {
            AuthoredTreeSegment segment = segments[i];
            if (segment == null || segment.State != AuthoredTreeSegment.SegmentState.Locked)
            {
                continue;
            }

            if (segment.IsTriggerSatisfied())
            {
                segment.BeginReveal();
            }
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            RestartTree();
        }
    }

    public void StartGrowth()
    {
        if (trunkSegment != null)
        {
            trunkSegment.BeginReveal();
        }
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

        if (autoStartOnPlay)
        {
            StartGrowth();
        }

        choiceGate?.ResetGate();
    }

    public void PruneSegment(AuthoredTreeSegment segment)
    {
        if (segment == null)
        {
            return;
        }

        if (choiceGate != null)
        {
            if (choiceGate.TryHandlePrune(segment))
            {
                return;
            }

            if (!choiceGate.CanPrune(segment))
            {
                return;
            }
        }

        if (!segment.CanBePruned)
        {
            return;
        }

        segment.PruneWithFade(defaultPruneFadeDuration);
    }
}
