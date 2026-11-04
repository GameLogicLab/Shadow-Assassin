using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("Background Music")]
    public AudioSource musicSource;
    public AudioClip backgroundMusic;

    private const string MusicPrefKey = "MusicEnabled";

    void Awake()
    {
        // Keep this alive across scenes (Main Menu -> Levels)
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        SetupMusic();
    }

    void SetupMusic()
    {
        if (musicSource == null)
            musicSource = gameObject.AddComponent<AudioSource>();

        musicSource.clip = backgroundMusic;
        musicSource.loop = true;
        musicSource.playOnAwake = false;

        bool isEnabled = PlayerPrefs.GetInt(MusicPrefKey, 1) == 1;
        musicSource.mute = !isEnabled;

        if (isEnabled && backgroundMusic != null)
            musicSource.Play();
    }

    public void SetMusicEnabled(bool enabled)
    {
        if (musicSource == null) return;

        musicSource.mute = !enabled;
        PlayerPrefs.SetInt(MusicPrefKey, enabled ? 1 : 0);
        PlayerPrefs.Save();

        if (enabled && backgroundMusic != null && !musicSource.isPlaying)
            musicSource.Play();
        else if (!enabled)
            musicSource.Stop();
    }

    public bool IsMusicEnabled()
    {
        return PlayerPrefs.GetInt(MusicPrefKey, 1) == 1;
    }
}