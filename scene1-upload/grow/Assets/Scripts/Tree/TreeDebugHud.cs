using UnityEngine;

public class TreeDebugHud : MonoBehaviour
{
    [SerializeField] private TreeGrowthController tree;
    [SerializeField] private bool showHud = true;

    private void Awake()
    {
        if (tree == null)
        {
            tree = FindObjectOfType<TreeGrowthController>();
        }
    }

    private void OnGUI()
    {
        if (!showHud || tree == null)
        {
            return;
        }

        Rect rect = new Rect(12f, 12f, 280f, 96f);
        GUI.Box(rect, GUIContent.none);
        GUILayout.BeginArea(rect);
        GUILayout.Label($"Nutrients: {tree.Nutrients:0.0} / {tree.MaxNutrients:0.0}");
        GUILayout.Label($"Branches: {tree.BranchCount}");
        GUILayout.Label($"Growing tips: {tree.ActiveTipCount}");
        GUILayout.Label($"Flowers: {tree.FlowerCount}");
        GUILayout.Label("Drag mouse: prune | R: restart");
        GUILayout.EndArea();
    }
}
