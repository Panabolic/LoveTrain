using UnityEngine;

public class Bullet : MonoBehaviour
{
    public float speed = 10f; // �ν����Ϳ��� ������ �� �ִ� źȯ�� �ӵ�

    private Rigidbody2D rb;

    void Awake()
    {
        // Rigidbody2D ������Ʈ ��������
        rb = GetComponent<Rigidbody2D>();
    }

    public void Launch(Vector2 direction)
    {
        // Rigidbody2D�� ���� ���� źȯ �߻�
        rb.linearVelocity = direction.normalized * speed;
    }
}