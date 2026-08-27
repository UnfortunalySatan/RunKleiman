using UnityEngine;

public class AudioUnlocker : MonoBehaviour
{
    // Перетащите сюда объект с вашим MusicManager из инспектора
    [Header("Ссылка на ваш менеджер музыки")]
    [SerializeField] private MonoBehaviour musicManager;

    // Напишите точное имя метода, который запускает музыку в вашем скрипте
    [Header("Имя метода запуска (например, PlayNextTrack или PlayMusic)")]
    [SerializeField] private string methodName = "PlayNextTrack";

    private bool isAudioUnlocked = false;

    void Update()
    {
        // Если музыка уже запущена — ничего не делаем
        if (isAudioUnlocked) return;

        // Ждем абсолютно любого первого клика мыши или нажатия клавиши от игрока
        if (Input.anyKeyDown || Input.GetMouseButtonDown(0))
        {
            UnlockAudioAndPlay();
        }
    }

    private void UnlockAudioAndPlay()
    {
        isAudioUnlocked = true;

        // 1. Принудительно будим звуковую систему Unity внутри WebGL-браузера
        AudioListener.pause = false;

        // 2. Безопасно запускаем музыку в вашем скрипте, не ломая его структуру
        if (musicManager != null)
        {
            musicManager.Invoke(methodName, 0f);
            Debug.Log("[AudioUnlocker] Первый клик получен! Аудио-контекст активирован, музыка запущена.");
        }
        else
        {
            Debug.LogError("[AudioUnlocker] Забыли прикрепить MusicManager в инспекторе!");
        }

        // Удаляем этот вспомогательный скрипт, так как его задача выполнена
        Destroy(this);
    }
}
