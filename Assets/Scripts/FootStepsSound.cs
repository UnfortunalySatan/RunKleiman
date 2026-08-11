using UnityEngine;

public class FootStepsSound : MonoBehaviour
{
    [SerializeField] Animator animator;
    [SerializeField] PlayerMovement pm;
    [Header("Звуки шагов")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip[] footstepClips; // массив звуков шагов
    [SerializeField] private float maxClipLength = 0.15f; // максимальная длина одного звука (сек)
    [SerializeField] private AudioClip hitSound;
    [Header("Настройки звука")]
    [SerializeField] private float pitchMin = 0.85f;
    [SerializeField] private float pitchMax = 1.15f;
    [SerializeField] private float volumeMin = 0.7f;
    [SerializeField] private float volumeMax = 1.0f;

    private int lastIndex = -1;
    private AudioClip[] trimmedClips; // массив обрезанных звуков

    void Start()
    {
        // Если AudioSource не назначен, ищем на этом объекте
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        if (audioSource == null)
        {
            Debug.LogWarning("AudioSource не найден! Добавьте компонент AudioSource.");
            return;
        }

        // Обрезаем звуки до maxClipLength
        if (footstepClips != null && footstepClips.Length > 0)
        {
            TrimAudioClips();
        }
    }

    // Обрезка всех звуков до одинаковой длины
    void TrimAudioClips()
    {
        trimmedClips = new AudioClip[footstepClips.Length];

        for (int i = 0; i < footstepClips.Length; i++)
        {
            AudioClip original = footstepClips[i];
            if (original == null) continue;

            // Если звук короче maxClipLength, оставляем как есть
            if (original.length <= maxClipLength)
            {
                trimmedClips[i] = original;
                continue;
            }

            // Создаём обрезанную копию звука
            trimmedClips[i] = TrimAudioClip(original, maxClipLength);
        }
    }

    // Обрезка одного AudioClip
    AudioClip TrimAudioClip(AudioClip clip, float length)
    {
        // Получаем данные звука
        int sampleCount = Mathf.FloorToInt(length * clip.frequency);
        float[] samples = new float[sampleCount * clip.channels];

        // Создаём новый AudioClip
        AudioClip newClip = AudioClip.Create(
            clip.name + "_trimmed",
            sampleCount,
            clip.channels,
            clip.frequency,
            false
        );

        // Копируем данные из оригинального клипа
        float[] originalSamples = new float[clip.samples * clip.channels];
        clip.GetData(originalSamples, 0);

        // Копируем нужное количество семплов
        for (int i = 0; i < samples.Length && i < originalSamples.Length; i++)
        {
            samples[i] = originalSamples[i];
        }

        newClip.SetData(samples, 0);
        return newClip;
    }

    // Этот метод вызывается из анимации
    public void PlayFootstep()
    {
        if (audioSource == null) return;

        // Используем обрезанные звуки, если они есть
        AudioClip[] clips = trimmedClips != null && trimmedClips.Length > 0 ? trimmedClips : footstepClips;

        if (clips == null || clips.Length == 0) return;

        // Выбираем случайный звук, но не тот же, что играл до этого
        int randomIndex;
        do
        {
            randomIndex = Random.Range(0, clips.Length);
        } while (randomIndex == lastIndex && clips.Length > 1);

        lastIndex = randomIndex;

        // Рандомизация pitch и volume
        audioSource.pitch = Random.Range(pitchMin, pitchMax);
        audioSource.volume = Random.Range(volumeMin, volumeMax);

        // Воспроизведение
        audioSource.PlayOneShot(clips[randomIndex]);
    }

    public void CallEnd()
    {
        EventBus.isWallHit?.Invoke();
        pm.StartPosition();
    }

    public void OffMove()
    {
        pm.playerOffMove();
        audioSource.PlayOneShot(hitSound);
    }
}
