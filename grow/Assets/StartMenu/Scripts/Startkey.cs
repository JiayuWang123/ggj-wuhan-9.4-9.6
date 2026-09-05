using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public class PressAnyKeyToStart : MonoBehaviour
{
    [Header("Optional UI to hide after start")]
    public GameObject Startkey;

    [Header("Scene to load (leave empty to only hide UI)")]
    public string nextSceneName = "";

    [Header("Skip start when clicking UI buttons")]
    public bool ignoreWhenPointerOverUi = true;

    private bool isGameStarted;

    private void Update()
    {
        if (isGameStarted)
        {
            return;
        }

        if (!Input.anyKeyDown)
        {
            return;
        }

        if (ignoreWhenPointerOverUi &&
            EventSystem.current != null &&
            EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

        StartGame();
    }

    private void StartGame()
    {
        isGameStarted = true;

        if (!string.IsNullOrEmpty(nextSceneName))
        {
            SceneManager.LoadScene(nextSceneName);
            return;
        }

        if (Startkey != null && Startkey != gameObject)
        {
            Startkey.SetActive(false);
        }
    }
}
