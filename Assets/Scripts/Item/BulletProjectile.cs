using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D))]
public sealed class BulletProjectile : MonoBehaviour
{
    [SerializeField, Min(0.1f)] private float lifetime = 3f;

    private Rigidbody2D body;
    private readonly HashSet<MonsterHealth> hitMonsters = new();
    private int damage;
    private int remainingPenetration;
    private bool isSpent;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        body.gravityScale = 0f;
    }

    private void OnEnable()
    {
        hitMonsters.Clear();
        isSpent = false;
        Destroy(gameObject, lifetime);
    }

    public void Launch(
        Vector2 direction,
        PlayerLevelStats levelStats
    )
    {
        if (body == null)
        {
            body = GetComponent<Rigidbody2D>();
        }

        damage = Mathf.RoundToInt(levelStats.AttackPower);
        remainingPenetration = levelStats.Penetration;

        Vector2 normalizedDirection = direction.normalized;
        transform.right = normalizedDirection;
        body.linearVelocity =
            normalizedDirection * levelStats.BulletSpeed;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isSpent)
        {
            return;
        }

        MonsterHealth monster = other.GetComponentInParent<MonsterHealth>();
        if (monster == null)
        {
            isSpent = true;
            Destroy(gameObject);
            return;
        }

        if (!hitMonsters.Add(monster))
        {
            return;
        }

        monster.TakeDamage(
            damage,
            MonsterDamageSource.PlayerBullet
        );

        if (!monster.IsDead)
        {
            MonsterKnockback knockback =
                monster.GetComponent<MonsterKnockback>();

            knockback?.Apply(body.linearVelocity);
        }

        if (remainingPenetration > 0)
        {
            remainingPenetration--;
            return;
        }

        isSpent = true;
        Destroy(gameObject);
    }
}
