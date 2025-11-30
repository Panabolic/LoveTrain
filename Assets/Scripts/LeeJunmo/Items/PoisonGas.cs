using UnityEngine;
using System.Collections.Generic;

public class PoisonGas : MonoBehaviour
{
    private float damagePerTick;
    private float tickRate;
    private float moveSpeed;

    private float tickTimer = 0f;
    private float destroyXPos = -40f;

    private List<Enemy> enemiesInGas = new List<Enemy>();

    // ✨ [추가] 내 콜라이더 제어용 변수
    private Collider2D myCollider;

    private void Awake()
    {
        myCollider = GetComponent<Collider2D>();
    }

    public void Initialize(float damage, float tickRate, float speed)
    {
        this.damagePerTick = damage;
        this.tickRate = tickRate;
        this.moveSpeed = speed;

        // ✨ [핵심] 생성 초기에는 콜라이더를 꺼서 데미지 판정을 막음
        if (myCollider != null)
        {
            myCollider.enabled = false;
        }

        // 초기화 시 리스트 비우기 (풀링 대비)
        enemiesInGas.Clear();
    }

    // ✨ [Animation Event] 애니메이션의 특정 프레임(가스가 퍼진 시점)에서 호출
    public void AnimEvent_EnableGas()
    {
        if (myCollider != null)
        {
            myCollider.enabled = true;
        }
    }

    private void Update()
    {
        if (Time.timeScale == 0) return;

        transform.Translate(Vector3.left * moveSpeed * Time.deltaTime);

        if (transform.position.x < destroyXPos)
        {
            Destroy(gameObject);
            return;
        }

        tickTimer += Time.deltaTime;
        if (tickTimer >= tickRate)
        {
            DealDamage();
            tickTimer = 0f;
        }
    }

    private void DealDamage()
    {
        for (int i = enemiesInGas.Count - 1; i >= 0; i--)
        {
            Enemy enemy = enemiesInGas[i];
            if (enemy != null && enemy.gameObject.activeSelf)
            {
                enemy.TakeDamage(damagePerTick);
            }
            else
            {
                enemiesInGas.RemoveAt(i);
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        Enemy enemy = collision.GetComponent<Enemy>();
        if (enemy != null && !enemiesInGas.Contains(enemy))
        {
            enemiesInGas.Add(enemy);
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        Enemy enemy = collision.GetComponent<Enemy>();
        if (enemy != null && enemiesInGas.Contains(enemy))
        {
            enemiesInGas.Remove(enemy);
        }
    }
}