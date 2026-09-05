using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
[DisallowMultipleComponent]
public class StartMenuUIController : MonoBehaviour
{
    private const float EdgePadding = 24f;
    private const float BottomButtonWidth = 180f;
    private const float BottomButtonHeight = 56f;
    private const float BottomButtonSpacing = 16f;

    [Header("Gameplay")]
    [SerializeField] private string gameplaySceneName = "2";

    [Header("Placeholder sprites (replace in Inspector or keep defaults)")]
    [SerializeField] private Sprite buttonSprite;
    [SerializeField] private Sprite panelSprite;
    [SerializeField] private Sprite sliderTrackSprite;
    [SerializeField] private Sprite sliderFillSprite;
    [SerializeField] private Sprite sliderHandleSprite;

    private GameController gameController;
    private SoundManager soundManager;
    private bool editModeRebuildScheduled;

    public bool NeedsRebuild()
    {
        if (FindDirectChild("Startkey") != null)
        {
            return true;
        }

        if (FindDirectChild("Volumeup") != null)
        {
            return true;
        }

        if (FindDirectChild("VolumeMute") != null)
        {
            return true;
        }

        if (FindLegacyQuitButton() != null)
        {
            return true;
        }

        return FindDirectChild("VolumePanel") == null
            || FindDirectChild("BottomButtons") == null
            || FindDirectChild("PressAnyKeyHandler") == null
            || FindDirectChild("MenuControllers") == null;
    }

    public void RebuildUI()
    {
        LoadDefaultSprites();
        RemoveManagedWidgets();
        EnsureControllers();
        BuildVolumeSlider();
        BuildBottomButtons();
        EnsurePressAnyKeyHandler();
        EnsureBackgroundOnBottom();
        WirePersistentEventsInEditor();
    }

    private void Awake()
    {
        if (Application.isPlaying && NeedsRebuild())
        {
            RebuildUI();
        }
    }

    private void OnEnable()
    {
        if (Application.isPlaying)
        {
            return;
        }

        ScheduleEditModeRebuild();
    }

    private void ScheduleEditModeRebuild()
    {
        if (editModeRebuildScheduled || !NeedsRebuild())
        {
            return;
        }

        editModeRebuildScheduled = true;
#if UNITY_EDITOR
        UnityEditor.EditorApplication.delayCall += RunScheduledEditModeRebuild;
#endif
    }

#if UNITY_EDITOR
    private void RunScheduledEditModeRebuild()
    {
        editModeRebuildScheduled = false;

        if (this == null || !isActiveAndEnabled || Application.isPlaying)
        {
            return;
        }

        if (!NeedsRebuild())
        {
            return;
        }

        RebuildUI();
        UnityEditor.EditorUtility.SetDirty(gameObject);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
        Debug.Log("[StartMenu] UI rebuilt in edit mode.");
    }
#endif

    private Transform FindLegacyQuitButton()
    {
        if (FindDirectChild("BottomButtons") != null)
        {
            return null;
        }

        Transform legacyQuit = FindDirectChild("QuitButton");
        return legacyQuit;
    }

    private void LoadDefaultSprites()
    {
        buttonSprite ??= LoadUiSprite("Assets/StartMenu/image/UI/placeholder_button.png");
        panelSprite ??= LoadUiSprite("Assets/StartMenu/image/UI/placeholder_panel.png");
        sliderTrackSprite ??= LoadUiSprite("Assets/StartMenu/image/UI/placeholder_slider_track.png");
        sliderFillSprite ??= LoadUiSprite("Assets/StartMenu/image/UI/placeholder_slider_fill.png");
        sliderHandleSprite ??= LoadUiSprite("Assets/StartMenu/image/UI/placeholder_slider_handle.png");
    }

    private static Sprite LoadUiSprite(string assetPath)
    {
#if UNITY_EDITOR
        return UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
#else
        return null;
#endif
    }

    private void RemoveManagedWidgets()
    {
        DestroyIfExists("Startkey");
        DestroyIfExists("Volumeup");
        DestroyIfExists("VolumeMute");
        DestroyIfExists("tool_wand");

        Transform legacyQuit = FindLegacyQuitButton();
        if (legacyQuit != null)
        {
            DestroyObject(legacyQuit.gameObject);
        }

        DestroyIfExists("VolumePanel");
        DestroyIfExists("BottomButtons");
        DestroyIfExists("PressAnyKeyHandler");
        DestroyIfExists("MenuControllers");
    }

    private void DestroyIfExists(string objectName)
    {
        Transform target = FindDirectChild(objectName);
        if (target != null)
        {
            DestroyObject(target.gameObject);
        }
    }

    private Transform FindDirectChild(string objectName)
    {
        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            if (child.name == objectName)
            {
                return child;
            }
        }

        return null;
    }

    private void DestroyObject(GameObject target)
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            UnityEditor.Undo.DestroyObjectImmediate(target);
            return;
        }
#endif

        Destroy(target);
    }

    private void EnsureControllers()
    {
        GameObject controllerRoot = new GameObject("MenuControllers");
        controllerRoot.transform.SetParent(transform, false);
        RegisterCreatedObject(controllerRoot);

        gameController = controllerRoot.AddComponent<GameController>();
        gameController.gameplaySceneName = gameplaySceneName;

        soundManager = controllerRoot.AddComponent<SoundManager>();
    }

    private void BuildVolumeSlider()
    {
        GameObject panel = CreateUiObject("VolumePanel", transform);
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(1f, 1f);
        panelRect.anchorMax = new Vector2(1f, 1f);
        panelRect.pivot = new Vector2(1f, 1f);
        panelRect.anchoredPosition = new Vector2(-EdgePadding, -EdgePadding);
        panelRect.sizeDelta = new Vector2(360f, 88f);

        Image panelImage = panel.AddComponent<Image>();
        panelImage.sprite = panelSprite;
        panelImage.type = Image.Type.Sliced;
        panelImage.color = Color.white;
        panelImage.raycastTarget = true;

        GameObject labelObject = CreateUiObject("Label", panel.transform);
        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0f, 1f);
        labelRect.anchorMax = new Vector2(1f, 1f);
        labelRect.pivot = new Vector2(0.5f, 1f);
        labelRect.anchoredPosition = new Vector2(0f, -10f);
        labelRect.sizeDelta = new Vector2(-24f, 28f);

        Text label = labelObject.AddComponent<Text>();
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.text = "Volume";
        label.alignment = TextAnchor.MiddleCenter;
        label.color = Color.white;
        label.fontSize = 22;
        label.raycastTarget = false;

        GameObject sliderObject = CreateUiObject("VolumeSlider", panel.transform);
        RectTransform sliderRect = sliderObject.GetComponent<RectTransform>();
        sliderRect.anchorMin = new Vector2(0f, 0f);
        sliderRect.anchorMax = new Vector2(1f, 0f);
        sliderRect.pivot = new Vector2(0.5f, 0f);
        sliderRect.anchoredPosition = new Vector2(0f, 14f);
        sliderRect.sizeDelta = new Vector2(-24f, 30f);

        Slider slider = sliderObject.AddComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.wholeNumbers = false;
        slider.value = soundManager.GetSavedVolume();

        GameObject background = CreateUiObject("Background", sliderObject.transform);
        StretchRect(background.GetComponent<RectTransform>());
        Image backgroundImage = background.AddComponent<Image>();
        backgroundImage.sprite = sliderTrackSprite;
        backgroundImage.type = Image.Type.Sliced;
        backgroundImage.color = Color.white;

        GameObject fillArea = CreateUiObject("Fill Area", sliderObject.transform);
        RectTransform fillAreaRect = fillArea.GetComponent<RectTransform>();
        fillAreaRect.anchorMin = new Vector2(0f, 0.25f);
        fillAreaRect.anchorMax = new Vector2(1f, 0.75f);
        fillAreaRect.offsetMin = new Vector2(8f, 0f);
        fillAreaRect.offsetMax = new Vector2(-8f, 0f);

        GameObject fill = CreateUiObject("Fill", fillArea.transform);
        StretchRect(fill.GetComponent<RectTransform>());
        Image fillImage = fill.AddComponent<Image>();
        fillImage.sprite = sliderFillSprite;
        fillImage.type = Image.Type.Sliced;
        fillImage.color = Color.white;

        GameObject handleSlideArea = CreateUiObject("Handle Slide Area", sliderObject.transform);
        StretchRect(handleSlideArea.GetComponent<RectTransform>());

        GameObject handle = CreateUiObject("Handle", handleSlideArea.transform);
        RectTransform handleRect = handle.GetComponent<RectTransform>();
        handleRect.sizeDelta = new Vector2(24f, 24f);
        Image handleImage = handle.AddComponent<Image>();
        handleImage.sprite = sliderHandleSprite;
        handleImage.color = Color.white;

        slider.fillRect = fill.GetComponent<RectTransform>();
        slider.handleRect = handleRect;
        slider.targetGraphic = handleImage;

        WireSliderVolume(slider, soundManager);
    }

    private void BuildBottomButtons()
    {
        GameObject row = CreateUiObject("BottomButtons", transform);
        RectTransform rowRect = row.GetComponent<RectTransform>();
        rowRect.anchorMin = new Vector2(0f, 0f);
        rowRect.anchorMax = new Vector2(0f, 0f);
        rowRect.pivot = new Vector2(0f, 0f);
        rowRect.anchoredPosition = new Vector2(EdgePadding, EdgePadding);

        HorizontalLayoutGroup layout = row.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = BottomButtonSpacing;
        layout.childAlignment = TextAnchor.LowerLeft;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        ContentSizeFitter fitter = row.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        CreateMenuButton(row.transform, "QuitButton", "Quit", gameController, nameof(GameController.QuitGame));
        CreateMenuButton(row.transform, "RestartButton", "Restart", gameController, nameof(GameController.RestartGame));
    }

    private void CreateMenuButton(
        Transform parent,
        string objectName,
        string labelText,
        GameController controller,
        string methodName)
    {
        GameObject buttonObject = CreateUiObject(objectName, parent);
        RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
        buttonRect.sizeDelta = new Vector2(BottomButtonWidth, BottomButtonHeight);

        Image image = buttonObject.AddComponent<Image>();
        image.sprite = buttonSprite;
        image.type = Image.Type.Sliced;
        image.color = Color.white;

        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = image;
        WireButtonClick(button, controller, methodName);

        GameObject textObject = CreateUiObject("Text", buttonObject.transform);
        StretchRect(textObject.GetComponent<RectTransform>());
        Text text = textObject.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.text = labelText;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.black;
        text.fontSize = 24;
        text.raycastTarget = false;
    }

    private void EnsurePressAnyKeyHandler()
    {
        GameObject handler = CreateUiObject("PressAnyKeyHandler", transform);
        StretchRect(handler.GetComponent<RectTransform>());

        Image blocker = handler.AddComponent<Image>();
        blocker.color = new Color(0f, 0f, 0f, 0f);
        blocker.raycastTarget = false;

        PressAnyKeyToStart pressAnyKey = handler.AddComponent<PressAnyKeyToStart>();
        pressAnyKey.nextSceneName = gameplaySceneName;
        pressAnyKey.ignoreWhenPointerOverUi = true;
    }

    private void EnsureBackgroundOnBottom()
    {
        Transform background = transform.Find("Background");
        if (background != null)
        {
            background.SetAsFirstSibling();
        }
    }

    private GameObject CreateUiObject(string objectName, Transform parent)
    {
        GameObject uiObject = new GameObject(objectName, typeof(RectTransform));
        uiObject.transform.SetParent(parent, false);
        uiObject.layer = LayerMask.NameToLayer("UI");
        RegisterCreatedObject(uiObject);
        return uiObject;
    }

    private void RegisterCreatedObject(GameObject uiObject)
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            UnityEditor.Undo.RegisterCreatedObjectUndo(uiObject, "Rebuild StartMenu UI");
        }
#endif
    }

    private void WirePersistentEventsInEditor()
    {
#if UNITY_EDITOR
        if (Application.isPlaying)
        {
            return;
        }

        Transform controllers = transform.Find("MenuControllers");
        if (controllers == null)
        {
            return;
        }

        SoundManager manager = controllers.GetComponent<SoundManager>();
        GameController controller = controllers.GetComponent<GameController>();
        Slider slider = transform.Find("VolumePanel/VolumeSlider")?.GetComponent<Slider>();
        Button quitButton = transform.Find("BottomButtons/QuitButton")?.GetComponent<Button>();
        Button restartButton = transform.Find("BottomButtons/RestartButton")?.GetComponent<Button>();

        if (slider != null && manager != null)
        {
            WireSliderVolume(slider, manager);
        }

        if (quitButton != null && controller != null)
        {
            WireButtonClick(quitButton, controller, nameof(GameController.QuitGame));
        }

        if (restartButton != null && controller != null)
        {
            WireButtonClick(restartButton, controller, nameof(GameController.RestartGame));
        }
#endif
    }

    private static void WireSliderVolume(Slider slider, SoundManager manager)
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            UnityEditor.Events.UnityEventTools.AddPersistentListener(slider.onValueChanged, manager.SetVolume);
            UnityEditor.EditorUtility.SetDirty(slider);
            return;
        }
#endif

        slider.onValueChanged.AddListener(manager.SetVolume);
    }

    private static void WireButtonClick(Button button, GameController controller, string methodName)
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            if (methodName == nameof(GameController.QuitGame))
            {
                UnityEditor.Events.UnityEventTools.AddPersistentListener(button.onClick, controller.QuitGame);
            }
            else if (methodName == nameof(GameController.RestartGame))
            {
                UnityEditor.Events.UnityEventTools.AddPersistentListener(button.onClick, controller.RestartGame);
            }

            UnityEditor.EditorUtility.SetDirty(button);
            return;
        }
#endif

        if (methodName == nameof(GameController.QuitGame))
        {
            button.onClick.AddListener(controller.QuitGame);
        }
        else if (methodName == nameof(GameController.RestartGame))
        {
            button.onClick.AddListener(controller.RestartGame);
        }
    }

    private static void StretchRect(RectTransform rectTransform)
    {
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
    }
}
