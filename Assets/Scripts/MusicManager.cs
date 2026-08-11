using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MusicManager : MonoBehaviour
{
    [Header("Музыка")]
    [SerializeField] private AudioClip[] musicTracks;        // массив треков
    [SerializeField] private AudioSource musicSource;        // источник для музыки

    [Header("Настройки")]
    [SerializeField] private bool playOnStart = true;
    [SerializeField] private float fadeDuration = 2f;        // длительность затухания (сек)
    [SerializeField] private float volumeMultiplier = 0.8f;  // базовый множитель громкости (0-1)
    [SerializeField] private float timeBetweenTracks = 1f;   // пауза между треками

    [Header("UI")]
    [SerializeField] private Slider musicSlider;             // слайдер громкости музыки
    [SerializeField] private Slider soundSlider;             // слайдер громкости звуков (опционально)

    private List<AudioClip> playlist = new List<AudioClip>(); // очередь треков
    private int currentTrackIndex = 0;
    private float baseVolume = 1f;
    private bool isFading = false;
    private bool isPlaying = false;

    void Start()
    {
        // Проверяем AudioSource
        if (musicSource == null)
        {
            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.loop = false;
            musicSource.playOnAwake = false;
            musicSource.spatialBlend = 0f; // 2D звук
        }

        // Загружаем сохранённую громкость (если есть)
        baseVolume = PlayerPrefs.GetFloat("MusicVolume", 1f);

        // Создаём плейлист
        CreatePlaylist();

        // Настройка слайдера
        if (musicSlider != null)
        {
            musicSlider.value = baseVolume;
            musicSlider.onValueChanged.AddListener(SetMusicVolume);
        }

        if (soundSlider != null)
        {
            // Для звуков можно отдельно сохранять
            float soundVolume = PlayerPrefs.GetFloat("SoundVolume", 1f);
            soundSlider.value = soundVolume;
            soundSlider.onValueChanged.AddListener(SetSoundVolume);
        }

        // Запускаем воспроизведение
        if (playOnStart && musicTracks.Length > 0)
        {
            PlayNextTrack();
        }
    }

    // ===== СОЗДАНИЕ ПЛЕЙЛИСТА =====
    void CreatePlaylist()
    {
        playlist.Clear();
        // Добавляем все треки в список
        foreach (var track in musicTracks)
        {
            if (track != null)
                playlist.Add(track);
        }

        // Перемешиваем плейлист (алгоритм Фишера-Йетса)
        for (int i = playlist.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            AudioClip temp = playlist[i];
            playlist[i] = playlist[j];
            playlist[j] = temp;
        }

        currentTrackIndex = 0;
        Debug.Log($"Плейлист создан! Всего треков: {playlist.Count}");
    }

    // ===== ВОСПРОИЗВЕДЕНИЕ СЛЕДУЮЩЕГО ТРЕКА =====
    public void PlayNextTrack()
    {
        if (playlist.Count == 0)
        {
            Debug.LogWarning("Нет треков для воспроизведения!");
            return;
        }

        // Если дошли до конца плейлиста – перемешиваем заново
        if (currentTrackIndex >= playlist.Count)
        {
            CreatePlaylist();
        }

        AudioClip nextTrack = playlist[currentTrackIndex];
        currentTrackIndex++;

        if (nextTrack == null)
        {
            Debug.LogWarning("Трек null, пропускаем...");
            PlayNextTrack();
            return;
        }

        StartCoroutine(PlayTrackWithFade(nextTrack));
    }

    // ===== КОРУТИНА ВОСПРОИЗВЕДЕНИЯ С ЗАТУХАНИЕМ =====
    IEnumerator PlayTrackWithFade(AudioClip track)
    {
        isPlaying = true;
        isFading = true;

        // Плавно уменьшаем громкость текущего трека (если он играет)
        if (musicSource.isPlaying)
        {
            float startVolume = musicSource.volume;
            float elapsed = 0f;

            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                musicSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / fadeDuration);
                yield return null;
            }
            musicSource.Stop();
            musicSource.volume = 0f;
        }

        // Небольшая пауза между треками
        yield return new WaitForSeconds(timeBetweenTracks);

        // Начинаем новый трек
        musicSource.clip = track;
        musicSource.volume = 0f;
        musicSource.Play();

        // Плавное увеличение громкости
        float elapsedFade = 0f;
        float targetVolume = baseVolume * volumeMultiplier;

        while (elapsedFade < fadeDuration)
        {
            elapsedFade += Time.deltaTime;
            musicSource.volume = Mathf.Lerp(0f, targetVolume, elapsedFade / fadeDuration);
            yield return null;
        }

        musicSource.volume = targetVolume;
        isFading = false;

        // Ждём окончания трека
        yield return new WaitForSeconds(track.length - fadeDuration);

        // Если трек всё ещё играет, запускаем следующий
        if (musicSource.isPlaying && !isFading)
        {
            PlayNextTrack();
        }
    }

    // ===== УПРАВЛЕНИЕ ГРОМКОСТЬЮ =====

    public void SetMusicVolume(float volume)
    {
        baseVolume = Mathf.Clamp01(volume);
        musicSource.volume = baseVolume * volumeMultiplier;

        // Сохраняем настройку
        PlayerPrefs.SetFloat("MusicVolume", baseVolume);
        PlayerPrefs.Save();
    }

    public void SetSoundVolume(float volume)
    {
        volume = Mathf.Clamp01(volume);
        PlayerPrefs.SetFloat("SoundVolume", volume);
        PlayerPrefs.Save();

        // Отправляем событие для обновления громкости звуков
        EventBus.soundVolumeChanged?.Invoke(volume);
    }

    // ===== ПУБЛИЧНЫЕ МЕТОДЫ =====

    // Переключение на следующий трек (можно вызвать извне)
    public void SkipTrack()
    {
        StopAllCoroutines();
        StartCoroutine(PlayTrackWithFade(playlist[currentTrackIndex]));
    }

    // Пауза/воспроизведение
    public void TogglePause()
    {
        if (musicSource.isPlaying)
            musicSource.Pause();
        else
            musicSource.UnPause();
    }

    // Остановка музыки
    public void StopMusic()
    {
        StopAllCoroutines();
        musicSource.Stop();
        isPlaying = false;
    }

    // Запуск музыки (если остановлена)
    public void StartMusic()
    {
        if (!isPlaying && playlist.Count > 0)
        {
            PlayNextTrack();
        }
    }

    // Получить текущий трек (для отображения в UI)
    public AudioClip GetCurrentTrack()
    {
        return musicSource.clip;
    }

    // Обновить громкость из слайдера (вызывается при загрузке)
    public void RefreshVolume()
    {
        if (musicSlider != null)
        {
            SetMusicVolume(musicSlider.value);
        }
    }
}