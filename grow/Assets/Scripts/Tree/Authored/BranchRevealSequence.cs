using UnityEngine;

/// <summary>
/// Enforces a designer-defined global order for branch triggers.
/// A segment can only start after every earlier step has started, been pruned,
/// or is itself waiting only on a prune requirement.
/// </summary>
public class BranchRevealSequence : MonoBehaviour
{
    [SerializeField] private AuthoredTreeSegment[] orderedSegments;

    public bool HasOrderedSequence => orderedSegments != null && orderedSegments.Length > 0;

    public bool IsBlockedBySequence(AuthoredTreeSegment segment)
    {
        int index = IndexOf(segment);
        if (index <= 0)
        {
            return false;
        }

        for (int i = 0; i < index; i++)
        {
            if (!IsSequenceStepComplete(orderedSegments[i]))
            {
                return true;
            }
        }

        return false;
    }

    public AuthoredTreeSegment GetNextSegmentReadyToStart()
    {
        if (!HasOrderedSequence)
        {
            return null;
        }

        for (int i = 0; i < orderedSegments.Length; i++)
        {
            AuthoredTreeSegment segment = orderedSegments[i];
            if (segment == null)
            {
                continue;
            }

            if (segment.State != AuthoredTreeSegment.SegmentState.Locked)
            {
                continue;
            }

            if (segment.WasPruned || !segment.gameObject.activeInHierarchy)
            {
                continue;
            }

            if (!ArePreviousStepsComplete(i))
            {
                return null;
            }

            if (!segment.IsTriggerSatisfied())
            {
                continue;
            }

            return segment;
        }

        return null;
    }

    private bool ArePreviousStepsComplete(int index)
    {
        for (int i = 0; i < index; i++)
        {
            if (!IsSequenceStepComplete(orderedSegments[i]))
            {
                return false;
            }
        }

        return true;
    }

    private bool IsSequenceStepComplete(AuthoredTreeSegment segment)
    {
        if (segment == null)
        {
            return true;
        }

        if (segment.WasPruned || !segment.gameObject.activeInHierarchy)
        {
            return true;
        }

        if (segment.State != AuthoredTreeSegment.SegmentState.Locked)
        {
            return true;
        }

        return segment.IsWaitingOnlyOnPruneRequirement();
    }

    private int IndexOf(AuthoredTreeSegment segment)
    {
        if (segment == null || orderedSegments == null)
        {
            return -1;
        }

        for (int i = 0; i < orderedSegments.Length; i++)
        {
            if (orderedSegments[i] == segment)
            {
                return i;
            }
        }

        return -1;
    }
}
