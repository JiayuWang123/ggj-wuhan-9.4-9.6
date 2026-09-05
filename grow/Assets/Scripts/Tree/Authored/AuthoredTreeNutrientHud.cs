using UnityEngine;

/// <summary>
/// Small on-screen panel showing available branch nutrients for the authored tree.
/// </summary>
public class AuthoredTreeNutrientHud : MonoBehaviour
{
    [SerializeField] private BranchNutrientBudget nutrientBudget;
    [SerializeField] private bool showHud = true;
    [SerializeField] private Vector2 screenOffset = new Vector2(12f, 12f);
    [SerializeField] private float panelWidth = 220f;

    private int displayedAvailable;
    private int displayedActive;
    private int displayedMax = 8;
    private GUIStyle boxStyle;
    private GUIStyle titleStyle;
    private GUIStyle labelStyle;
    private GUIStyle warningStyle;
    private bool stylesReady;

    private void Awake()
    {
        if (nutrientBudget == null)
        {
            nutrientBudget = GetComponent<BranchNutrientBudget>();
        }

        if (nutrientBudget == null)
        {
            nutrientBudget = FindObjectOfType<BranchNutrientBudget>();
        }
    }

    private void OnEnable()
    {
        if (nutrientBudget != null)
        {
            nutrientBudget.SlotsChanged += OnSlotsChanged;
        }
    }

    private void Start()
    {
        RefreshDisplay();
    }

    private void OnDisable()
    {
        if (nutrientBudget != null)
        {
            nutrientBudget.SlotsChanged -= OnSlotsChanged;
        }
    }

    private void OnSlotsChanged(int activeCount, int availableNutrients)
    {
        displayedActive = activeCount;
        displayedAvailable = availableNutrients;
        displayedMax = nutrientBudget != null ? nutrientBudget.MaxConcurrentBranches : displayedMax;
    }

    private void RefreshDisplay()
    {
        if (nutrientBudget == null)
        {
            return;
        }

        displayedActive = nutrientBudget.ActiveBranchCount;
        displayedAvailable = nutrientBudget.AvailableNutrients;
        displayedMax = nutrientBudget.MaxConcurrentBranches;
    }

    private void OnGUI()
    {
        if (!showHud || nutrientBudget == null)
        {
            return;
        }

        RefreshDisplay();
        EnsureStyles();

        float panelHeight = 118f;
        Rect panelRect = new Rect(screenOffset.x, screenOffset.y, panelWidth, panelHeight);
        GUI.Box(panelRect, GUIContent.none, boxStyle);

        GUILayout.BeginArea(new Rect(panelRect.x + 10f, panelRect.y + 8f, panelRect.width - 20f, panelRect.height - 16f));
        GUILayout.Label("养分", titleStyle);
        GUILayout.Label($"可用营养：{displayedAvailable} / {displayedMax}", labelStyle);
        GUILayout.Label($"当前树枝：{displayedActive}", labelStyle);
        GUILayout.Space(6f);
        DrawNutrientBar();
        GUILayout.Space(4f);

        if (displayedAvailable <= 0)
        {
            GUILayout.Label("营养耗尽，修剪后可继续生长", warningStyle);
        }

        GUILayout.EndArea();
    }

    private void DrawNutrientBar()
    {
        Rect barRect = GUILayoutUtility.GetRect(panelWidth - 20f, 18f);
        int usedCount = Mathf.Clamp(displayedMax - displayedAvailable, 0, displayedMax);
        float slotWidth = barRect.width / displayedMax;
        float gap = 2f;

        for (int i = 0; i < displayedMax; i++)
        {
            bool isAvailable = i >= usedCount;
            Rect slotRect = new Rect(
                barRect.x + i * slotWidth + gap * 0.5f,
                barRect.y,
                slotWidth - gap,
                barRect.height);

            Color slotColor = isAvailable ? new Color(0.35f, 0.78f, 0.42f, 0.95f) : new Color(0.35f, 0.35f, 0.35f, 0.85f);
            DrawSolidRect(slotRect, slotColor);
        }
    }

    private void DrawSolidRect(Rect rect, Color color)
    {
        Color previous = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = previous;
    }

    private void EnsureStyles()
    {
        if (stylesReady)
        {
            return;
        }

        boxStyle = new GUIStyle(GUI.skin.box)
        {
            alignment = TextAnchor.UpperLeft
        };

        titleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 15,
            fontStyle = FontStyle.Bold
        };

        labelStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 13
        };

        warningStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 11,
            normal = { textColor = new Color(1f, 0.72f, 0.35f) }
        };

        stylesReady = true;
    }
}
