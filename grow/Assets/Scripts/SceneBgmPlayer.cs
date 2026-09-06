using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class SceneBgmPlayer : MonoBehaviour
{
    [Tooltip("把该场景的 BGM（.mp3）拖到这里。切场景会销毁本物体，下一关从开头重播。")]
    [SerializeField] private AudioClip bgmClip;

    [SerializeField, Range(0f, 1f)] private float volume = 0.6f;

    private void Awake()
    {
        AudioSource source = GetComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = true;
        source.spatialBlend = 0f;
        source.volume = volume;
        if (bgmClip != null)
            source.clip = bgmClip;

        if (source.clip != null)
            source.Play();
    }
}
