using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D))]
public sealed class PlayerDash : MonoBehaviour
{
    private const string DashActionPath = "PlayerInput/Dash";
    private const string PlayerBoundsLayerName = "PlayerBounds";
    private const float DiagonalDirection = 0.70710678f;
    private const float CollisionSkin = 0.01f;

    [SerializeField] private InputActionAsset inputActions;
    [SerializeField, Min(0f)] private float dashDistance = 2f;
    [SerializeField, Min(0f)] private float dashInterval = 1f;
    [SerializeField, Min(0.01f)] private float dashDuration = 0.12f;

    private Rigidbody2D body;
    private PlayerMovement playerMovement;
    private InputAction dashAction;
    private Vector2 dashStartPosition;
    private Vector2 dashTargetPosition;
    private float dashElapsedTime;
    private float nextDashTime;
    private ContactFilter2D dashContactFilter;
    private readonly RaycastHit2D[] dashHits = new RaycastHit2D[1];

    public bool IsDashing { get; private set; }

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        playerMovement = GetComponent<PlayerMovement>();
        dashContactFilter = new ContactFilter2D
        {
            useLayerMask = true,
            layerMask = LayerMask.GetMask(PlayerBoundsLayerName),
            useTriggers = false
        };

        if (inputActions == null || playerMovement == null)
        {
            Debug.LogError(
                "PlayerDash references are not fully assigned.",
                this
            );
            enabled = false;
            return;
        }

        dashAction = inputActions.FindAction(DashActionPath, true);
    }

    private void OnEnable()
    {
        dashAction?.Enable();
    }

    private void OnDisable()
    {
        dashAction?.Disable();
        IsDashing = false;
    }

    private void Update()
    {
        if (dashAction == null ||
            !dashAction.WasPressedThisFrame())
        {
            return;
        }

        if (EventSystem.current != null &&
            EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

        if (GameManager.HasInstance &&
            !GameManager.Instance.IsPlaying)
        {
            return;
        }

        TryDash(playerMovement.GetDashDirection());
    }

    private void FixedUpdate()
    {
        if (!IsDashing)
        {
            return;
        }

        float duration = Mathf.Max(0.01f, dashDuration);

        if (dashElapsedTime >= duration)
        {
            IsDashing = false;
            return;
        }

        dashElapsedTime = Mathf.Min(
            dashElapsedTime + Time.fixedDeltaTime,
            duration
        );
        float progress = dashElapsedTime / duration;
        Vector2 nextPosition = Vector2.Lerp(
            dashStartPosition,
            dashTargetPosition,
            progress
        );

        MoveToDashPosition(nextPosition);
    }

    public bool TryDash(Vector2 direction)
    {
        if (IsDashing || Time.time < nextDashTime)
        {
            return false;
        }

        if (direction.sqrMagnitude <= 0.0001f)
        {
            return false;
        }

        direction = SnapToEightDirections(direction);
        dashStartPosition = body.position;
        dashTargetPosition =
            dashStartPosition + direction * dashDistance;
        dashElapsedTime = 0f;
        body.linearVelocity = Vector2.zero;
        IsDashing = true;
        nextDashTime = Time.time + dashInterval;
        return true;
    }

    private void MoveToDashPosition(Vector2 nextPosition)
    {
        Vector2 movement = nextPosition - body.position;
        float distance = movement.magnitude;

        if (distance <= Mathf.Epsilon)
        {
            return;
        }

        Vector2 direction = movement / distance;
        int hitCount = body.Cast(
            direction,
            dashContactFilter,
            dashHits,
            distance
        );

        if (hitCount == 0)
        {
            body.position = nextPosition;
            return;
        }

        float allowedDistance = Mathf.Max(
            0f,
            dashHits[0].distance - CollisionSkin
        );
        body.position += direction * allowedDistance;
        IsDashing = false;
    }

    private static Vector2 SnapToEightDirections(
        Vector2 direction
    )
    {
        float angle = Mathf.Atan2(
            direction.y,
            direction.x
        ) * Mathf.Rad2Deg;
        int directionIndex =
            Mathf.RoundToInt(angle / 45f);
        directionIndex =
            (directionIndex % 8 + 8) % 8;

        return directionIndex switch
        {
            0 => Vector2.right,
            1 => new Vector2(
                DiagonalDirection,
                DiagonalDirection
            ),
            2 => Vector2.up,
            3 => new Vector2(
                -DiagonalDirection,
                DiagonalDirection
            ),
            4 => Vector2.left,
            5 => new Vector2(
                -DiagonalDirection,
                -DiagonalDirection
            ),
            6 => Vector2.down,
            _ => new Vector2(
                DiagonalDirection,
                -DiagonalDirection
            )
        };
    }
}
