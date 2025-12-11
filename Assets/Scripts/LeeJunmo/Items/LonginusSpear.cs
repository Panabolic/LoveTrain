using UnityEngine;

public class LonginusSpear : MonoBehaviour
{
    private float damage;
    private float speed;
    private float lifeTime; // 수명
    private Vector3 moveDirection;

    private System.Action onDisappearCallback;
    private float timer = 0f; // 경과 시간

    // ✨ [수정] Initialize에서 lifeTime을 받도록 변경
    public void Initialize(float damage, float speed, float lifeTime, Vector3 startPos, Vector3 targetPos, System.Action onDisappear)
    {
        this.damage = damage;
        this.speed = speed;
        this.lifeTime = lifeTime;
        this.onDisappearCallback = onDisappear;

        transform.position = startPos;

        // 이동 방향 계산 (목표를 향해)
        this.moveDirection = (targetPos - startPos).normalized;

        // 회전 설정
        float angle = Mathf.Atan2(moveDirection.y, moveDirection.x) * Mathf.Rad2Deg;

        transform.rotation = Quaternion.Euler(0, 0, angle);

        this.timer = 0f;
    }

    private void Update()
    {
        if (Time.timeScale == 0) return;

        // 1. 이동 (방향대로 계속 직진)
        transform.position += moveDirection * speed * Time.deltaTime;

        // 2. ✨ [핵심] 시간 체크 (3초 지나면 파괴)
        timer += Time.deltaTime;
        if (timer >= lifeTime)
        {
            Disappear();
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.layer == LayerMask.NameToLayer("Enemy"))
        {
            Enemy enemy = collision.GetComponent<Enemy>();
            if (enemy != null && enemy.gameObject.activeSelf)
            {
                SoundEventBus.Publish(SoundID.Item_Longinus);
                enemy.TakeDamage(damage);
            }
        }
    }

    private void Disappear()
    {
        onDisappearCallback?.Invoke();
        Destroy(gameObject);
    }
}