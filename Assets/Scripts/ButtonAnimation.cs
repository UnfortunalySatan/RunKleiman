using UnityEngine;

public class ButtonAnimation : MonoBehaviour
{
    [Header("Настройки анимации")]
    [SerializeField] private float rotationSpeed = 2f;
    [SerializeField] private float rotationAngle = 15f;
    [SerializeField] private float scaleSpeed = 1.5f;
    [SerializeField] private float scaleAmount = 0.1f;   // теперь 0.1 = увеличение на 10% от исходного

    private RectTransform rectTransform;

    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    void Update()
    {
        // Вращение (как было)
        float rotationZ = Mathf.Sin(Time.time * rotationSpeed) * rotationAngle;
        rectTransform.localRotation = Quaternion.Euler(0, 0, rotationZ);

        // Масштаб: колеблется от 1 до 1 + scaleAmount
        // (Mathf.Sin + 1) / 2 даёт значение от 0 до 1
        float scaleFactor = 1f + (Mathf.Sin(Time.time * scaleSpeed) + 1f) * 0.5f * scaleAmount;
        rectTransform.localScale = Vector3.one * scaleFactor;
    }
}