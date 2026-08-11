using UnityEngine;
using UnityEngine.UI;

public class SoundManager : MonoBehaviour
{
    [Header("Audio")]
    [SerializeField] private AudioClip buttonClickSound;
    [SerializeField] private AudioSource audioSource;

    [Header("Настройки звука")]
    [SerializeField] private float pitchMin = 0.9f;
    [SerializeField] private float pitchMax = 1.1f;

    private float currentVolume = 1f;

    private void Start()
    {
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }

        // Загружаем сохранённую громкость
        currentVolume = PlayerPrefs.GetFloat("SoundVolume", 1f);
        audioSource.volume = currentVolume;

        // Подписываем все кнопки
        Button[] buttons = FindObjectsByType<Button>();
        foreach (var button in buttons)
        {
            button.onClick.AddListener(PlayButtonSound);
        }

        // Подписываемся на событие изменения громкости (из MusicManager)
        EventBus.soundVolumeChanged += SetVolume;
    }

    private void OnDestroy()
    {
        EventBus.soundVolumeChanged -= SetVolume;
    }

    public void SetVolume(float volume)
    {
        currentVolume = Mathf.Clamp01(volume);
        audioSource.volume = currentVolume;
    }

    public void PlayButtonSound()
    {
        if (buttonClickSound == null || audioSource == null)
            return;

        audioSource.pitch = Random.Range(pitchMin, pitchMax);
        audioSource.volume = currentVolume;
        audioSource.PlayOneShot(buttonClickSound);
    }

    public void PlaySound(AudioClip clip, float volume = 1f)
    {
        if (clip == null || audioSource == null)
            return;

        audioSource.pitch = Random.Range(pitchMin, pitchMax);
        audioSource.volume = currentVolume * Mathf.Clamp01(volume);
        audioSource.PlayOneShot(clip);
    }
}