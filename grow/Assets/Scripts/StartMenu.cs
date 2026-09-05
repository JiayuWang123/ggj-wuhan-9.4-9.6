using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Start scene menu: load gameplay scene, or quit the application.
/// Wire these methods to UI Button OnClick events.
/// </summary>
public class StartMenu : MonoBehaviour
{
    [SerializeField] private string gameplaySceneName = "2";
    [SerializeField] [Range(0f, 1f)] private float alphaHitThreshold = 0.1f;

    private void Awake()
    {
        foreach (Image image in GetComponentsInChildren<Image>(true))
        {
            if (image.GetComponent<Button>() != null)
            {
                image.alphaHitTestMinimumThreshold = alphaHitThreshold;
            }
            else
            {
                image.raycastTarget = false;
            }
        }
    }

    public void StartGame()
    {
        SceneManager.LoadScene(gameplaySceneName);
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
