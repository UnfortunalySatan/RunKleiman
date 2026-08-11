using UnityEngine;

public class FootStepsSound : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private PlayerMovement pm;
    [Header("Звуки шагов")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip[] footstepClips;
    [SerializeField] private AudioClip hitSound;

    [Header("Настройки звука")]
    [SerializeField] private float pitchMin = 0.85f;
    [SerializeField] private float pitchMax = 1.15f;
    [SerializeField] private float volumeMin = 0.7f;
    [SerializeField] private float volumeMax = 1.0f;

    private int lastIndex = -1;

    void Start()
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        if (audioSource == null)
        {
            Debug.LogWarning("[FootStepsSound] AudioSource не найден!");
        }
    }

    // Метод вызывается из анимации
    public void PlayFootstep()
    {
        if (audioSource == null || footstepClips == null || footstepClips.Length == 0) return;

        // Выбираем случайный звук, исключая повторение предыдущего
        int randomIndex = lastIndex;
        if (footstepClips.Length > 1)
        {
            do
            {
                randomIndex = Random.Range(0, footstepClips.Length);
            } while (randomIndex == lastIndex);
        }
        else
        {
            randomIndex = 0;
        }

        lastIndex = randomIndex;

        if (footstepClips[randomIndex] == null) return;

        // Рандомизация pitch и volume
        audioSource.pitch = Random.Range(pitchMin, pitchMax);
        audioSource.volume = Random.Range(volumeMin, volumeMax);

        // Воспроизведение
        audioSource.PlayOneShot(footstepClips[randomIndex]);
    }

    public void CallEnd()
    {
        EventBus.isWallHit?.Invoke();
        if (pm != null) pm.StartPosition();
    }

    public void OffMove()
    {
        if (pm != null) pm.playerOffMove();
        if (audioSource != null && hitSound != null)
        {
            audioSource.pitch = 1f;
            audioSource.volume = 1f;
            audioSource.PlayOneShot(hitSound);
        }
    }
}
