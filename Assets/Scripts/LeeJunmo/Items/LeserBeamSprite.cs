using UnityEngine;
using System.Collections.Generic;

public class LaserBeamSprite : MonoBehaviour
{
    [Header("설정")]
    public float damageInterval = 0.2f;

    private float damage;
    private float damageTimer;
    private List<Mob> enemiesInRange = new List<Mob>();

    public void Init(float dmg)
    {
        this.damage = dmg;
    }

    private void OnEnable()
    {
        damageTimer = 0f;
        enemiesInRange.Clear();
    }

    private void Update()
    {
        damageTimer += Time.deltaTime;
        if (damageTimer >= damageInterval)
        {
            DealDamageToAll();
            damageTimer = 0f;
        }
    }

    private void DealDamageToAll()
    {
        for (int i = enemiesInRange.Count - 1; i >= 0; i--)
        {
            Mob enemy = enemiesInRange[i];

            if (enemy != null && enemy.gameObject.activeSelf)
            {
                enemy.TakeDamage(damage);
            }
            else
            {
                enemiesInRange.RemoveAt(i);
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        Mob enemy = collision.GetComponent<Mob>();
        if (enemy != null && !enemiesInRange.Contains(enemy))
        {
            enemiesInRange.Add(enemy);
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        Mob enemy = collision.GetComponent<Mob>();
        if (enemy != null && enemiesInRange.Contains(enemy))
        {
            enemiesInRange.Remove(enemy);
        }
    }
}