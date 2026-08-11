using UnityEngine;

public class Trap1 : MonoBehaviour
{
    [SerializeField] private float rotateSpeedMin = 50f;   // минимальная скорость
    [SerializeField] private float rotateSpeedMax = 140f;  // максимальная скорость
    private float rotateSpeed;                             // текущая скорость (будет рандомной)
    private Vector3 rotateAxis;                            // ось вращения

    void Start()
    {
        // Случайная скорость в диапазоне
        rotateSpeed = Random.Range(rotateSpeedMin, rotateSpeedMax);

        // Случайное направление по оси Y: либо (0,1,0), либо (0,-1,0)
        float direction = Random.Range(0, 2) == 0 ? -1f : 1f;
        rotateAxis = new Vector3(0, direction, 0);
    }

    void FixedUpdate()
    {
        Rotate();
    }

    void Rotate()
    {
        // Вращаем вокруг выбранной оси с заданной скоростью
        transform.Rotate(rotateAxis, rotateSpeed * Time.deltaTime);
    }
    
}