using UnityEngine;
using System.Collections.Generic;

public class PoisonGas : MonoBehaviour
{
    private float damagePerTick;
    private float tickRate;
    private float moveSpeed; // 이동 속도

    private float tickTimer = 0f;

    // 맵 밖으로 나갔다고 판단할 X 좌표 (기차 뒤쪽)
    private float destroyXPos = -40f;

    private List<Enemy> enemiesInGas = new List<Enemy>();

    public void Initialize(float damage, float tickRate, float speed)
    {
        this.damagePerTick = damage;
        this.tickRate = tickRate;
        this.moveSpeed = speed;
    }

    private void Update()
    {
        if (Time.timeScale == 0) return;

        // 1. 왼쪽으로 이동 (배경 스크롤과 비슷하게 뒤로 밀려남)
        transform.Translate(Vector3.left * moveSpeed * Time.deltaTime);

        // 2. 화면 밖으로 나가면 삭제
        if (transform.position.x < destroyXPos)
        {
            Destroy(gameObject);
            return;
        }

        // 3. 데미지 틱 처리
        tickTimer += Time.deltaTime;
        if (tickTimer >= tickRate)
        {
            DealDamage();
            tickTimer = 0f;
        }
    }

    private void DealDamage()
    {
        // 리스트 역순 순회 (중간 삭제 대비)
        for (int i = enemiesInGas.Count - 1; i >= 0; i--)
        {
            Enemy enemy = enemiesInGas[i];
            if (enemy != null && enemy.gameObject.activeSelf)
            {
                enemy.TakeDamage(damagePerTick);
            }
            else
            {
                // 적이 죽거나 사라졌으면 리스트에서 제거
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