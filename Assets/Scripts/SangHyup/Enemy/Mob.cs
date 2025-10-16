using System.Collections;
using UnityEngine;

public class Mob : Enemy
{
    // Components
    protected Rigidbody2D rigid2D;

    [Tooltip("�⺻ �ӵ�")]
    [SerializeField] protected float moveSpeed = 2.0f;
    [Tooltip("����� ���� �ӵ� ���� (��: 0.5 = 50%)")]
    [SerializeField] protected float minSpeedMultiplier = 0.5f;
    [Tooltip("����� �ְ� �ӵ� ���� (��: 2.0 = 200%)")]
    [SerializeField] protected float maxSpeedMultiplier = 2.0f;

    //private float moveDirection;

    // Target Components
    protected TrainController trainController;


    private void Awake()
    {
        rigid2D = GetComponent<Rigidbody2D>();

        // Get Target Components
        trainController = GameObject.FindWithTag("Player").GetComponent<TrainController>();
    }

    void Start()
    {
        if (trainController == null || targetRigid == null)
        {
            Debug.LogError("TrainController �Ǵ� Target�� ������� �ʾҽ��ϴ�!", this.gameObject);
            this.enabled = false;
        }
    }

    private void FixedUpdate()
    {
        if (!isAlive) return;

        // �̵� ���� ������
        //moveDirection = Mathf.Sign(targetRigid.position.x - rigid2D.position.x);
        //Vector2 nextXPosition = new Vector2(moveDirection * moveSpeed, rigid2D.position.y) * Time.fixedDeltaTime;
        //rigid2D.MovePosition(rigid2D.position + nextXPosition);
        //rigid2D.linearVelocity = Vector2.zero;

        // ������ ���� �ӷ°� �ӷ� ����(0.0 ~ 1.0)�� ���
        float currentTrainSpeed = trainController.CurrentSpeed;
        float speedRate = Mathf.InverseLerp(trainController.minSpeed, trainController.maxSpeed, currentTrainSpeed);

        float currentMultiplier;

        // �ڽ��� X��ǥ�� ������ X��ǥ�� ���Ͽ� ��/�ڸ� �Ǵ�
        if (transform.position.x > targetRigid.position.x)
        {
            // [��Ȳ] ���� �������� �տ� ���� ��
            // ���� �ӵ��� ������ ����� ���谡 �˴ϴ�.
            // ������ ����������(speedPercentage �� 1.0), ������ �ִ�ġ(maxMultiplier)�� ��������ϴ�.
            currentMultiplier = Mathf.Lerp(minSpeedMultiplier, maxSpeedMultiplier, speedRate);
        }
        else
        {
            // [��Ȳ] ���� �������� �ڿ� ���� ��
            // ���� �ӵ��� ������ �ݺ�� ���谡 �˴ϴ�.
            // Lerp�� min, max ������ �ٲ㼭, ������ ����������(speedPercentage �� 1.0) ������ �ּ�ġ(minMultiplier)�� ��������ϴ�.
            currentMultiplier = Mathf.Lerp(maxSpeedMultiplier, minSpeedMultiplier, speedRate);
        }

        // ���� �ӷ� = ���� �⺻ �ӷ� * ���� ���� ����
        float finalMoveSpeed = moveSpeed * currentMultiplier;

        // �̵� ����
        if (finalMoveSpeed > 0)
        {
            transform.position = Vector2.MoveTowards(transform.position, targetRigid.position, finalMoveSpeed * Time.deltaTime);
        }
    }

    protected override IEnumerator Die()
    {
        yield return base.Die();

        collision.enabled = true;
        isAlive = false;
        enabled = false;
    }
}
