using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerMovement))]
public sealed class BlanketRestraintEffect : MonoBehaviour
{
    private PlayerMovement playerMovement;

    private bool isRestrained;
    private bool restorePlayerMovement;
    private float restraintEndTime;

    public bool IsRestrained => isRestrained;

    public float RemainingDuration =>
        isRestrained
            ? Mathf.Max(
                0f,
                restraintEndTime - Time.time
            )
            : 0f;

    private void Awake()
    {
        playerMovement = GetComponent<PlayerMovement>();
    }

    private void Update()
    {
        if (!isRestrained ||
            Time.time < restraintEndTime)
        {
            return;
        }

        Release();
    }

    public void Apply(float duration)
    {
        duration = Mathf.Max(0f, duration);

        if (duration <= 0f ||
            playerMovement == null)
        {
            return;
        }

        if (!isRestrained)
        {
            isRestrained = true;
            restorePlayerMovement =
                playerMovement.enabled;
        }

        restraintEndTime = Mathf.Max(
            restraintEndTime,
            Time.time + duration
        );

        playerMovement.enabled = false;
    }

    private void Release()
    {
        if (!isRestrained)
        {
            return;
        }

        isRestrained = false;

        if (playerMovement != null &&
            restorePlayerMovement)
        {
            playerMovement.enabled = true;
        }

        Destroy(this);
    }

    private void OnDisable()
    {
        if (!isRestrained)
        {
            return;
        }

        isRestrained = false;

        if (playerMovement != null &&
            restorePlayerMovement)
        {
            playerMovement.enabled = true;
        }
    }
}
