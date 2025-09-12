using UnityEngine;
using UnityEngine.InputSystem;

public class Gun : MonoBehaviour
{
    public GameObject bulletPrefab; // �߻��� źȯ ������
    public Transform firePoint;     // źȯ�� ������ ��ġ (�ѱ�)

    // ���ο� �Է� �ý��� ���
    public InputAction shootAction;

    private void OnEnable()
    {
        shootAction.Enable();
    }

    private void OnDisable()
    {
        shootAction.Disable();
    }

    void Update()
    {
        // ���콺 ���� ��ư(shootAction)�� ���ȴ��� Ȯ��
        if (shootAction.triggered)
        {
            Shoot();
        }
    }

    void Shoot()
    {
        // źȯ ������ �ν��Ͻ�ȭ (����)
        GameObject bulletInstance = Instantiate(bulletPrefab, firePoint.position, firePoint.rotation);

        // źȯ ��ũ��Ʈ ��������
        Bullet bulletScript = bulletInstance.GetComponent<Bullet>();

        if (bulletScript != null)
        {
            // ���� ������ źȯ �߻� �������� ���
            bulletScript.Launch(transform.right);
        }
    }
}