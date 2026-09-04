using System;
using UnityEngine;

/// <summary>
/// Pauses trunk growth after two choice branches start, until the player prunes one of them.
/// </summary>
public class BranchChoiceGate : MonoBehaviour
{
    private enum GateState
    {
        Idle,
        SlowingTrunk,
        WaitingForChoice,
        Resolved
    }

    [SerializeField] private AuthoredTreeController tree;
    [SerializeField] private AuthoredTreeSegment trunk;
    [SerializeField] private AuthoredTreeSegment[] choiceBranches = Array.Empty<AuthoredTreeSegment>();
    [SerializeField] private float trunkSlowdownDuration = 2.5f;
    [SerializeField] private float pruneFadeDuration = 0.8f;

    private GateState gateState = GateState.Idle;
    private float slowdownElapsed;

    public bool IsWaitingForChoice => gateState == GateState.SlowingTrunk || gateState == GateState.WaitingForChoice;
    public bool IsResolved => gateState == GateState.Resolved;

    private void Awake()
    {
        if (tree == null)
        {
            tree = GetComponent<AuthoredTreeController>();
        }

        if (trunk == null && tree != null)
        {
            trunk = tree.GetComponentInChildren<AuthoredTreeSegment>();
        }
    }

    private void OnEnable()
    {
        SubscribeChoiceBranches();
    }

    private void OnDisable()
    {
        UnsubscribeChoiceBranches();
    }

    private void Update()
    {
        if (gateState != GateState.SlowingTrunk || trunk == null)
        {
            return;
        }

        slowdownElapsed += Time.deltaTime;
        float t = trunkSlowdownDuration <= 0f
            ? 1f
            : Mathf.Clamp01(slowdownElapsed / trunkSlowdownDuration);
        float speed = 1f - t;
        trunk.SetGrowthSpeedMultiplier(speed);

        if (t >= 1f)
        {
            trunk.SetGrowthSpeedMultiplier(0f);
            gateState = GateState.WaitingForChoice;
        }
    }

    public void ResetGate()
    {
        slowdownElapsed = 0f;
        gateState = GateState.Idle;

        if (trunk != null)
        {
            trunk.SetGrowthSpeedMultiplier(1f);
        }
    }

    public bool CanPrune(AuthoredTreeSegment segment)
    {
        if (segment == null)
        {
            return false;
        }

        if (gateState == GateState.Resolved || gateState == GateState.Idle)
        {
            return segment.CanBePruned;
        }

        return IsChoiceBranch(segment) && segment.CanBePruned;
    }

    /// <returns>True if this gate handled the prune request.</returns>
    public bool TryHandlePrune(AuthoredTreeSegment segment)
    {
        if (segment == null || !IsChoiceBranch(segment) || !segment.CanBePruned)
        {
            return false;
        }

        if (gateState == GateState.Idle)
        {
            return false;
        }

        if (gateState == GateState.Resolved)
        {
            return false;
        }

        ResolveWithPrune(segment);
        return true;
    }

    private void SubscribeChoiceBranches()
    {
        for (int i = 0; i < choiceBranches.Length; i++)
        {
            if (choiceBranches[i] != null)
            {
                choiceBranches[i].RevealStarted += OnChoiceBranchRevealStarted;
            }
        }
    }

    private void UnsubscribeChoiceBranches()
    {
        for (int i = 0; i < choiceBranches.Length; i++)
        {
            if (choiceBranches[i] != null)
            {
                choiceBranches[i].RevealStarted -= OnChoiceBranchRevealStarted;
            }
        }
    }

    private void OnChoiceBranchRevealStarted(AuthoredTreeSegment segment)
    {
        if (gateState != GateState.Idle || !AllChoiceBranchesTriggered())
        {
            return;
        }

        gateState = GateState.SlowingTrunk;
        slowdownElapsed = 0f;
    }

    private bool AllChoiceBranchesTriggered()
    {
        if (choiceBranches == null || choiceBranches.Length == 0)
        {
            return false;
        }

        for (int i = 0; i < choiceBranches.Length; i++)
        {
            AuthoredTreeSegment branch = choiceBranches[i];
            if (branch == null || branch.State == AuthoredTreeSegment.SegmentState.Locked)
            {
                return false;
            }
        }

        return true;
    }

    private bool IsChoiceBranch(AuthoredTreeSegment segment)
    {
        for (int i = 0; i < choiceBranches.Length; i++)
        {
            if (choiceBranches[i] == segment)
            {
                return true;
            }
        }

        return false;
    }

    private void ResolveWithPrune(AuthoredTreeSegment prunedBranch)
    {
        gateState = GateState.Resolved;

        if (trunk != null)
        {
            trunk.SetGrowthSpeedMultiplier(1f);
        }

        for (int i = 0; i < choiceBranches.Length; i++)
        {
            AuthoredTreeSegment branch = choiceBranches[i];
            if (branch == null)
            {
                continue;
            }

            if (branch == prunedBranch)
            {
                branch.PruneWithFade(pruneFadeDuration);
            }
        }
    }
}
