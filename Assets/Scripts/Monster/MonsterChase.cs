using UnityEngine;

public sealed class MonsterChase : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform target;

    [Header("Movement")]
    [SerializeField, Min(0f)]
    private float stopDistance = 0.5f;

    private static Transform cachedPlayerTarget;

    private Rigidbody2D body;
    private MonsterHealth monsterHealth;
    private MonsterKnockback knockback;
    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        monsterHealth = GetComponent<MonsterHealth>();
        knockback = GetComponent<MonsterKnockback>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        body.bodyType = RigidbodyType2D.Dynamic;
        body.gravityScale = 0f;
        body.freezeRotation = true;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
    }

    private void OnEnable()
    {
        ResolveTarget();
    }

    private void OnDisable()
    {
        StopMovement();
    }

    private void FixedUpdate()
    {
        if (monsterHealth.IsDead)
        {
            StopMovement();
            return;
        }

        if (GameManager.HasInstance && !GameManager.Instance.IsPlaying)
        {
            StopMovement();
            return;
        }

        if (target == null)
        {
            ResolveTarget();
        }

        if (target == null)
        {
            StopMovement();
            return;
        }

        // 넉백 중에는 추적 이동을 잠시 중지한다.
        if (knockback != null && knockback.IsActive)
        {
            StopMovement();
            return;
        }

        Vector2 offset = (Vector2)target.position - body.position;

        if (offset.sqrMagnitude <= stopDistance * stopDistance)
        {
            StopMovement();
            return;
        }

        Vector2 direction = offset.normalized;

        body.linearVelocity = direction * monsterHealth.MoveSpeed;

        // 현재 몬스터 애니메이션이 Side 기반이므로 좌우 반전
        if (Mathf.Abs(direction.x) > 0.001f)
        {
            spriteRenderer.flipX = direction.x < 0f;
        }
    }

    private void ResolveTarget()
    {
        if (target != null)
        {
            cachedPlayerTarget = target;
            return;
        }

        if (cachedPlayerTarget == null)
        {
            PlayerMental player = FindFirstObjectByType<PlayerMental>();

            if (player != null)
            {
                cachedPlayerTarget = player.transform;
            }
        }

        target = cachedPlayerTarget;
    }

    private void StopMovement()
    {
        if (body != null)
        {
            body.linearVelocity = Vector2.zero;
        }
    }
}