using UnityEngine;

public sealed partial class HunterDirector
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
                EnterBird();
                break;

            case 1:
                EnterMoth();
                break;

            default:
                EnterSign();
                break;
        }
    }

    private void EnterBird()
    {
        DisableSpecialAttacks();

        birdAttack.enabled = true;
        chase.enabled = true;

        currentPattern = AttackPattern.Bird;
        patternEndTime =
            Time.time + profile.BirdDuration;
    }

    private void EnterMoth()
    {
        DisableSpecialAttacks();

        birdAttack.enabled = false;
        mothAttack.enabled = true;
        chase.enabled = true;

        currentPattern = AttackPattern.Moth;
        patternEndTime =
            Time.time + profile.MothDuration;
    }

    private void EnterSign()
    {
        DisableSpecialAttacks();

        birdAttack.enabled = false;
        signAttack.enabled = true;
        chase.enabled = false;

        currentPattern = AttackPattern.Sign;
        patternEndTime =
            Time.time + profile.SignDuration;
    }

    private void EnterRecovery()
    {
        DisableSpecialAttacks();

        birdAttack.enabled = false;
        chase.enabled = false;
        StopMovement();

        currentPattern = AttackPattern.Recovery;
        patternEndTime =
            Time.time + profile.RecoveryDuration;
    }

    private void DisableSpecialAttacks()
    {
        mothAttack.enabled = false;
        signAttack.enabled = false;
    }

    private void StopAllPatterns()
    {
        if (mothAttack != null)
        {
            mothAttack.enabled = false;
        }

        if (signAttack != null)
        {
            signAttack.enabled = false;
        }

        if (birdAttack != null)
        {
            birdAttack.enabled = false;
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
