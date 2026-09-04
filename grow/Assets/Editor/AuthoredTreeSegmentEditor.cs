#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(AuthoredTreeSegment))]
public class AuthoredTreeSegmentEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space();
        AuthoredTreeSegment segment = (AuthoredTreeSegment)target;
        if (GUILayout.Button("从此段添加子分支", GUILayout.Height(28)))
        {
            AuthoredBranchBuilder.AddChildBranch(segment);
        }
    }
}

public static class AuthoredBranchBuilder
{
    private const string DefaultBranchSpritePath = "Assets/Art/Trees/Level01/03_branch_left.png";

    [MenuItem("GameObject/GGJ/Add Authored Branch", false, 10)]
    private static void AddBranchFromMenu()
    {
        AuthoredTreeSegment parent = Selection.activeGameObject != null
            ? Selection.activeGameObject.GetComponent<AuthoredTreeSegment>()
            : null;

        if (parent == null)
        {
            EditorUtility.DisplayDialog(
                "Authored Tree",
                "请先选中一个带 AuthoredTreeSegment 的父段（主干或已有分枝）。",
                "OK");
            return;
        }

        AddChildBranch(parent);
    }

    [MenuItem("GameObject/GGJ/Add Authored Branch", true)]
    private static bool ValidateAddBranchFromMenu()
    {
        return Selection.activeGameObject != null
            && Selection.activeGameObject.GetComponent<AuthoredTreeSegment>() != null;
    }

    public static void AddChildBranch(AuthoredTreeSegment parent, Sprite sprite = null)
    {
        if (parent == null)
        {
            return;
        }

        AuthoredTreeController tree = parent.GetComponentInParent<AuthoredTreeController>();
        if (tree == null)
        {
            EditorUtility.DisplayDialog(
                "Authored Tree",
                "父段必须在 AuthoredTree 物体下面。",
                "OK");
            return;
        }

        sprite ??= LoadDefaultSprite();
        int branchIndex = parent.GetComponentsInChildren<TreeRevealTrigger>(true).Length + 1;
        Vector3 localJunction = ComputeNextJunctionLocal(parent);

        GameObject triggerGo = new GameObject($"Trigger{branchIndex}");
        Undo.RegisterCreatedObjectUndo(triggerGo, "Add Authored Branch");
        triggerGo.transform.SetParent(parent.transform, false);
        triggerGo.transform.localPosition = localJunction;
        triggerGo.transform.localRotation = Quaternion.identity;
        TreeRevealTrigger trigger = triggerGo.AddComponent<TreeRevealTrigger>();

        GameObject segmentGo = new GameObject($"Segment_Branch{branchIndex}");
        Undo.RegisterCreatedObjectUndo(segmentGo, "Add Authored Branch");
        int branchLayer = LayerMask.NameToLayer("Branch");
        if (branchLayer >= 0)
        {
            segmentGo.layer = branchLayer;
        }

        segmentGo.transform.SetParent(tree.transform, false);
        segmentGo.transform.position = triggerGo.transform.position;
        segmentGo.transform.localRotation = Quaternion.identity;

        GameObject visualGo = new GameObject("Visual");
        visualGo.transform.SetParent(segmentGo.transform, false);
        visualGo.transform.localPosition = Vector3.zero;
        visualGo.transform.localRotation = Quaternion.identity;
        visualGo.transform.localScale = Vector3.one;
        if (branchLayer >= 0)
        {
            visualGo.layer = branchLayer;
        }

        SpriteRenderer spriteRenderer = visualGo.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = sprite;
        spriteRenderer.sortingOrder = 0;

        BoxCollider2D collider = segmentGo.AddComponent<BoxCollider2D>();
        collider.isTrigger = true;
        FitColliderToSprite(collider, spriteRenderer);

        AuthoredTreeSegment segment = segmentGo.AddComponent<AuthoredTreeSegment>();
        WireSegment(segment, parent, trigger, spriteRenderer, visualGo.transform, collider);
        WireTrigger(trigger, segment);

        Selection.activeGameObject = segmentGo;
        EditorGUIUtility.PingObject(segmentGo);
        SceneView.lastActiveSceneView?.FrameSelected();
    }

    private static void WireSegment(
        AuthoredTreeSegment segment,
        AuthoredTreeSegment parent,
        TreeRevealTrigger trigger,
        SpriteRenderer spriteRenderer,
        Transform revealTarget,
        BoxCollider2D collider)
    {
        SerializedObject serializedSegment = new SerializedObject(segment);
        serializedSegment.FindProperty("parentSegment").objectReferenceValue = parent;
        serializedSegment.FindProperty("startImmediatelyOnPlay").boolValue = false;
        serializedSegment.FindProperty("trigger").objectReferenceValue = trigger;
        serializedSegment.FindProperty("spriteRenderer").objectReferenceValue = spriteRenderer;
        serializedSegment.FindProperty("revealTarget").objectReferenceValue = revealTarget;
        serializedSegment.FindProperty("pruneCollider").objectReferenceValue = collider;
        serializedSegment.FindProperty("revealMode").enumValueIndex = (int)AuthoredTreeSegment.RevealMode.ClipFromBottom;
        serializedSegment.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void WireTrigger(TreeRevealTrigger trigger, AuthoredTreeSegment ownerSegment)
    {
        SerializedObject serializedTrigger = new SerializedObject(trigger);
        serializedTrigger.FindProperty("ownerSegment").objectReferenceValue = ownerSegment;
        serializedTrigger.ApplyModifiedPropertiesWithoutUndo();
    }

    private static Vector3 ComputeNextJunctionLocal(AuthoredTreeSegment parent)
    {
        TreeRevealTrigger[] existingTriggers = parent.GetComponentsInChildren<TreeRevealTrigger>(true);
        if (existingTriggers.Length > 0)
        {
            float highestY = float.MinValue;
            float referenceX = 0f;
            for (int i = 0; i < existingTriggers.Length; i++)
            {
                Vector3 local = parent.transform.InverseTransformPoint(existingTriggers[i].transform.position);
                if (local.y > highestY)
                {
                    highestY = local.y;
                    referenceX = local.x;
                }
            }

            return new Vector3(referenceX, highestY + 1.5f, 0f);
        }

        SpriteRenderer parentSprite = parent.GetComponentInChildren<SpriteRenderer>();
        if (parentSprite != null && parentSprite.sprite != null)
        {
            Bounds bounds = parentSprite.sprite.bounds;
            return new Vector3(bounds.center.x, Mathf.Lerp(bounds.min.y, bounds.max.y, 0.45f), 0f);
        }

        return new Vector3(0f, 1f, 0f);
    }

    private static void FitColliderToSprite(BoxCollider2D collider, SpriteRenderer spriteRenderer)
    {
        if (spriteRenderer == null || spriteRenderer.sprite == null)
        {
            return;
        }

        Bounds bounds = spriteRenderer.sprite.bounds;
        collider.size = bounds.size;
        collider.offset = bounds.center;
    }

    private static Sprite LoadDefaultSprite()
    {
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(DefaultBranchSpritePath);
        if (sprite == null)
        {
            Debug.LogWarning($"未找到默认分枝贴图：{DefaultBranchSpritePath}");
        }

        return sprite;
    }
}
#endif
