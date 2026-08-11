using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using YG;

public class MusicManager : MonoBehaviour
{
    [Header("Музыка")]
    [SerializeField] private AudioClip[] musicTracks;
    [SerializeField] private AudioSource musicSource;

    [Header("Настройки")]
    [SerializeField] private bool playOnStart = true;
    [SerializeField] private float fadeDuration = 1.5f;
    [SerializeField] private float volumeMultiplier = 0.8f;
    [SerializeField] private float timeBetweenTracks = 1f;

    [Header("UI Компоненты")]
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Slider soundSlider;

    private List<AudioClip> playlist = new List<AudioClip>();
    private int currentTrackIndex = 0;
    private float baseVolume = 1f;
    private bool isFading = false;
    private bool isPlaying = false;

    private Coroutine fadeCoroutine;

    void Start()
    {
        if (musicSource == null)
        {
            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.loop = false;
            musicSource.playOnAwake = false;
            musicSource.spatialBlend = 0f;
        }

        CreatePlaylist();

        if (YG2.isSDKEnabled)
        {
            InitializeVolumeSettings();
        }
        else
        {
            YG2.onGetSDKData += InitializeVolumeSettings;
        }
    }

    private void OnDestroy()
    {
        YG2.onGetSDKData -= InitializeVolumeSettings;
    }

    void InitializeVolumeSettings()
    {
        baseVolume = YG2.saves.musicVolume;
        float soundVolume = YG2.saves.soundVolume;

        if (musicSlider != null)
        {
            musicSlider.value = baseVolume;
            musicSlider.onValueChanged.AddListener(SetMusicVolume);
        }

        if (soundSlider != null)
        {
            soundSlider.value = soundVolume;
            soundSlider.onValueChanged.AddListener(SetSoundVolume);
        }

        musicSource.volume = baseVolume * volumeMultiplier;

        if (playOnStart && musicTracks.Length > 0 && !isPlaying)
        {
            PlayNextTrack();
        }
    }

    void Update()
    {
        // ИСПРАВЛЕНИЕ 1: Если игра на паузе (timeScale == 0), принудительно выходим из Update.
        // Это не даст системе ложно перелистнуть трек при выходе из меню/рекламы.
        if (Time.timeScale == 0f) return;

        if (isPlaying && !isFading && !musicSource.isPlaying)
        {
            PlayNextTrack();
        }
    }

    void CreatePlaylist()
    {
        playlist.Clear();
        foreach (var track in musicTracks)
        {
            if (track != null) playlist.Add(track);
        }

        for (int i = playlist.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            AudioClip temp = playlist[i];
            playlist[i] = playlist[j];
            playlist[j] = temp;
        }

        currentTrackIndex = 0;
    }

    public void PlayNextTrack()
    {
        if (playlist.Count == 0) return;

        if (currentTrackIndex >= playlist.Count)
        {
            CreatePlaylist();
        }

        AudioClip nextTrack = playlist[currentTrackIndex];
        currentTrackIndex++;

        if (nextTrack == null)
        {
            PlayNextTrack();
            return;
        }

        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }

        fadeCoroutine = StartCoroutine(PlayTrackWithFade(nextTrack));
    }

    IEnumerator PlayTrackWithFade(AudioClip track)
    {
        isFading = true;

        if (musicSource.isPlaying)
        {
            float startVolume = musicSource.volume;
            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                musicSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / fadeDuration);
                yield return null;
            }
            musicSource.Stop();
        }

        // ИСПРАВЛЕНИЕ 2: Заменяем WaitForSeconds на WaitForSecondsRealtime.
        // Теперь задержка между песнями считается по реальным часам компьютера, игнорируя любые паузы Яндекса.
        yield return new WaitForSecondsRealtime(timeBetweenTracks);

        musicSource.clip = track;
        musicSource.volume = 0f;
        musicSource.Play();

        float elapsedFade = 0f;
        float targetVolume = baseVolume * volumeMultiplier;

        while (elapsedFade < fadeDuration)
        {
            elapsedFade += Time.unscaledDeltaTime;
            musicSource.volume = Mathf.Lerp(0f, targetVolume, elapsedFade / fadeDuration);
            yield return null;
        }

        musicSource.volume = targetVolume;
        isFading = false;
        isPlaying = true;
        fadeCoroutine = null;
    }

    public void SetMusicVolume(float volume)
    {
        baseVolume = Mathf.Clamp01(volume);
        if (!isFading)
        {
            musicSource.volume = baseVolume * volumeMultiplier;
        }
        YG2.saves.musicVolume = baseVolume;
    }

    public void SetSoundVolume(float volume)
    {
        volume = Mathf.Clamp01(volume);
        YG2.saves.soundVolume = volume;
        EventBus.soundVolumeChanged?.Invoke(volume);
    }

    public void SaveMusicVolumeFromSlider()
    {
        if (musicSlider != null)
        {
            SetMusicVolume(musicSlider.value);
            YG2.SaveProgress();
            Debug.Log($"[MusicManager] Громкость музыки ({musicSlider.value}) успешно отправлена в облако YG.");
        }
    }

    public void SaveSoundVolumeFromSlider()
    {
        if (soundSlider != null)
        {
            SetSoundVolume(soundSlider.value);
            YG2.SaveProgress();
            Debug.Log($"[MusicManager] Громкость звуков ({soundSlider.value}) успешно отправлена в облако YG.");
        }
    }

    public void TogglePause()
    {
        if (musicSource.isPlaying)
            musicSource.Pause();
        else
            musicSource.UnPause();
    }
}
