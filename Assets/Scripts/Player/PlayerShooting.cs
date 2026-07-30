using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerMovement))]
public sealed class PlayerShooting : MonoBehaviour
{
    private const string FireActionPath = "PlayerInput/Fire";
    private const string ReloadActionPath = "PlayerInput/Reload";

    [SerializeField] private InputActionAsset inputActions;
    [SerializeField] private BulletProjectile bulletPrefab;
    [SerializeField] private Camera aimCamera;
    [SerializeField] private PlayerAmmo playerAmmo;
    [SerializeField, Min(0.01f)] private float fireCooldown = 0.25f;
    [SerializeField, Min(0f)] private float spawnOffset = 0.65f;
    [SerializeField, Min(0f)] private float attackAnimationDuration = 0.5f;

    private InputAction fireAction;
    private InputAction reloadAction;
    private PlayerMovement playerMovement;
    private float nextFireTime;

    private void Awake()
    {
        playerMovement = GetComponent<PlayerMovement>();

        if (aimCamera == null)
        {
            aimCamera = Camera.main;
        }

        if (GameManager.HasInstance)
        {
            PlayerAmmo persistentAmmo =
                GameManager.Instance.GetComponent<PlayerAmmo>();

            if (persistentAmmo != null)
            {
                playerAmmo = persistentAmmo;
            }
        }

        if (playerAmmo == null)
        {
            playerAmmo = FindFirstObjectByType<PlayerAmmo>();
        }

        if (inputActions == null ||
            bulletPrefab == null ||
            aimCamera == null ||
            playerAmmo == null)
        {
            Debug.LogError("PlayerShooting references are not fully assigned.", this);
            enabled = false;
            return;
        }

        fireAction = inputActions.FindAction(FireActionPath, true);
        reloadAction = inputActions.FindAction(ReloadActionPath, true);
    }

    private void OnEnable()
    {
        fireAction?.Enable();
        reloadAction?.Enable();
    }

    private void OnDisable()
    {
        fireAction?.Disable();
        reloadAction?.Disable();
    }

    private void Update()
    {
        if (reloadAction.WasPressedThisFrame())
        {
            playerAmmo.TryStartReload();
        }

        if (!fireAction.WasPressedThisFrame())
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

        TryFire(GetAimDirection());
    }

    public bool TryFire(Vector2 aimDirection)
    {
        if (Time.time < nextFireTime ||
            aimDirection.sqrMagnitude <= 0.0001f ||
            !playerAmmo.TryUseAmmo())
        {
            return false;
        }

        aimDirection.Normalize();

        Vector2 spawnPosition = (Vector2)transform.position + aimDirection * spawnOffset;
        BulletProjectile bullet = Instantiate(bulletPrefab, spawnPosition, Quaternion.identity);
        bullet.Launch(aimDirection);

        if(SoundManager.HasInstance)
            SoundManager.Instance.PlaySFX(ESFXType.Fire);

        if (playerMovement.isActiveAndEnabled)
        {
            playerMovement.PlayAttackAnimation(
                aimDirection,
                attackAnimationDuration
            );
        }

        nextFireTime = Time.time + fireCooldown;
        return true;
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
