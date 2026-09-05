using UnityEngine;

/// <summary>
/// Small on-screen panel showing the current owned branch nutrient pool.
/// </summary>
public class AuthoredTreeNutrientHud : MonoBehaviour
{
    [SerializeField] private BranchNutrientBudget nutrientBudget;
    [SerializeField] private bool showHud = true;
    [SerializeField] private Vector2 screenOffset = new Vector2(12f, 12f);
    [SerializeField] private float panelWidth = 220f;

    private int displayedAvailable;
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

    private void OnSlotsChanged(int unusedActiveCount, int availableNutrients)
    {
        displayedAvailable = availableNutrients;
    }

    private void RefreshDisplay()
    {
        if (nutrientBudget == null)
        {
            return;
        }

        displayedAvailable = nutrientBudget.AvailableNutrients;
    }

    private void OnGUI()
    {
        if (!showHud || nutrientBudget == null)
        {
            return;
        }

        RefreshDisplay();
        EnsureStyles();

        float panelHeight = 78f;
        Rect panelRect = new Rect(screenOffset.x, screenOffset.y, panelWidth, panelHeight);
        GUI.Box(panelRect, GUIContent.none, boxStyle);

        GUILayout.BeginArea(new Rect(panelRect.x + 10f, panelRect.y + 8f, panelRect.width - 20f, panelRect.height - 16f));
        GUILayout.Label("养分", titleStyle);
        GUILayout.Label($"拥有营养：{displayedAvailable}", labelStyle);

        if (displayedAvailable <= 0)
        {
            GUILayout.Label("营养耗尽，修剪后可继续生长", warningStyle);
        }

        GUILayout.EndArea();
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
