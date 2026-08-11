using UnityEngine;
using UnityEngine.UI;
using YG;

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

        if (YG2.isSDKEnabled)
        {
            ApplyLoadedVolume();
        }
        else
        {
            YG2.onGetSDKData += ApplyLoadedVolume;
        }

        RefreshButtonListeners();
        EventBus.soundVolumeChanged += SetVolume;
    }

    private void OnDestroy()
    {
        YG2.onGetSDKData -= ApplyLoadedVolume;
        EventBus.soundVolumeChanged -= SetVolume;
    }

    void ApplyLoadedVolume()
    {
        // Читаем громкость эффектов напрямую из Облака Яндекса
        currentVolume = YG2.saves.soundVolume;
        if (audioSource != null)
        {
            audioSource.volume = currentVolume;
        }
    }

    public void RefreshButtonListeners()
    {
        // Безопасный поиск кнопок (включая неактивные панели на сцене)
        Button[] buttons = FindObjectsByType<Button>(FindObjectsInactive.Include);
        foreach (var button in buttons)
        {
            button.onClick.RemoveListener(PlayButtonSound);
            button.onClick.AddListener(PlayButtonSound);
        }
    }

    public void SetVolume(float volume)
    {
        currentVolume = Mathf.Clamp01(volume);
        audioSource.volume = currentVolume;
    }

    public void PlayButtonSound()
    {
        if (buttonClickSound == null || audioSource == null) return;

        audioSource.pitch = Random.Range(pitchMin, pitchMax);
        audioSource.volume = currentVolume;
        audioSource.PlayOneShot(buttonClickSound);
    }
}
