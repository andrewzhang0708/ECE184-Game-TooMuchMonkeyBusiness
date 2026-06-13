using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameAudioSettings : MonoBehaviour
{
    private const string OverallVolumeKey = "Audio.OverallVolume";
    private const string MusicVolumeKey = "Audio.MusicVolume";
    private const string SfxVolumeKey = "Audio.SfxVolume";
    private const string MusicObjectName = "BGM";

    private static GameAudioSettings instance;
    private readonly HashSet<AudioSource> registeredSources = new HashSet<AudioSource>();
    private float nextSourceScanTime;

    public static float OverallVolume { get; private set; } = 1f;
    public static float MusicVolume { get; private set; } = 1f;
    public static float SfxVolume { get; private set; } = 1f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        if (instance != null)
        {
            return;
        }

        GameObject settingsObject = new GameObject("Game Audio Settings");
        instance = settingsObject.AddComponent<GameAudioSettings>();
        DontDestroyOnLoad(settingsObject);
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        OverallVolume = PlayerPrefs.GetFloat(OverallVolumeKey, 1f);
        MusicVolume = PlayerPrefs.GetFloat(MusicVolumeKey, 1f);
        SfxVolume = PlayerPrefs.GetFloat(SfxVolumeKey, 1f);
        AudioListener.volume = OverallVolume;

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void Start()
    {
        RegisterSceneAudioSources();
    }

    private void Update()
    {
        if (Time.unscaledTime < nextSourceScanTime)
        {
            return;
        }

        nextSourceScanTime = Time.unscaledTime + 0.25f;
        RegisterSceneAudioSources();
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            instance = null;
        }
    }

    public static void SetOverallVolume(float volume)
    {
        OverallVolume = Mathf.Clamp01(volume);
        AudioListener.volume = OverallVolume;
        PlayerPrefs.SetFloat(OverallVolumeKey, OverallVolume);
        PlayerPrefs.Save();
    }

    public static void SetMusicVolume(float volume)
    {
        MusicVolume = Mathf.Clamp01(volume);
        PlayerPrefs.SetFloat(MusicVolumeKey, MusicVolume);
        PlayerPrefs.Save();
    }

    public static void SetSfxVolume(float volume)
    {
        SfxVolume = Mathf.Clamp01(volume);
        PlayerPrefs.SetFloat(SfxVolumeKey, SfxVolume);
        PlayerPrefs.Save();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        registeredSources.Clear();
        RegisterSceneAudioSources();
    }

    private void RegisterSceneAudioSources()
    {
        AudioSource[] sources = FindObjectsByType<AudioSource>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );

        foreach (AudioSource source in sources)
        {
            if (source == null || !registeredSources.Add(source))
            {
                continue;
            }

            AudioCategoryVolume category = source.GetComponent<AudioCategoryVolume>();
            if (category == null)
            {
                category = source.gameObject.AddComponent<AudioCategoryVolume>();
            }

            category.IsMusic = IsBgmSource(source.transform);
        }
    }

    private static bool IsBgmSource(Transform sourceTransform)
    {
        Transform current = sourceTransform;

        while (current != null)
        {
            if (current.name == MusicObjectName)
            {
                return true;
            }

            current = current.parent;
        }

        return false;
    }
}
