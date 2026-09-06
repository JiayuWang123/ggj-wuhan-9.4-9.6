using UnityEngine;

/// <summary>
/// Hang this on a star and drag in the collect mp3.
/// Plays through a short-lived 2D source so the sound is not cut when the star disables.
/// </summary>
[DisallowMultipleComponent]
public class StarCollectSfx : MonoBehaviour
{
    [Tooltip("这颗星星被收集时播放的音效，把 .mp3 拖到这里。")]
    [SerializeField] private AudioClip collectSfx;
    [SerializeField] [Range(0f, 1f)] private float volume = 1f;

    public void Play()
    {
        if (collectSfx == null)
            return;

        GameObject oneShot = new GameObject("StarCollectSfxOneShot");
        DontDestroyOnLoad(oneShot);

        AudioSource source = oneShot.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = false;
        source.spatialBlend = 0f;
        source.clip = collectSfx;
        source.volume = volume;
        source.Play();

        Destroy(oneShot, collectSfx.length + 0.05f);
    }
}
