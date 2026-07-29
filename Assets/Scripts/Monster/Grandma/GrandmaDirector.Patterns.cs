using UnityEngine;

public sealed partial class GrandmaDirector
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

        if (Time.time < patternEndTime)
        {
            return;
        }

        if (currentPattern == AttackPattern.Recovery)
        {
            EnterNextPattern();
            return;
        }

        EnterRecovery();
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
                EnterTeaCup();
                break;

            case 1:
                EnterBlanket();
                break;

            default:
                EnterRedString();
                break;
        }
    }

    private void EnterTeaCup()
    {
        DisableSpecialAttacks();

        redStringAttack.enabled = false;
        chase.enabled = true;
        teaCupAttack.enabled = true;
        chase.enabled = false;
        StopMovement();

        currentPattern = AttackPattern.TeaCup;
        patternEndTime =
            Time.time + profile.TeaCupDuration;
    }

    private void EnterBlanket()
    {
        DisableSpecialAttacks();

        redStringAttack.enabled = false;
        restraintAttack.enabled = true;
        chase.enabled = true;

        currentPattern = AttackPattern.Blanket;
        patternEndTime =
            Time.time + profile.BlanketDuration;
    }

    private void EnterRedString()
    {
        DisableSpecialAttacks();

        redStringAttack.enabled = true;
        chase.enabled = true;

        currentPattern = AttackPattern.RedString;
        patternEndTime =
            Time.time + profile.RedStringDuration;
    }

    private void EnterRecovery()
    {
        DisableSpecialAttacks();

        redStringAttack.enabled = false;
        chase.enabled = false;
        StopMovement();

        currentPattern = AttackPattern.Recovery;
        patternEndTime =
            Time.time + profile.RecoveryDuration;
    }

    private void DisableSpecialAttacks()
    {
        teaCupAttack.enabled = false;
        restraintAttack.enabled = false;
    }

    private void StopAllPatterns()
    {
        if (teaCupAttack != null)
        {
            teaCupAttack.enabled = false;
        }

        if (restraintAttack != null)
        {
            restraintAttack.enabled = false;
        }

        if (redStringAttack != null)
        {
            redStringAttack.enabled = false;
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
