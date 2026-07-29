using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(MonsterHealth))]
public sealed class MonsterChase : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform target;

    private static Transform cachedPlayerTarget;

    private Rigidbody2D body;
    private MonsterHealth monsterHealth;
    private MonsterKnockback knockback;
    private MonsterSanityAppearance appearance;
    private SpriteRenderer spriteRenderer;
    private bool isMoving;

    public bool IsMoving => isMoving;
    public float StopDistance
    {
        get
        {
            MonsterStats stats = GetStats();

            return stats != null
                ? stats.Chase.StopDistance
                : 0.1f;
        }
    }

    public Transform Target
    {
        get
        {
            if (target == null)
            {
                ResolveTarget();
            }

            return target;
        }
    }

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        monsterHealth = GetComponent<MonsterHealth>();
        knockback = GetComponent<MonsterKnockback>();
        appearance = GetComponent<MonsterSanityAppearance>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (appearance != null)
        {
            appearance.SetAutomaticMovementDetection(false);
        }

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
            StopMovement(true);
            return;
        }

        Vector2 offset = (Vector2)target.position - body.position;

        float stopDistance = StopDistance;

        if (offset.sqrMagnitude <= stopDistance * stopDistance)
        {
            StopMovement();
            return;
        }

        Vector2 direction = offset.normalized;

        SetMovement(direction * monsterHealth.MoveSpeed);

        // 현재 몬스터 애니메이션이 Side 기반이므로 좌우 반전
        if (Mathf.Abs(direction.x) > 0.001f)
        {
            bool shouldFlip = direction.x < 0f;

            if (spriteRenderer.flipX != shouldFlip)
            {
                spriteRenderer.flipX = shouldFlip;
            }
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
            GameObject player = GameObject.FindGameObjectWithTag("Player");

            if (player != null)
            {
                cachedPlayerTarget = player.transform;
            }
        }

        target = cachedPlayerTarget;
    }

    private void SetMovement(Vector2 velocity)
    {
        SetVelocity(velocity);
        ReportMovement(velocity.sqrMagnitude > 0f);
    }

    private void StopMovement(bool preserveVisualMovement = false)
    {
        SetVelocity(Vector2.zero);
        ReportMovement(preserveVisualMovement);
    }

    private void SetVelocity(Vector2 velocity)
    {
        if (body != null && body.linearVelocity != velocity)
        {
            body.linearVelocity = velocity;
        }
    }

    private void ReportMovement(bool moving)
    {
        if (isMoving == moving)
        {
            return;
        }

        isMoving = moving;

        if (appearance != null)
        {
            appearance.SetMoving(isMoving);
        }
    }

    private MonsterStats GetStats()
    {
        if (monsterHealth == null)
        {
            monsterHealth = GetComponent<MonsterHealth>();
        }

        return monsterHealth != null
            ? monsterHealth.Stats
            : null;
    }
}
