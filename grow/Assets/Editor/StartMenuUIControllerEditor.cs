#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[CustomEditor(typeof(StartMenuUIController))]
public class StartMenuUIControllerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space(8f);
        EditorGUILayout.HelpBox(
            "If you still see old buttons, click Rebuild below. " +
            "You can also use Window > Start Menu > Rebuild UI.",
            MessageType.Info);

        StartMenuUIController controller = (StartMenuUIController)target;

        if (GUILayout.Button("Rebuild UI Now", GUILayout.Height(32f)))
        {
            StartMenuEditorTools.RebuildController(controller, saveScene: true);
        }

        if (GUILayout.Button("Rebuild UI (Do Not Save Scene)", GUILayout.Height(24f)))
        {
            StartMenuEditorTools.RebuildController(controller, saveScene: false);
        }
    }
}
#endif
