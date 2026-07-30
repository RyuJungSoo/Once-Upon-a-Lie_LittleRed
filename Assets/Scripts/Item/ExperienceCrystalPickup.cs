using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(BoxCollider2D))]
public sealed class ExperienceCrystalPickup : MonoBehaviour
{
    [SerializeField, Min(1)]
    private int experienceAmount = 1;

    public int ExperienceAmount =>
        experienceAmount;

    private bool isConsumed;

    private void Reset()
    {
        BoxCollider2D pickupCollider =
            GetComponent<BoxCollider2D>();

        if (pickupCollider != null)
        {
            pickupCollider.isTrigger = true;
        }
    }

    private void OnValidate()
    {
        experienceAmount = Mathf.Max(
            1,
            experienceAmount
        );
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isConsumed ||
            !other.CompareTag("Player"))
        {
            return;
        }

        if(SoundManager.HasInstance)
            SoundManager.Instance.PlaySFX(ESFXType.ExpCrystal);

        PlayerExperience playerExperience =
            ResolvePlayerExperience();

        if (!TryCollect(playerExperience))
        {
            return;
        }

        Destroy(gameObject);
    }

    public bool TryCollect(
        PlayerExperience playerExperience
    )
    {
        if (isConsumed ||
            playerExperience == null)
        {
            return false;
        }

        playerExperience.AddExperience(
            experienceAmount
        );

        isConsumed = true;
        return true;
    }

    private static PlayerExperience
        ResolvePlayerExperience()
    {
        if (GameManager.HasInstance)
        {
            PlayerExperience playerExperience =
                GameManager.Instance.PlayerExperience;

            if (playerExperience != null)
            {
                return playerExperience;
            }
        }

        return FindFirstObjectByType
            <PlayerExperience>();
    }
}
