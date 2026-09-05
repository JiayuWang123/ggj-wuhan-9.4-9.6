#if UNITY_EDITOR
using System.Collections.Generic;
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

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("碰撞体", EditorStyles.boldLabel);

        if (GUILayout.Button("替换为多边形碰撞 (PolygonCollider2D)"))
        {
            AuthoredColliderUtility.ReplaceWithPolygonCollider(segment);
        }

        if (GUILayout.Button("从 Sprite 重新生成碰撞形状"))
        {
            AuthoredColliderUtility.ResetPolygonFromSprite(segment);
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

    [MenuItem("Tools/GGJ/Replace Selected Segment Colliders With Polygon")]
    private static void ReplaceSelectedColliders()
    {
        AuthoredTreeSegment[] segments = Selection.GetFiltered<AuthoredTreeSegment>(SelectionMode.Editable | SelectionMode.ExcludePrefab);
        for (int i = 0; i < segments.Length; i++)
        {
            AuthoredColliderUtility.ReplaceWithPolygonCollider(segments[i]);
        }
    }

    [MenuItem("Tools/GGJ/Convert Current Scene Segments To Polygon Colliders")]
    private static void ConvertCurrentSceneColliders()
    {
        AuthoredTreeSegment[] segments = Object.FindObjectsByType<AuthoredTreeSegment>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        for (int i = 0; i < segments.Length; i++)
        {
            AuthoredColliderUtility.ReplaceWithPolygonCollider(segments[i]);
        }

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

        Debug.Log($"Converted {segments.Length} authored segment colliders to PolygonCollider2D.");
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

        AuthoredTreeSegment segment = Undo.AddComponent<AuthoredTreeSegment>(segmentGo);
        PolygonCollider2D collider = AuthoredColliderUtility.CreatePolygonColliderOnSegment(segmentGo, spriteRenderer);
        WireSegment(segment, parent, trigger, spriteRenderer, visualGo.transform, collider);
        WireTrigger(trigger, segment);

        EditorUtility.SetDirty(segmentGo);
        EditorUtility.SetDirty(visualGo);
        EditorUtility.SetDirty(segment);

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
        Collider2D collider)
    {
        SerializedObject serializedSegment = new SerializedObject(segment);
        serializedSegment.FindProperty("parentSegment").objectReferenceValue = parent;
        serializedSegment.FindProperty("startImmediatelyOnPlay").boolValue = false;
        serializedSegment.FindProperty("trigger").objectReferenceValue = trigger;
        serializedSegment.FindProperty("spriteRenderer").objectReferenceValue = spriteRenderer;
        serializedSegment.FindProperty("revealTarget").objectReferenceValue = revealTarget;
        serializedSegment.FindProperty("pruneCollider").objectReferenceValue = collider;
        serializedSegment.FindProperty("revealMode").enumValueIndex = (int)AuthoredTreeSegment.RevealMode.ClipFromBottom;
        serializedSegment.ApplyModifiedProperties();
    }

    private static void WireTrigger(TreeRevealTrigger trigger, AuthoredTreeSegment ownerSegment)
    {
        SerializedObject serializedTrigger = new SerializedObject(trigger);
        serializedTrigger.FindProperty("ownerSegment").objectReferenceValue = ownerSegment;
        serializedTrigger.ApplyModifiedProperties();
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

public static class AuthoredColliderUtility
{
    public static void ReplaceWithPolygonCollider(AuthoredTreeSegment segment)
    {
        if (segment == null)
        {
            return;
        }

        SpriteRenderer spriteRenderer = segment.GetComponentInChildren<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            EditorUtility.DisplayDialog("Authored Tree", "找不到 SpriteRenderer。", "OK");
            return;
        }

        GameObject segmentGo = segment.gameObject;
        GameObject visualGo = spriteRenderer.gameObject;
        RemoveBoxColliders(segmentGo, visualGo);
        RemovePolygonColliderFromVisual(visualGo);

        PolygonCollider2D polygon = CreatePolygonColliderOnSegment(segmentGo, spriteRenderer);
        AssignPruneCollider(segment, polygon);
        EditorUtility.SetDirty(segment);
        EditorUtility.SetDirty(segmentGo);
    }

    public static void ResetPolygonFromSprite(AuthoredTreeSegment segment)
    {
        if (segment == null)
        {
            return;
        }

        SpriteRenderer spriteRenderer = segment.GetComponentInChildren<SpriteRenderer>();
        if (spriteRenderer == null || spriteRenderer.sprite == null)
        {
            EditorUtility.DisplayDialog("Authored Tree", "找不到 Sprite。", "OK");
            return;
        }

        GameObject segmentGo = segment.gameObject;
        PolygonCollider2D polygon = segmentGo.GetComponent<PolygonCollider2D>();
        if (polygon == null)
        {
            ReplaceWithPolygonCollider(segment);
            return;
        }

        ApplySpritePhysicsShapeToSegment(polygon, spriteRenderer);
        polygon.isTrigger = true;
        AssignPruneCollider(segment, polygon);
        EditorUtility.SetDirty(segment);
        EditorUtility.SetDirty(segmentGo);
    }

    public static PolygonCollider2D CreatePolygonColliderOnSegment(GameObject segmentGo, SpriteRenderer spriteRenderer)
    {
        if (segmentGo == null || spriteRenderer == null)
        {
            return null;
        }

        GameObject visualGo = spriteRenderer.gameObject;
        RemoveBoxColliders(segmentGo, visualGo);
        RemovePolygonColliderFromVisual(visualGo);

        PolygonCollider2D polygon = segmentGo.GetComponent<PolygonCollider2D>();
        if (polygon == null)
        {
            polygon = Undo.AddComponent<PolygonCollider2D>(segmentGo);
        }

        polygon.isTrigger = true;
        if (spriteRenderer.sprite != null)
        {
            ApplySpritePhysicsShapeToSegment(polygon, spriteRenderer);
        }

        return polygon;
    }

    public static PolygonCollider2D CreatePolygonColliderOnVisual(GameObject visualGo, SpriteRenderer spriteRenderer)
    {
        return CreatePolygonColliderOnSegment(visualGo.transform.parent != null ? visualGo.transform.parent.gameObject : visualGo, spriteRenderer);
    }

    private static void ApplySpritePhysicsShapeToSegment(PolygonCollider2D polygon, SpriteRenderer spriteRenderer)
    {
        Sprite sprite = spriteRenderer.sprite;
        Transform segmentTransform = polygon.transform;
        Transform visualTransform = spriteRenderer.transform;
        int shapeCount = sprite.GetPhysicsShapeCount();

        if (shapeCount <= 0)
        {
            ApplyBoundsFallbackPolygon(polygon, sprite, visualTransform, segmentTransform);
            return;
        }

        polygon.pathCount = shapeCount;
        List<Vector2> path = new List<Vector2>();
        for (int shapeIndex = 0; shapeIndex < shapeCount; shapeIndex++)
        {
            path.Clear();
            sprite.GetPhysicsShape(shapeIndex, path);
            TransformPathToSegment(path, visualTransform, segmentTransform);
            polygon.SetPath(shapeIndex, path);
        }
    }

    private static void ApplyBoundsFallbackPolygon(
        PolygonCollider2D polygon,
        Sprite sprite,
        Transform visualTransform,
        Transform segmentTransform)
    {
        Bounds bounds = sprite.bounds;
        Vector2 min = bounds.min;
        Vector2 max = bounds.max;
        List<Vector2> path = new List<Vector2>
        {
            min,
            new Vector2(max.x, min.y),
            max,
            new Vector2(min.x, max.y)
        };
        TransformPathToSegment(path, visualTransform, segmentTransform);
        polygon.pathCount = 1;
        polygon.SetPath(0, path);
    }

    private static void TransformPathToSegment(List<Vector2> path, Transform visualTransform, Transform segmentTransform)
    {
        for (int i = 0; i < path.Count; i++)
        {
            Vector3 worldPoint = visualTransform.TransformPoint(path[i]);
            path[i] = segmentTransform.InverseTransformPoint(worldPoint);
        }
    }

    private static void ApplySpritePhysicsShape(PolygonCollider2D polygon, Sprite sprite)
    {
        int shapeCount = sprite.GetPhysicsShapeCount();
        if (shapeCount <= 0)
        {
            Bounds bounds = sprite.bounds;
            Vector2 min = bounds.min;
            Vector2 max = bounds.max;
            polygon.pathCount = 1;
            polygon.SetPath(0, new Vector2[]
            {
                min,
                new Vector2(max.x, min.y),
                max,
                new Vector2(min.x, max.y)
            });
            return;
        }

        polygon.pathCount = shapeCount;
        List<Vector2> path = new List<Vector2>();
        for (int shapeIndex = 0; shapeIndex < shapeCount; shapeIndex++)
        {
            path.Clear();
            sprite.GetPhysicsShape(shapeIndex, path);
            polygon.SetPath(shapeIndex, path);
        }
    }

    private static void RemovePolygonColliderFromVisual(GameObject visualGo)
    {
        PolygonCollider2D visualPolygon = visualGo.GetComponent<PolygonCollider2D>();
        if (visualPolygon != null)
        {
            Undo.DestroyObjectImmediate(visualPolygon);
        }
    }

    private static void RemoveBoxColliders(GameObject segmentGo, GameObject visualGo)
    {
        BoxCollider2D segmentBox = segmentGo.GetComponent<BoxCollider2D>();
        if (segmentBox != null)
        {
            Undo.DestroyObjectImmediate(segmentBox);
        }

        BoxCollider2D visualBox = visualGo.GetComponent<BoxCollider2D>();
        if (visualBox != null)
        {
            Undo.DestroyObjectImmediate(visualBox);
        }
    }

    private static void AssignPruneCollider(AuthoredTreeSegment segment, Collider2D collider)
    {
        SerializedObject serializedSegment = new SerializedObject(segment);
        serializedSegment.FindProperty("pruneCollider").objectReferenceValue = collider;
        serializedSegment.ApplyModifiedProperties();
    }
}
#endif
