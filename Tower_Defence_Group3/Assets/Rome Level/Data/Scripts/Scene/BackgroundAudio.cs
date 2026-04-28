using UnityEngine;

public class BackgroundAudio : MonoBehaviour
{
    public AudioClip backgroundMusic;
    [Range(0f, 1f)]
    public float volume = 0.5f;

    private AudioSource audioSource;

    void Start()
    {
        // Create and configure the AudioSource
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.clip = backgroundMusic;
        audioSource.loop = true;
        audioSource.playOnAwake = true;
        audioSource.volume = volume;

        // Play the background music
        audioSource.Play();
    }
}
