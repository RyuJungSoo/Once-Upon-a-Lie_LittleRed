using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D))]
public sealed class BulletProjectile : MonoBehaviour
{
    [SerializeField, Min(0f)] private float speed = 12f;
    [SerializeField, Min(0.1f)] private float lifetime = 3f;
    [SerializeField, Min(1)] private int damage = 1;

    private Rigidbody2D body;
    private bool hasHit;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        body.gravityScale = 0f;
    }

    private void OnEnable()
    {
        hasHit = false;
        Destroy(gameObject, lifetime);
    }

    public void Launch(Vector2 direction)
    {
        if (body == null)
        {
            body = GetComponent<Rigidbody2D>();
        }

        Vector2 normalizedDirection = direction.normalized;
        transform.right = normalizedDirection;
        body.linearVelocity = normalizedDirection * speed;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (hasHit)
        {
            return;
        }

        hasHit = true;

        MonsterHealth monster = other.GetComponentInParent<MonsterHealth>();
        if (monster != null)
        {
            monster.TakeDamage(damage);
        }

        Destroy(gameObject);
    }
}
