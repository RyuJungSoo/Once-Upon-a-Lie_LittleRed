using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(SpriteRenderer))]
public sealed class MonsterSanityAppearance : MonoBehaviour
{
    private static readonly int SanityStageParameter = Animator.StringToHash("SanityStage");
    private static readonly int MotionStateParameter = Animator.StringToHash("MotionState");

    private static PlayerMental cachedPlayerMental;

    [SerializeField] private PlayerMental playerMental;
    [SerializeField] private EMentalState fallbackMentalState = EMentalState.High;
    [SerializeField] private MonsterMotionState motionState = MonsterMotionState.Idle;
    [SerializeField] private bool detectMovementAutomatically = true;
    [SerializeField, Min(0f)] private float movingSpeedThreshold = 0.01f;

    private Animator animator;
    private PlayerMental subscribedPlayerMental;
    private Vector3 previousPosition;
    private bool isMoving;

    public enum MonsterMotionState
    {
        Idle = 0,
        Run = 1,
        Attack = 2
    }

    public PlayerMental MentalSource => playerMental;
    public EMentalState CurrentMentalState { get; private set; }
    public MonsterMotionState CurrentMotionState => motionState;

    private void Awake()
    {
        animator = GetComponent<Animator>();
    }

    private void OnEnable()
    {
        ResolvePlayerMental();
        SubscribeToMental();
        ApplyMentalState(playerMental != null ? playerMental.CurrentMentalState : fallbackMentalState);
        isMoving = motionState == MonsterMotionState.Run;
        ApplyMotionState();
        previousPosition = transform.position;
    }

    private void OnDisable()
    {
        UnsubscribeFromMental();
        isMoving = false;
        motionState = MonsterMotionState.Idle;
    }

    private void LateUpdate()
    {
        if (playerMental == null)
        {
            ResolvePlayerMental();
            SubscribeToMental();

            if (playerMental != null)
            {
                ApplyMentalState(playerMental.CurrentMentalState);
            }
        }

        if (!detectMovementAutomatically)
        {
            return;
        }

        Vector3 currentPosition = transform.position;

        if (motionState != MonsterMotionState.Attack)
        {
            float minimumDistance = movingSpeedThreshold * Time.deltaTime;
            bool isMoving = (currentPosition - previousPosition).sqrMagnitude
                > minimumDistance * minimumDistance;
            SetMoving(isMoving);
        }

        previousPosition = currentPosition;
    }

    public void SetPlayerMental(PlayerMental source)
    {
        if (playerMental == source)
        {
            return;
        }

        UnsubscribeFromMental();
        playerMental = source;

        if (playerMental != null)
        {
            cachedPlayerMental = playerMental;
        }

        if (!isActiveAndEnabled)
        {
            return;
        }

        SubscribeToMental();
        ApplyMentalState(playerMental != null ? playerMental.CurrentMentalState : fallbackMentalState);
    }

    public void SetMotionState(MonsterMotionState state)
    {
        if (state != MonsterMotionState.Attack)
        {
            isMoving = state == MonsterMotionState.Run;
        }

        if (motionState == state)
        {
            return;
        }

        motionState = state;
        ApplyMotionState();
    }

    public void SetMoving(bool isMoving)
    {
        this.isMoving = isMoving;

        if (motionState == MonsterMotionState.Attack)
        {
            return;
        }

        SetMotionState(isMoving ? MonsterMotionState.Run : MonsterMotionState.Idle);
    }

    public void RestoreMovementMotionState()
    {
        SetMotionState(isMoving ? MonsterMotionState.Run : MonsterMotionState.Idle);
    }

    public void SetAutomaticMovementDetection(bool shouldDetect)
    {
        detectMovementAutomatically = shouldDetect;
        previousPosition = transform.position;
    }

    private void ResolvePlayerMental()
    {
        if (playerMental != null)
        {
            cachedPlayerMental = playerMental;
            return;
        }

        if (cachedPlayerMental == null)
        {
            cachedPlayerMental = FindFirstObjectByType<PlayerMental>();
        }

        playerMental = cachedPlayerMental;
    }

    private void SubscribeToMental()
    {
        if (playerMental == null ||
            subscribedPlayerMental == playerMental)
        {
            return;
        }

        UnsubscribeFromMental();

        playerMental.OnMentalStateChanged += HandleMentalStateChanged;
        subscribedPlayerMental = playerMental;
    }

    private void UnsubscribeFromMental()
    {
        if (subscribedPlayerMental != null)
        {
            subscribedPlayerMental.OnMentalStateChanged -=
                HandleMentalStateChanged;
        }

        subscribedPlayerMental = null;
    }

    private void HandleMentalStateChanged(EMentalState mentalState)
    {
        ApplyMentalState(mentalState);
    }

    private void ApplyMentalState(EMentalState mentalState)
    {
        CurrentMentalState = mentalState;

        if (animator != null)
        {
            animator.SetInteger(
                SanityStageParameter,
                ToAnimatorStage(mentalState)
            );
        }
    }

    private static int ToAnimatorStage(EMentalState mentalState)
    {
        switch (mentalState)
        {
            case EMentalState.Low:
                return 0;

            case EMentalState.Medium:
                return 1;

            case EMentalState.High:
            default:
                return 2;
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
