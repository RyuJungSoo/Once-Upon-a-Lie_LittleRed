using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(MonsterHealth))]
[RequireComponent(typeof(MonsterChase))]
public sealed class BlanketContactRestraint : MonoBehaviour
{
    [Header("Contact Restraint")]
    [SerializeField, Min(0f)]
    private float restraintDuration = 1.5f;

    private MonsterHealth monsterHealth;
    private PlayerMental playerMental;
    private bool hasDetonated;

    public float RestraintDuration => restraintDuration;
    public bool HasDetonated => hasDetonated;

    private void Awake()
    {
        monsterHealth = GetComponent<MonsterHealth>();
        ResolvePlayerMental();
    }

    private void OnEnable()
    {
        hasDetonated = false;
    }

    private void OnCollisionEnter2D(
        Collision2D collision
    )
    {
        TryDetonate(collision.collider);
    }

    private void OnCollisionStay2D(
        Collision2D collision
    )
    {
        TryDetonate(collision.collider);
    }

    private void TryDetonate(Collider2D other)
    {
        if (hasDetonated ||
            other == null ||
            monsterHealth == null ||
            monsterHealth.IsDead)
        {
            return;
        }

        if (GameManager.HasInstance &&
            !GameManager.Instance.IsPlaying)
        {
            return;
        }

        PlayerMovement playerMovement =
            other.GetComponentInParent<PlayerMovement>();

        if (playerMovement == null ||
            !playerMovement.CompareTag("Player"))
        {
            return;
        }

        hasDetonated = true;

        DamagePlayer();
        RestrainPlayer(playerMovement);

        monsterHealth.TakeDamage(
            monsterHealth.CurrentHealth
        );
    }

    private void DamagePlayer()
    {
        ResolvePlayerMental();

        if (playerMental == null ||
            playerMental.IsDepleted)
        {
            return;
        }

        playerMental.TakeMentalDamage(
            monsterHealth.Damage
        );
    }

    private void RestrainPlayer(
        PlayerMovement playerMovement
    )
    {
        BlanketRestraintEffect restraint =
            playerMovement.GetComponent
                <BlanketRestraintEffect>();

        if (restraint == null)
        {
            restraint = playerMovement.gameObject
                .AddComponent<BlanketRestraintEffect>();
        }

        restraint.Apply(restraintDuration);
    }

    private void ResolvePlayerMental()
    {
        if (playerMental != null)
        {
            return;
        }

        if (GameManager.HasInstance)
        {
            playerMental = GameManager.Instance
                .GetComponent<PlayerMental>();
        }

        if (playerMental == null)
        {
            playerMental =
                FindFirstObjectByType<PlayerMental>();
        }
    }

    private void OnValidate()
    {
        restraintDuration =
            Mathf.Max(0f, restraintDuration);
    }
}
