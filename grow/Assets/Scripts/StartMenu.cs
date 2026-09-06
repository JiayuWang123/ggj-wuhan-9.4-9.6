using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Start scene menu: load gameplay scene, or quit the application.
/// Wire these methods to UI Button OnClick events.
/// </summary>
public class StartMenu : MonoBehaviour
{
    [SerializeField] private string gameplaySceneName = "1";
    [SerializeField] [Range(0f, 1f)] private float alphaHitThreshold = 0.1f;

    [Tooltip("开始 / 退出按钮的点击音效，把 .mp3 拖到这里。")]
    [SerializeField] private AudioClip clickSfx;
    [SerializeField] [Range(0f, 1f)] private float clickVolume = 1f;
    [Tooltip("点击开始后，等待多少秒再进入第一关。")]
    [SerializeField] [Min(0f)] private float sceneChangeDelaySeconds = 1f;

    private AudioSource sfxSource;
    private bool busy;

    private void Awake()
    {
        sfxSource = GetComponent<AudioSource>();
        if (sfxSource == null)
            sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.playOnAwake = false;
        sfxSource.loop = false;
        sfxSource.spatialBlend = 0f;

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
        if (busy)
            return;
        StartCoroutine(PlayClickThen(sceneChangeDelaySeconds, () => SceneManager.LoadScene(gameplaySceneName)));
    }

    public void QuitGame()
    {
        if (busy)
            return;
        StartCoroutine(PlayClickThen(0f, QuitNow));
    }

    private IEnumerator PlayClickThen(float delaySeconds, System.Action next)
    {
        busy = true;
        foreach (Button button in GetComponentsInChildren<Button>(true))
            button.interactable = false;

        if (clickSfx != null)
            sfxSource.PlayOneShot(clickSfx, clickVolume);

        float wait = delaySeconds;
        if (wait <= 0f && clickSfx != null)
            wait = Mathf.Min(clickSfx.length, 0.4f);

        if (wait > 0f)
            yield return new WaitForSecondsRealtime(wait);

        next();
    }

    private static void QuitNow()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
