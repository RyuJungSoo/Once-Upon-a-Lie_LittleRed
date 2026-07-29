using UnityEngine;

public sealed partial class MonsterAimedChargeAttack
{
    private void OnCollisionEnter2D(Collision2D collision)
    {
        TryHitPlayer(collision);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        TryHitPlayer(collision);
    }

    private void TryHitPlayer(Collision2D collision)
    {
        if (!IsCharging ||
            hasDamagedThisCharge ||
            !collision.gameObject.CompareTag("Player"))
        {
            return;
        }

        ResolvePlayerMental();

        if (playerMental == null ||
            playerMental.IsDepleted)
        {
            BeginRecovery();
            return;
        }

        playerMental.TakeMentalDamage(monsterHealth.Damage);

        PlayerMovement playerMovement =
            collision.gameObject
                .GetComponentInParent<PlayerMovement>();

        if (playerMovement != null)
        {
            playerMovement.ApplyKnockback(transform.position);
        }

        hasDamagedThisCharge = true;
        BeginRecovery();
    }

    private void ResolveTarget()
    {
        if (target != null)
        {
            return;
        }

        GameObject player =
            GameObject.FindGameObjectWithTag("Player");

        if (player != null)
        {
            target = player.transform;
        }
    }

    private void ResolvePlayerMental()
    {
        if (playerMental != null)
        {
            return;
        }

        if (GameManager.HasInstance)
        {
            playerMental =
                GameManager.Instance
                    .GetComponent<PlayerMental>();
        }

        if (playerMental == null)
        {
            playerMental = FindFirstObjectByType<PlayerMental>();
        }
    }

    private void PauseChase()
    {
        if (chase == null ||
            !chase.enabled)
        {
            return;
        }

        chase.enabled = false;
        ownsChasePause = true;
    }

    private void ReleaseChase()
    {
        if (!ownsChasePause)
        {
            return;
        }

        chase.enabled = true;
        ownsChasePause = false;
    }

    private void StopMovement()
    {
        if (body != null &&
            body.linearVelocity != Vector2.zero)
        {
            body.linearVelocity = Vector2.zero;
        }
    }

    private void UpdateFacing(Vector2 direction)
    {
        if (spriteRenderer == null ||
            Mathf.Abs(direction.x) <= 0.001f)
        {
            return;
        }

        spriteRenderer.flipX = direction.x < 0f;
    }

    private void OnValidate()
    {
        chargeRange = Mathf.Max(0.1f, chargeRange);
        aimDuration = Mathf.Max(0f, aimDuration);
        chargeSpeed = Mathf.Max(0.1f, chargeSpeed);
        chargeDuration = Mathf.Max(0.05f, chargeDuration);
        recoveryDuration = Mathf.Max(0f, recoveryDuration);
        chargeCooldown = Mathf.Max(0f, chargeCooldown);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.45f, 0.1f, 0.75f);
        Gizmos.DrawWireSphere(transform.position, ChargeRange);
    }
}
