using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerMovement))]
public sealed class PlayerShooting : MonoBehaviour
{
    private const string FireActionPath = "PlayerInput/Fire";

    [SerializeField] private InputActionAsset inputActions;
    [SerializeField] private BulletProjectile bulletPrefab;
    [SerializeField] private Camera aimCamera;
    [SerializeField, Min(0.01f)] private float fireCooldown = 0.25f;
    [SerializeField, Min(0f)] private float spawnOffset = 0.65f;
    [SerializeField, Min(0f)] private float attackAnimationDuration = 0.5f;

    private InputAction fireAction;
    private PlayerMovement playerMovement;
    private float nextFireTime;

    private void Awake()
    {
        playerMovement = GetComponent<PlayerMovement>();

        if (aimCamera == null)
        {
            aimCamera = Camera.main;
        }

        if (inputActions == null || bulletPrefab == null || aimCamera == null)
        {
            Debug.LogError("PlayerShooting references are not fully assigned.", this);
            enabled = false;
            return;
        }

        fireAction = inputActions.FindAction(FireActionPath, true);
    }

    private void OnEnable()
    {
        fireAction?.Enable();
    }

    private void OnDisable()
    {
        fireAction?.Disable();
    }

    private void Update()
    {
        if (!fireAction.WasPressedThisFrame() || Time.time < nextFireTime)
        {
            return;
        }

        Vector2 aimDirection = GetAimDirection();

        if (aimDirection.sqrMagnitude <= 0.0001f)
        {
            return;
        }

        Vector2 spawnPosition = (Vector2)transform.position + aimDirection * spawnOffset;
        BulletProjectile bullet = Instantiate(bulletPrefab, spawnPosition, Quaternion.identity);
        bullet.Launch(aimDirection);

        playerMovement.PlayAttackAnimation(aimDirection, attackAnimationDuration);
        nextFireTime = Time.time + fireCooldown;
    }

    private Vector2 GetAimDirection()
    {
        Vector2 mousePosition = Mouse.current.position.ReadValue();
        float distanceToGameplayPlane = Mathf.Abs(aimCamera.transform.position.z - transform.position.z);
        Vector3 screenPosition = new(mousePosition.x, mousePosition.y, distanceToGameplayPlane);
        Vector2 worldPosition = aimCamera.ScreenToWorldPoint(screenPosition);
        return (worldPosition - (Vector2)transform.position).normalized;
    }
}
