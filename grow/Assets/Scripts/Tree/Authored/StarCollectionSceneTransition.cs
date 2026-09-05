using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Loads the next gameplay scene after all stars in the current level are collected.
/// </summary>
public class StarCollectionSceneTransition : MonoBehaviour
{
    [SerializeField] private StarProtectionSystem starProtection;
    [SerializeField] private string nextSceneName = "";
    [SerializeField] private float transitionDelaySeconds = 0.5f;

    private bool transitionTriggered;

    private void Awake()
    {
        if (starProtection == null)
        {
            starProtection = GetComponent<StarProtectionSystem>();
        }
    }

    private void OnEnable()
    {
        if (starProtection != null)
        {
            starProtection.StarCollected += OnStarCollected;
        }
    }

    private void OnDisable()
    {
        if (starProtection != null)
        {
            starProtection.StarCollected -= OnStarCollected;
        }

        CancelInvoke(nameof(LoadNextScene));
    }

    private void OnStarCollected(int count)
    {
        if (transitionTriggered || starProtection == null)
        {
            return;
        }

        if (count < starProtection.MaxStarCount)
        {
            return;
        }

        if (string.IsNullOrEmpty(ResolveNextSceneName()))
        {
            return;
        }

        transitionTriggered = true;

        if (transitionDelaySeconds > 0f)
        {
            Invoke(nameof(LoadNextScene), transitionDelaySeconds);
            return;
        }

        LoadNextScene();
    }

    private string ResolveNextSceneName()
    {
        if (!string.IsNullOrEmpty(nextSceneName))
        {
            return nextSceneName;
        }

        switch (SceneManager.GetActiveScene().name)
        {
            case "1":
                return "2";
            case "2":
                return "3";
            case "3":
                return "Start";
            default:
                return "";
        }
    }

    private void LoadNextScene()
    {
        string targetScene = ResolveNextSceneName();
        if (string.IsNullOrEmpty(targetScene))
        {
            transitionTriggered = false;
            return;
        }

        SceneManager.LoadScene(targetScene);
    }
}
