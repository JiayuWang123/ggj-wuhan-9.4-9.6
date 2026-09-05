using UnityEngine;

/// <summary>
/// On-screen panel showing how many stars the player has collected.
/// </summary>
public class AuthoredTreeStarHud : MonoBehaviour
{
    [SerializeField] private StarProtectionSystem starProtection;
    [SerializeField] private bool showHud = true;
    [SerializeField] private Vector2 screenOffset = new Vector2(12f, 142f);
    [SerializeField] private float panelWidth = 220f;

    private int displayedCount;
    private int displayedMax = 3;
    private GUIStyle boxStyle;
    private GUIStyle titleStyle;
    private GUIStyle labelStyle;
    private bool stylesReady;

    private void Awake()
    {
        if (starProtection == null)
        {
            starProtection = GetComponent<StarProtectionSystem>();
        }

        if (starProtection == null)
        {
            starProtection = FindObjectOfType<StarProtectionSystem>();
        }
    }

    private void OnEnable()
    {
        if (starProtection != null)
        {
            starProtection.StarCollected += OnStarCollected;
        }
    }

    private void Start()
    {
        RefreshDisplay();
    }

    private void OnDisable()
    {
        if (starProtection != null)
        {
            starProtection.StarCollected -= OnStarCollected;
        }
    }

    private void OnStarCollected(int count)
    {
        displayedCount = count;
        displayedMax = starProtection != null ? starProtection.MaxStarCount : displayedMax;
    }

    private void RefreshDisplay()
    {
        if (starProtection == null)
        {
            return;
        }

        displayedCount = starProtection.CollectedStarCount;
        displayedMax = starProtection.MaxStarCount;
    }

    private void OnGUI()
    {
        if (!showHud || starProtection == null)
        {
            return;
        }

        RefreshDisplay();
        EnsureStyles();

        float panelHeight = 88f;
        Rect panelRect = new Rect(screenOffset.x, screenOffset.y, panelWidth, panelHeight);
        GUI.Box(panelRect, GUIContent.none, boxStyle);

        GUILayout.BeginArea(new Rect(panelRect.x + 10f, panelRect.y + 8f, panelRect.width - 20f, panelRect.height - 16f));
        GUILayout.Label("星星", titleStyle);
        GUILayout.Label($"收集数量：{displayedCount} / {displayedMax}", labelStyle);
        GUILayout.Space(6f);
        DrawStarBar();
        GUILayout.EndArea();
    }

    private void DrawStarBar()
    {
        Rect barRect = GUILayoutUtility.GetRect(panelWidth - 20f, 22f);
        float slotWidth = barRect.width / displayedMax;
        float gap = 6f;

        for (int i = 0; i < displayedMax; i++)
        {
            bool isCollected = i < displayedCount;
            Rect slotRect = new Rect(
                barRect.x + i * slotWidth + gap * 0.5f,
                barRect.y,
                slotWidth - gap,
                barRect.height);

            Color slotColor = isCollected
                ? new Color(1f, 0.9f, 0.25f, 0.95f)
                : new Color(0.35f, 0.35f, 0.35f, 0.85f);
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

        stylesReady = true;
    }
}
