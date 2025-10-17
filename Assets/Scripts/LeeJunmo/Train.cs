using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem; // 이 줄을 추가하세요!

public enum TrainCar
{
    front, middle, rear
}

public class Train : MonoBehaviour
{
    public float moveSpeed = 5.0f;

    //private List<Collider2D> carColliders;

    private void Awake()
    {
        //carColliders = GetComponentsInChildren<Collider2D>().ToList();
    }

    void Update()
    {
        // Keyboard.current는 현재 연결된 키보드를 의미합니다.
        // 'a' 키가 눌리고 있는지 확인합니다.
        if (Keyboard.current.aKey.isPressed)
        {
            transform.Translate(Vector3.left * moveSpeed * Time.deltaTime);
        }

        // 'd' 키가 눌리고 있는지 확인합니다.
        if (Keyboard.current.dKey.isPressed)
        {
            transform.Translate(Vector3.right * moveSpeed * Time.deltaTime);
        }
    }

    public virtual void TakeDamage(float damageAmount, TrainCar trainCar)
    {
        switch(trainCar)
        {
            case TrainCar.front:

                break;

            case TrainCar.middle:

                break;

            case TrainCar.rear:

                break;
        }
    }

    //public List<Collider2D> GetCarColliders() { return carColliders; }
}