using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// When a star trigger/collider overlaps a branch prune collider,
/// locks pruning for that branch and its parent chain.
/// </summary>
public class StarProtectionSystem : MonoBehaviour
{
    [SerializeField] private AuthoredTreeController tree;
    [SerializeField] private StarProtectionMarker[] starMarkers = System.Array.Empty<StarProtectionMarker>();

    private const int MaxCollectibleStars = 3;

    private readonly HashSet<int> activatedPairs = new HashSet<int>();
    private readonly HashSet<int> collectedStarIds = new HashSet<int>();
    private AuthoredTreeSegment[] segments = System.Array.Empty<AuthoredTreeSegment>();

    public int CollectedStarCount { get; private set; }
    public int MaxStarCount => MaxCollectibleStars;

    public event Action<int> StarCollected;

    private void Awake()
    {
        if (tree == null)
        {
            tree = GetComponent<AuthoredTreeController>();
        }

        if (starMarkers == null || starMarkers.Length == 0)
        {
            starMarkers = GetComponentsInChildren<StarProtectionMarker>(true);
        }
    }

    private void OnEnable()
    {
        BindSegments();
    }

    private void Start()
    {
        BindSegments();
    }

    private void OnDisable()
    {
        UnbindSegments();
    }

    private void Update()
    {
        if (starMarkers == null || starMarkers.Length == 0)
        {
            return;
        }

        RefreshSegmentsIfNeeded();
        CheckStarColliderContacts();
    }

    public void ResetProtectionState()
    {
        activatedPairs.Clear();
        collectedStarIds.Clear();
        CollectedStarCount = 0;
        StarCollected?.Invoke(CollectedStarCount);
        RefreshSegmentsIfNeeded();

        for (int i = 0; i < segments.Length; i++)
        {
            if (segments[i] != null)
            {
                segments[i].ClearProtectedFromPruning();
            }
        }
    }

    private void BindSegments()
    {
        UnbindSegments();
        RefreshSegmentsIfNeeded();

        for (int i = 0; i < segments.Length; i++)
        {
            if (segments[i] != null)
            {
                segments[i].RevealStarted += OnSegmentRevealStarted;
            }
        }
    }

    private void UnbindSegments()
    {
        for (int i = 0; i < segments.Length; i++)
        {
            if (segments[i] != null)
            {
                segments[i].RevealStarted -= OnSegmentRevealStarted;
            }
        }
    }

    private void RefreshSegmentsIfNeeded()
    {
        if (tree == null)
        {
            segments = System.Array.Empty<AuthoredTreeSegment>();
            return;
        }

        segments = tree.GetSegments();
    }

    private void OnSegmentRevealStarted(AuthoredTreeSegment segment)
    {
        CheckStarColliderContacts();
    }

    private void CheckStarColliderContacts()
    {
        for (int starIndex = 0; starIndex < starMarkers.Length; starIndex++)
        {
            StarProtectionMarker star = starMarkers[starIndex];
            if (star == null)
            {
                continue;
            }

            for (int segmentIndex = 0; segmentIndex < segments.Length; segmentIndex++)
            {
                AuthoredTreeSegment segment = segments[segmentIndex];
                if (!IsSegmentEligibleForStarCheck(segment))
                {
                    continue;
                }

                Collider2D branchCollider = segment.PruneCollider;
                if (branchCollider == null || !branchCollider.enabled)
                {
                    continue;
                }

                if (!segment.IsRevealedPortionTouchingStar(
                        star.transform.position,
                        star.ActivationRadius,
                        star.ContactCollider))
                {
                    continue;
                }

                int pairKey = BuildPairKey(star, segment);
                if (activatedPairs.Contains(pairKey))
                {
                    continue;
                }

                TryCollectStar(star);
                activatedPairs.Add(pairKey);
                AuthoredTreeSegment.ProtectBranchChainToRoot(segment);
            }
        }
    }

    private void TryCollectStar(StarProtectionMarker star)
    {
        int starId = star.GetInstanceID();
        if (collectedStarIds.Contains(starId) || CollectedStarCount >= MaxCollectibleStars)
        {
            return;
        }

        collectedStarIds.Add(starId);
        CollectedStarCount = collectedStarIds.Count;
        StarCollected?.Invoke(CollectedStarCount);
    }

    private static bool IsSegmentEligibleForStarCheck(AuthoredTreeSegment segment)
    {
        if (segment == null || segment.WasPruned || !segment.gameObject.activeInHierarchy)
        {
            return false;
        }

        return segment.State == AuthoredTreeSegment.SegmentState.Revealing
            || segment.State == AuthoredTreeSegment.SegmentState.Complete;
    }

    private static int BuildPairKey(StarProtectionMarker star, AuthoredTreeSegment segment)
    {
        return (star.GetInstanceID() * 397) ^ segment.GetInstanceID();
    }
}
