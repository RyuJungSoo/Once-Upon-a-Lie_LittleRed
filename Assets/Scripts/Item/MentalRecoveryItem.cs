using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(BoxCollider2D))]
public sealed class MentalRecoveryItem : MonoBehaviour
{
    [Header("Mental Recovery")]
    [SerializeField, Range(0f, 1f)]
    private float mentalRestoreRatio;

    [Header("Timed Protection")]
    [SerializeField, Min(0f)]
    private float passiveMentalDrainPauseDuration;

    [SerializeField, Min(0f)]
    private float incomingMentalDamageBlockDuration;

    [Header("Pickup Audio")]
    [SerializeField]
    private ESFXType pickupSfxType;

    public float MentalRestoreRatio =>
        mentalRestoreRatio;

    public float PassiveMentalDrainPauseDuration =>
        passiveMentalDrainPauseDuration;

    public float IncomingMentalDamageBlockDuration =>
        incomingMentalDamageBlockDuration;

    public ESFXType PickupSfxType =>
        pickupSfxType;

    private bool isConsumed;

    private void OnEnable()
    {
        isConsumed = false;
    }

    private void Reset()
    {
        BoxCollider2D itemCollider =
            GetComponent<BoxCollider2D>();

        if (itemCollider != null)
        {
            itemCollider.isTrigger = true;
        }
    }

    private void OnValidate()
    {
        mentalRestoreRatio = Mathf.Clamp01(
            mentalRestoreRatio
        );
        passiveMentalDrainPauseDuration = Mathf.Max(
            0f,
            passiveMentalDrainPauseDuration
        );
        incomingMentalDamageBlockDuration = Mathf.Max(
            0f,
            incomingMentalDamageBlockDuration
        );
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isConsumed ||
            !other.CompareTag("Player"))
        {
            return;
        }

        PlayerMental playerMental =
            ResolvePlayerMental();

        if (!TryApply(playerMental))
        {
            return;
        }

        if (!ItemPool.Release(gameObject))
        {
            Destroy(gameObject);
        }
    }

    public bool TryApply(PlayerMental playerMental)
    {
        if (isConsumed || playerMental == null)
        {
            return false;
        }

        playerMental.RestoreMentalByMaxRatio(
            mentalRestoreRatio
        );
        playerMental.PausePassiveMentalDrain(
            passiveMentalDrainPauseDuration
        );
        playerMental.BlockIncomingMentalDamage(
            incomingMentalDamageBlockDuration
        );

        isConsumed = true;
        PlayPickupSfx();
        return true;
    }

    private void PlayPickupSfx()
    {
        if (!SoundManager.HasInstance)
        {
            return;
        }

        SoundManager.Instance.PlaySFX(
            pickupSfxType
        );
    }

    private static PlayerMental ResolvePlayerMental()
    {
        if (GameManager.HasInstance)
        {
            PlayerMental playerMental =
                GameManager.Instance
                    .GetComponent<PlayerMental>();

            if (playerMental != null)
            {
                return playerMental;
            }
        }

        return FindFirstObjectByType<PlayerMental>();
    }
}
