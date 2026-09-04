using System.Collections.Generic;
using UnityEngine;

public class BranchNode : MonoBehaviour
{
    [SerializeField] private SpriteRenderer visual;
    [SerializeField] private Color normalColor = new Color(0.34f, 0.22f, 0.12f, 1f);
    [SerializeField] private Color floweringColor = new Color(1f, 0.88f, 0.42f, 1f);

    private readonly List<BranchNode> children = new List<BranchNode>();

    public TreeGrowthController Controller { get; private set; }
    public BranchNode Parent { get; private set; }
    public IReadOnlyList<BranchNode> Children => children;
    public int Depth { get; private set; }
    public float AngleDegrees { get; private set; }
    public float CurrentLength { get; private set; }
    public float TargetLength { get; private set; }
    public float WidthScale { get; private set; }
    public bool IsTip { get; private set; }
    public bool IsFlowering { get; private set; }
    public Vector3 StartPosition { get; private set; }

    public Vector3 TipPosition => StartPosition + transform.up * CurrentLength;
    public float NormalizedGrowth => TargetLength <= 0f ? 1f : Mathf.Clamp01(CurrentLength / TargetLength);

    private void Awake()
    {
        if (visual == null)
        {
            visual = GetComponentInChildren<SpriteRenderer>();
        }
    }

    public void Initialize(
        TreeGrowthController controller,
        BranchNode parent,
        Vector3 startPosition,
        float angleDegrees,
        int depth,
        float targetLength,
        float widthScale)
    {
        Controller = controller;
        Parent = parent;
        StartPosition = startPosition;
        AngleDegrees = angleDegrees;
        Depth = depth;
        CurrentLength = 0.01f;
        TargetLength = Mathf.Max(0.05f, targetLength);
        WidthScale = Mathf.Max(0.05f, widthScale);
        IsTip = true;
        IsFlowering = false;

        if (Parent != null)
        {
            Parent.children.Add(this);
        }

        ApplyTransform();
        SetColor(normalColor);
    }

    public void Grow(float lengthDelta)
    {
        if (!IsTip || IsFlowering)
        {
            return;
        }

        CurrentLength = Mathf.Min(TargetLength, CurrentLength + Mathf.Max(0f, lengthDelta));
        ApplyTransform();
        if (!Controller.CheckEnvironmentAtTip(this) || !IsTip)
        {
            return;
        }

        if (Mathf.Approximately(CurrentLength, TargetLength))
        {
            IsTip = false;
            Controller.NotifyNodeReachedTarget(this);
        }
    }

    public void MarkAsTerminalTip()
    {
        IsTip = false;
        Bloom();
    }

    public void Bloom()
    {
        if (IsFlowering)
        {
            return;
        }

        IsFlowering = true;
        IsTip = false;
        SetColor(floweringColor);
        Controller.NotifyBranchFlowered(this);
    }

    public void DetachFromParent()
    {
        if (Parent != null)
        {
            Parent.children.Remove(this);
            Parent = null;
        }
    }

    public float GetSubtreeLength()
    {
        float length = CurrentLength;
        for (int i = 0; i < children.Count; i++)
        {
            length += children[i].GetSubtreeLength();
        }

        return length;
    }

    public void CollectSubtree(List<BranchNode> result)
    {
        result.Add(this);
        for (int i = 0; i < children.Count; i++)
        {
            children[i].CollectSubtree(result);
        }
    }

    private void ApplyTransform()
    {
        transform.position = StartPosition;
        transform.rotation = Quaternion.Euler(0f, 0f, AngleDegrees);
        transform.localScale = new Vector3(WidthScale, CurrentLength, 1f);
    }

    private void SetColor(Color color)
    {
        if (visual != null)
        {
            visual.color = color;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponent<TreeLight>() != null)
        {
            Bloom();
            return;
        }

        TreeHazard hazard = other.GetComponent<TreeHazard>();
        if (hazard != null)
        {
            Controller.HandleHazardHit(this, hazard);
        }
    }
}
