using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(SpriteRenderer))]
public sealed class MonsterSanityAppearance : MonoBehaviour
{
    private static readonly int SanityStageParameter = Animator.StringToHash("SanityStage");
    private static readonly int MotionStateParameter = Animator.StringToHash("MotionState");

    private static PlayerSanity cachedPlayerSanity;

    [SerializeField] private PlayerSanity playerSanity;
    [SerializeField] private PlayerSanity.SanityStage fallbackSanityStage = PlayerSanity.SanityStage.High;
    [SerializeField] private MonsterMotionState motionState = MonsterMotionState.Idle;
    [SerializeField] private bool detectMovementAutomatically = true;
    [SerializeField, Min(0f)] private float movingSpeedThreshold = 0.01f;

    private Animator animator;
    private bool isSubscribed;
    private Vector3 previousPosition;

    public enum MonsterMotionState
    {
        Idle = 0,
        Run = 1,
        Attack = 2
    }

    public PlayerSanity SanitySource => playerSanity;
    public PlayerSanity.SanityStage CurrentSanityStage { get; private set; }
    public MonsterMotionState CurrentMotionState => motionState;

    private void Awake()
    {
        animator = GetComponent<Animator>();
    }

    private void OnEnable()
    {
        ResolvePlayerSanity();
        SubscribeToSanity();
        ApplySanityStage(playerSanity != null ? playerSanity.CurrentStage : fallbackSanityStage);
        ApplyMotionState();
        previousPosition = transform.position;
    }

    private void OnDisable()
    {
        UnsubscribeFromSanity();
    }

    private void LateUpdate()
    {
        Vector3 currentPosition = transform.position;

        if (detectMovementAutomatically && motionState != MonsterMotionState.Attack)
        {
            float minimumDistance = movingSpeedThreshold * Time.deltaTime;
            bool isMoving = (currentPosition - previousPosition).sqrMagnitude
                > minimumDistance * minimumDistance;
            SetMoving(isMoving);
        }

        previousPosition = currentPosition;
    }

    public void SetPlayerSanity(PlayerSanity source)
    {
        if (playerSanity == source)
        {
            return;
        }

        UnsubscribeFromSanity();
        playerSanity = source;

        if (playerSanity != null)
        {
            cachedPlayerSanity = playerSanity;
        }

        if (!isActiveAndEnabled)
        {
            return;
        }

        SubscribeToSanity();
        ApplySanityStage(playerSanity != null ? playerSanity.CurrentStage : fallbackSanityStage);
    }

    public void SetMotionState(MonsterMotionState state)
    {
        motionState = state;
        ApplyMotionState();
    }

    public void SetMoving(bool isMoving)
    {
        SetMotionState(isMoving ? MonsterMotionState.Run : MonsterMotionState.Idle);
    }

    private void ResolvePlayerSanity()
    {
        if (playerSanity != null)
        {
            cachedPlayerSanity = playerSanity;
            return;
        }

        if (cachedPlayerSanity == null)
        {
            cachedPlayerSanity = FindFirstObjectByType<PlayerSanity>();
        }

        playerSanity = cachedPlayerSanity;
    }

    private void SubscribeToSanity()
    {
        if (playerSanity == null || isSubscribed)
        {
            return;
        }

        playerSanity.SanityStageChanged += HandleSanityStageChanged;
        isSubscribed = true;
    }

    private void UnsubscribeFromSanity()
    {
        if (playerSanity != null && isSubscribed)
        {
            playerSanity.SanityStageChanged -= HandleSanityStageChanged;
        }

        isSubscribed = false;
    }

    private void HandleSanityStageChanged(
        PlayerSanity.SanityStage previousStage,
        PlayerSanity.SanityStage currentStage)
    {
        ApplySanityStage(currentStage);
    }

    private void ApplySanityStage(PlayerSanity.SanityStage stage)
    {
        CurrentSanityStage = stage;

        if (animator != null)
        {
            animator.SetInteger(SanityStageParameter, (int)stage);
        }
    }

    private void ApplyMotionState()
    {
        if (animator != null)
        {
            animator.SetInteger(MotionStateParameter, (int)motionState);
        }
    }
}
