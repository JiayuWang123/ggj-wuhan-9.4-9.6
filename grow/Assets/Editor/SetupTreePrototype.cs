using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class SetupTreePrototype
{
    private const string ScenePath = "Assets/Scenes/Main.unity";
    private const string BranchPrefabPath = "Assets/Prefabs/Branch.prefab";

    [MenuItem("Tools/GGJ/Setup Tree Prototype")]
    public static void Setup()
    {
        GameObject branchPrefab = EnsureBranchPrefabHasNode();
        if (branchPrefab == null)
        {
            Debug.LogError($"Could not load {BranchPrefabPath}.");
            return;
        }

        EditorSceneManager.OpenScene(ScenePath);

        GameObject treeRoot = GameObject.Find("TreeRoot");
        if (treeRoot == null)
        {
            treeRoot = new GameObject("TreeRoot");
            treeRoot.transform.position = Vector3.zero;
        }

        TreeGrowthController growth = GetOrAdd<TreeGrowthController>(treeRoot);
        SwipePruner pruner = GetOrAdd<SwipePruner>(treeRoot);
        TreeDebugHud hud = GetOrAdd<TreeDebugHud>(treeRoot);

        BranchNode branchNode = branchPrefab.GetComponent<BranchNode>();

        SerializedObject growthSo = new SerializedObject(growth);
        growthSo.FindProperty("branchPrefab").objectReferenceValue = branchNode;
        growthSo.FindProperty("branchContainer").objectReferenceValue = treeRoot.transform;
        growthSo.FindProperty("lightLayerMask").intValue = 1 << 7;
        growthSo.FindProperty("hazardLayerMask").intValue = 1 << 8;
        growthSo.ApplyModifiedPropertiesWithoutUndo();

        SerializedObject prunerSo = new SerializedObject(pruner);
        prunerSo.FindProperty("targetCamera").objectReferenceValue = Camera.main;
        prunerSo.FindProperty("tree").objectReferenceValue = growth;
        prunerSo.FindProperty("branchLayerMask").intValue = 1 << 6;
        prunerSo.ApplyModifiedPropertiesWithoutUndo();

        SerializedObject hudSo = new SerializedObject(hud);
        hudSo.FindProperty("tree").objectReferenceValue = growth;
        hudSo.ApplyModifiedPropertiesWithoutUndo();

        EditorUtility.SetDirty(treeRoot);
        EditorSceneManager.MarkSceneDirty(treeRoot.scene);
        EditorSceneManager.SaveScene(treeRoot.scene);
        AssetDatabase.SaveAssets();

        Debug.Log("Tree prototype setup complete. Press Play, drag the mouse across branches to prune, and press R to restart.");
    }

    private static GameObject EnsureBranchPrefabHasNode()
    {
        GameObject contents = PrefabUtility.LoadPrefabContents(BranchPrefabPath);
        if (contents == null)
        {
            return null;
        }

        bool changed = false;
        BranchNode node = contents.GetComponent<BranchNode>();
        if (node == null)
        {
            node = contents.AddComponent<BranchNode>();
            changed = true;
        }

        SpriteRenderer visual = contents.GetComponentInChildren<SpriteRenderer>();
        if (visual != null)
        {
            SerializedObject nodeSo = new SerializedObject(node);
            nodeSo.FindProperty("visual").objectReferenceValue = visual;
            nodeSo.ApplyModifiedPropertiesWithoutUndo();
            changed = true;
        }

        if (contents.layer != 6)
        {
            SetLayerRecursively(contents, 6);
            changed = true;
        }

        if (changed)
        {
            PrefabUtility.SaveAsPrefabAsset(contents, BranchPrefabPath);
        }

        PrefabUtility.UnloadPrefabContents(contents);
        return AssetDatabase.LoadAssetAtPath<GameObject>(BranchPrefabPath);
    }

    private static T GetOrAdd<T>(GameObject target) where T : Component
    {
        T component = target.GetComponent<T>();
        if (component == null)
        {
            component = target.AddComponent<T>();
        }

        return component;
    }

    private static void SetLayerRecursively(GameObject target, int layer)
    {
        target.layer = layer;
        foreach (Transform child in target.transform)
        {
            SetLayerRecursively(child.gameObject, layer);
        }
    }
}
