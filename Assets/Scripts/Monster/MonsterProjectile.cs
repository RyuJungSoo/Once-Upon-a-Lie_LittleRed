using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public sealed class MonsterProjectile : MonoBehaviour
{
    private Rigidbody2D body;
    private PlayerMental playerMental;
    private float damage;
    private bool hasHit;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        body.gravityScale = 0f;
    }

    public void Launch(
        Vector2 direction,
        float projectileSpeed,
        float projectileDamage,
        float projectileLifetime
    )
    {
        if (body == null)
        {
            body = GetComponent<Rigidbody2D>();
        }

        Vector2 launchDirection =
            direction.sqrMagnitude > 0f
                ? direction.normalized
                : Vector2.right;

        hasHit = false;
        damage = Mathf.Max(0f, projectileDamage);
        transform.right = launchDirection;
        body.linearVelocity =
            launchDirection * Mathf.Max(0f, projectileSpeed);

        if (Application.isPlaying)
        {
            Destroy(
                gameObject,
                Mathf.Max(0.1f, projectileLifetime)
            );
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (hasHit || !HasPlayerTag(other.transform))
        {
            return;
        }

        hasHit = true;
        ResolvePlayerMental();
        playerMental?.TakeMentalDamage(damage);
        DestroyProjectile();
    }

    private static bool HasPlayerTag(Transform collisionTransform)
    {
        Transform current = collisionTransform;

        while (current != null)
        {
            if (current.CompareTag("Player"))
            {
                return true;
            }

            current = current.parent;
        }

        return false;
    }

    private void ResolvePlayerMental()
    {
        if (playerMental != null)
        {
            return;
        }

        if (GameManager.HasInstance)
        {
            playerMental =
                GameManager.Instance.GetComponent<PlayerMental>();
        }

        if (playerMental == null)
        {
            playerMental =
                FindFirstObjectByType<PlayerMental>();
        }
    }

    private void DestroyProjectile()
    {
        if (Application.isPlaying)
        {
            Destroy(gameObject);
            return;
        }

        DestroyImmediate(gameObject);
    }
}
