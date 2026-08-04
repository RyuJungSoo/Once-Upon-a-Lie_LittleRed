using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D))]
public sealed class PlayerDash : MonoBehaviour
{
    private const string DashActionPath = "PlayerInput/Dash";

    [SerializeField] private InputActionAsset inputActions;
    [SerializeField] private Camera aimCamera;
    [SerializeField, Min(0f)] private float dashDistance = 2f;

    private Rigidbody2D body;
    private InputAction dashAction;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();

        if (aimCamera == null)
        {
            aimCamera = Camera.main;
        }

        if (inputActions == null || aimCamera == null)
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
    }

    private void Update()
    {
        if (dashAction == null ||
            !dashAction.WasPressedThisFrame() ||
            Mouse.current == null)
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

        TryDash(GetPointerWorldPosition());
    }

    public bool TryDash(Vector2 targetWorldPosition)
    {
        Vector2 direction = targetWorldPosition - body.position;

        if (direction.sqrMagnitude <= 0.0001f)
        {
            return false;
        }

        body.position += direction.normalized * dashDistance;
        return true;
    }

    private Vector2 GetPointerWorldPosition()
    {
        Vector2 mousePosition = Mouse.current.position.ReadValue();
        float distanceToGameplayPlane = Mathf.Abs(
            aimCamera.transform.position.z - transform.position.z
        );
        Vector3 screenPosition = new(
            mousePosition.x,
            mousePosition.y,
            distanceToGameplayPlane
        );
        return aimCamera.ScreenToWorldPoint(screenPosition);
    }
}
