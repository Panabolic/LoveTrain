using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum TrainCar
{
    front, middle, rear
}

public class Train : MonoBehaviour
{
    [Tooltip("A/D키와 상관없이 고정된 기차의 속도 값입니다.")]
    [SerializeField] private float baseSpeedValue = 200f;


    public int Level = 1;

    // CurrentSpeed가 Train의 프로퍼티가 됨
    public float CurrentSpeed { get; private set; }

    [Header("디버그 정보")]
    [SerializeField] private float _currentSpeedForInspector;

    // ✨ [추가] UI가 최대 속도 값을 읽을 수 있도록 public getter를 추가합니다.
    public float BaseSpeedValue => baseSpeedValue;

    private void Awake()
    {
        //carColliders = GetComponentsInChildren<Collider2D>().ToList();
    }

    // 속도 초기화
    private void Start()
    {
        CurrentSpeed = baseSpeedValue;
        _currentSpeedForInspector = CurrentSpeed;
    }

    // 디버그용
    void Update()
    {
        _currentSpeedForInspector = CurrentSpeed;
    }

    public virtual void TakeDamage(float damageAmount, TrainCar trainCar)
    {
        switch (trainCar)
        {
            case TrainCar.front:

                break;

            case TrainCar.middle:

                break;

            case TrainCar.rear:

                break;
        }

        if(CurrentSpeed > 0)
        {
            if(CurrentSpeed - damageAmount <= 0)
            {
                CurrentSpeed = 0;
            }
            else
            {
                CurrentSpeed -= damageAmount;
            }
        }
    }

    public void IncreaseSpeedTest()
    {
        CurrentSpeed += 20;
    }

    public void DecreaseSpeedTest()
    {
        CurrentSpeed -= 20;
    }
}