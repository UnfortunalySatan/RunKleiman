using UnityEngine;

public class Trap2 : MonoBehaviour
{
    [Header("Настройки вращения")]
    [SerializeField] private float upSpeed = 20f;     // скорость подъёма (град/сек)
    [SerializeField] private float downSpeed = 60f;   // скорость опускания (град/сек)
    [SerializeField] private float minAngle = 0f;
    [SerializeField] private float maxAngle = 75f;


    private float currentAngle = 0f;
    private bool isGoingUp = true;



    private void FixedUpdate()
    {
        Rotate();
    }

    void Rotate()
    {
        // Выбираем скорость в зависимости от направления
        float speed = isGoingUp ? upSpeed : downSpeed;
        float delta = speed * Time.fixedDeltaTime;

        // Меняем угол
        if (isGoingUp)
        {
            currentAngle += delta;
            if (currentAngle >= maxAngle)
            {
                currentAngle = maxAngle;
                isGoingUp = false;
            }
        }
        else
        {
            currentAngle -= delta;
            if (currentAngle <= minAngle)
            {
                currentAngle = minAngle;
                isGoingUp = true;
            }
        }

        // Применяем поворот локально по оси X
        transform.localRotation = Quaternion.Euler(currentAngle, 0f, 0f);

    }
}