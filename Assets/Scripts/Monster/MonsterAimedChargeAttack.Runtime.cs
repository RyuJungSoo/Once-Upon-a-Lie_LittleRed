using UnityEngine;

public sealed partial class MonsterAimedChargeAttack
{
    private void FixedUpdate()
    {
        if (monsterHealth == null ||
            monsterHealth.IsDead)
        {
            StopMovement();
            return;
        }

        if (GameManager.HasInstance &&
            !GameManager.Instance.IsPlaying)
        {
            StopMovement();
            return;
        }

        if (knockback != null &&
            knockback.IsActive)
        {
            if (IsRunning)
            {
                BeginRecovery();
            }

            StopMovement();
            return;
        }

        if (target == null)
        {
            ResolveTarget();
        }

        switch (currentState)
        {
            case ChargeState.Chasing:
                if (automaticActivation)
                {
                    UpdateChasing();
                }
                break;

            case ChargeState.Aiming:
                UpdateAiming();
                break;

            case ChargeState.Charging:
                UpdateCharging();
                break;

            case ChargeState.Recovering:
                UpdateRecovery();
                break;
        }
    }

    public void SetAutomaticActivation(bool shouldActivate)
    {
        automaticActivation = shouldActivate;
    }

    public bool StartCharge(
        Transform newTarget,
        MonsterAimedChargeSettings settings = null
    )
    {
        if (!isActiveAndEnabled ||
            IsRunning ||
            newTarget == null ||
            Time.time < nextChargeTime)
        {
            return false;
        }

        target = newTarget;
        overrideSettings = settings;
        BeginAiming();
        return true;
    }

    public void CancelAttack()
    {
        currentState = ChargeState.Chasing;
        chargeDirection = Vector2.zero;
        stateEndTime = 0f;
        hasDamagedThisCharge = false;

        StopMovement();
        ReleaseChase();
        appearance?.RestoreMovementMotionState();
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }

    private void ResetState()
    {
        currentState = ChargeState.Chasing;
        chargeDirection = Vector2.zero;
        stateEndTime = 0f;
        nextChargeTime = 0f;
        hasDamagedThisCharge = false;
        ownsChasePause = false;
    }

    private void UpdateChasing()
    {
        ReleaseChase();

        if (target == null ||
            Time.time < nextChargeTime)
        {
            return;
        }

        Vector2 offset =
            (Vector2)target.position - body.position;

        if (offset.sqrMagnitude <=
            ChargeRange * ChargeRange)
        {
            BeginAiming();
        }
    }

    private void UpdateAiming()
    {
        StopMovement();
        UpdateAimDirection();

        if (Time.time >= stateEndTime)
        {
            BeginCharge();
        }
    }

    private void UpdateCharging()
    {
        ApplyChargeMovement();

        if (Time.time >= stateEndTime)
        {
            BeginRecovery();
        }
    }

    private void UpdateRecovery()
    {
        StopMovement();

        if (Time.time < stateEndTime)
        {
            return;
        }

        currentState = ChargeState.Chasing;
        ReleaseChase();
        appearance?.RestoreMovementMotionState();
    }

    private void BeginAiming()
    {
        currentState = ChargeState.Aiming;
        stateEndTime = Time.time + AimDuration;

        PauseChase();
        StopMovement();
        UpdateAimDirection();

        appearance?.SetMotionState(
            MonsterSanityAppearance.MonsterMotionState.Idle
        );
    }

    private void UpdateAimDirection()
    {
        if (target == null)
        {
            return;
        }

        Vector2 direction =
            (Vector2)target.position - body.position;

        if (direction.sqrMagnitude <= 0.0001f)
        {
            return;
        }

        chargeDirection = direction.normalized;
        UpdateFacing(chargeDirection);
    }

    private void BeginCharge()
    {
        if (chargeDirection.sqrMagnitude <= 0.0001f)
        {
            BeginRecovery();
            return;
        }

        currentState = ChargeState.Charging;
        stateEndTime = Time.time + ChargeDuration;
        hasDamagedThisCharge = false;

        appearance?.SetMotionState(
            MonsterSanityAppearance.MonsterMotionState.Attack
        );

        ApplyChargeMovement();
    }

    private void ApplyChargeMovement()
    {
        if (body == null)
        {
            return;
        }

        body.linearVelocity =
            chargeDirection * ChargeSpeed;
    }

    private void BeginRecovery()
    {
        if (currentState == ChargeState.Recovering)
        {
            return;
        }

        currentState = ChargeState.Recovering;
        stateEndTime = Time.time + RecoveryDuration;
        nextChargeTime = stateEndTime + ChargeCooldown;

        StopMovement();

        appearance?.SetMotionState(
            MonsterSanityAppearance.MonsterMotionState.Idle
        );
    }

}
