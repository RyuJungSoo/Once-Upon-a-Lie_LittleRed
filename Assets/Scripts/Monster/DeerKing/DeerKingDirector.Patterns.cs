using UnityEngine;

public sealed partial class DeerKingDirector
{
    private void Update()
    {
        if (profile == null ||
            monsterHealth == null ||
            monsterHealth.IsDead)
        {
            StopAllPatterns();
            return;
        }

        if (GameManager.HasInstance &&
            !GameManager.Instance.IsPlaying)
        {
            StopAllPatterns();
            return;
        }

        switch (currentPattern)
        {
            case AttackPattern.Ram:
            case AttackPattern.Ranged:
                if (Time.time >= patternEndTime)
                {
                    EnterRecovery();
                }
                break;

            case AttackPattern.AimedCharge:
                if (!chargeStarted ||
                    !aimedCharge.IsRunning)
                {
                    EnterRecovery();
                }
                break;

            case AttackPattern.Recovery:
                if (Time.time >= patternEndTime)
                {
                    EnterNextPattern();
                }
                break;
        }
    }

    private void EnterNextPattern()
    {
        if (profile == null)
        {
            StopAllPatterns();
            return;
        }

        nextPatternIndex = (nextPatternIndex + 1) % 3;

        switch (nextPatternIndex)
        {
            case 0:
                EnterRam();
                break;

            case 1:
                EnterRanged();
                break;

            default:
                EnterAimedCharge();
                break;
        }
    }

    private void EnterRam()
    {
        rangedAttack.enabled = false;
        aimedCharge.CancelAttack();

        contactAttack.enabled = true;
        chase.enabled = true;

        currentPattern = AttackPattern.Ram;
        patternEndTime = Time.time + profile.RamDuration;
        chargeStarted = false;
    }

    private void EnterRanged()
    {
        rangedAttack.enabled = false;
        aimedCharge.CancelAttack();

        contactAttack.enabled = false;
        chase.enabled = true;
        rangedAttack.enabled = true;

        currentPattern = AttackPattern.Ranged;
        patternEndTime = Time.time + profile.RangedDuration;
        chargeStarted = false;
    }

    private void EnterAimedCharge()
    {
        rangedAttack.enabled = false;
        aimedCharge.CancelAttack();

        contactAttack.enabled = false;
        chase.enabled = true;

        currentPattern = AttackPattern.AimedCharge;
        chargeStarted = aimedCharge.StartCharge(
            chase.Target,
            profile.AimedCharge
        );

        if (!chargeStarted)
        {
            EnterRecovery();
        }
    }

    private void EnterRecovery()
    {
        rangedAttack.enabled = false;
        aimedCharge.CancelAttack();
        contactAttack.enabled = false;
        chase.enabled = false;
        StopMovement();

        currentPattern = AttackPattern.Recovery;
        patternEndTime = Time.time + profile.RecoveryDuration;
        chargeStarted = false;
    }

    private void StopAllPatterns()
    {
        if (rangedAttack != null)
        {
            rangedAttack.enabled = false;
        }

        aimedCharge?.CancelAttack();

        if (contactAttack != null)
        {
            contactAttack.enabled = false;
        }

        if (chase != null)
        {
            chase.enabled = false;
        }

        StopMovement();
    }

    private void StopMovement()
    {
        if (body != null &&
            body.linearVelocity != Vector2.zero)
        {
            body.linearVelocity = Vector2.zero;
        }
    }
}
