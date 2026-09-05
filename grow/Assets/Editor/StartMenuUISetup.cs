#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class StartMenuEditorTools
{
    private const string ScenePath = "Assets/Scenes/StartMenu.unity";

    static StartMenuEditorTools()
    {
        EditorSceneManager.sceneOpened += OnSceneOpened;
        EditorApplication.delayCall += TryRebuildActiveSceneOnce;
    }

    [MenuItem("Window/Start Menu/Open Scene")]
    public static void OpenScene()
    {
        if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            EditorSceneManager.OpenScene(ScenePath);
        }
    }

    [MenuItem("Window/Start Menu/Rebuild UI")]
    public static void RebuildFromMenu()
    {
        StartMenuUIController controller = FindControllerInActiveScene();
        if (controller == null)
        {
            EditorUtility.DisplayDialog(
                "Start Menu",
                "Open Assets/Scenes/StartMenu.unity first.",
                "OK");
            return;
        }

        RebuildController(controller, saveScene: true);
    }

    [MenuItem("GameObject/UI/Start Menu/Rebuild UI", false, 0)]
    public static void RebuildFromGameObjectMenu()
    {
        RebuildFromMenu();
    }

    public static void RebuildController(StartMenuUIController controller, bool saveScene)
    {
        if (controller == null)
        {
            return;
        }

        Undo.IncrementCurrentGroup();
        Undo.SetCurrentGroupName("Rebuild StartMenu UI");

        controller.RebuildUI();

        EditorUtility.SetDirty(controller.gameObject);
        EditorSceneManager.MarkSceneDirty(controller.gameObject.scene);

        if (saveScene)
        {
            EditorSceneManager.SaveScene(controller.gameObject.scene);
        }

        Debug.Log("[StartMenu] UI rebuilt from editor menu.");
    }

    private static void OnSceneOpened(Scene scene, OpenSceneMode mode)
    {
        if (!IsStartMenuScene(scene))
        {
            return;
        }

        EditorApplication.delayCall += () =>
        {
            StartMenuUIController controller = FindControllerInScene(scene);
            if (controller != null && controller.NeedsRebuild())
            {
                RebuildController(controller, saveScene: true);
            }
        };
    }

    private static void TryRebuildActiveSceneOnce()
    {
        try
        {
            StartMenuUIController controller = FindControllerInActiveScene();
            if (controller != null && controller.NeedsRebuild())
            {
                RebuildController(controller, saveScene: true);
            }
        }
        catch (System.SystemException exception)
        {
            Debug.LogWarning($"[StartMenu] Auto rebuild skipped: {exception.Message}");
        }
    }

    private static StartMenuUIController FindControllerInActiveScene()
    {
        Scene activeScene = SceneManager.GetActiveScene();
        return activeScene.IsValid() ? FindControllerInScene(activeScene) : null;
    }

    private static StartMenuUIController FindControllerInScene(Scene scene)
    {
        if (!scene.IsValid())
        {
            return null;
        }

        foreach (GameObject root in scene.GetRootGameObjects())
        {
            StartMenuUIController controller = root.GetComponentInChildren<StartMenuUIController>(true);
            if (controller != null)
            {
                return controller;
            }
        }

        return null;
    }

    private static bool IsStartMenuScene(Scene scene)
    {
        return scene.path.Replace('\\', '/').EndsWith("Assets/Scenes/StartMenu.unity");
    }
}
#endif
