using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem; // �� ���� �߰��ϼ���!

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
        // Keyboard.current�� ���� ����� Ű���带 �ǹ��մϴ�.
        // 'a' Ű�� ������ �ִ��� Ȯ���մϴ�.
        if (Keyboard.current.aKey.isPressed)
        {
            transform.Translate(Vector3.left * moveSpeed * Time.deltaTime);
        }

        // 'd' Ű�� ������ �ִ��� Ȯ���մϴ�.
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