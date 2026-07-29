using System;
using UnityEngine;

[Serializable]
public sealed class MonsterAimedChargeSettings
{
    [Header("Detection")]
    [SerializeField, Min(0.1f)]
    private float chargeRange = 4f;

    [Header("Aiming")]
    [SerializeField, Min(0f)]
    private float aimDuration = 0.65f;

    [Header("Charge")]
    [SerializeField, Min(0.1f)]
    private float chargeSpeed = 8f;

    [SerializeField, Min(0.05f)]
    private float chargeDuration = 0.75f;

    [Header("Recovery")]
    [SerializeField, Min(0f)]
    private float recoveryDuration = 0.65f;

    [SerializeField, Min(0f)]
    private float chargeCooldown = 1.5f;

    public float ChargeRange => chargeRange;
    public float AimDuration => aimDuration;
    public float ChargeSpeed => chargeSpeed;
    public float ChargeDuration => chargeDuration;
    public float RecoveryDuration => recoveryDuration;
    public float ChargeCooldown => chargeCooldown;

    internal void Validate()
    {
        chargeRange = Mathf.Max(0.1f, chargeRange);
        aimDuration = Mathf.Max(0f, aimDuration);
        chargeSpeed = Mathf.Max(0.1f, chargeSpeed);
        chargeDuration = Mathf.Max(0.05f, chargeDuration);
        recoveryDuration = Mathf.Max(0f, recoveryDuration);
        chargeCooldown = Mathf.Max(0f, chargeCooldown);
    }
}

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(MonsterHealth))]
[RequireComponent(typeof(MonsterChase))]
public sealed partial class MonsterAimedChargeAttack : MonoBehaviour
{
    public enum ChargeState
    {
        Chasing,
        Aiming,
        Charging,
        Recovering
    }

    [Header("Activation")]
    [Tooltip("When enabled, this monster starts charging automatically in range.")]
    [SerializeField]
    private bool automaticActivation = true;

    [Header("Detection")]
    [SerializeField, Min(0.1f)]
    private float chargeRange = 4f;

    [Header("Aiming")]
    [SerializeField, Min(0f)]
    private float aimDuration = 0.65f;

    [Header("Charge")]
    [SerializeField, Min(0.1f)]
    private float chargeSpeed = 8f;

    [SerializeField, Min(0.05f)]
    private float chargeDuration = 0.75f;

    [Header("Recovery")]
    [SerializeField, Min(0f)]
    private float recoveryDuration = 0.65f;

    [SerializeField, Min(0f)]
    private float chargeCooldown = 1.5f;

    [Header("Target")]
    [SerializeField]
    private Transform target;

    private Rigidbody2D body;
    private MonsterHealth monsterHealth;
    private MonsterChase chase;
    private MonsterKnockback knockback;
    private MonsterSanityAppearance appearance;
    private SpriteRenderer spriteRenderer;
    private PlayerMental playerMental;
    private MonsterAimedChargeSettings overrideSettings;

    private ChargeState currentState;
    private Vector2 chargeDirection;
    private float stateEndTime;
    private float nextChargeTime;
    private bool hasDamagedThisCharge;
    private bool ownsChasePause;

    public ChargeState CurrentState => currentState;
    public Vector2 ChargeDirection => chargeDirection;
    public bool IsCharging =>
        currentState == ChargeState.Charging;
    public bool IsRunning =>
        currentState != ChargeState.Chasing;
    public bool AutomaticActivation =>
        automaticActivation;

    private float ChargeRange =>
        overrideSettings != null
            ? overrideSettings.ChargeRange
            : chargeRange;

    private float AimDuration =>
        overrideSettings != null
            ? overrideSettings.AimDuration
            : aimDuration;

    private float ChargeSpeed =>
        overrideSettings != null
            ? overrideSettings.ChargeSpeed
            : chargeSpeed;

    private float ChargeDuration =>
        overrideSettings != null
            ? overrideSettings.ChargeDuration
            : chargeDuration;

    private float RecoveryDuration =>
        overrideSettings != null
            ? overrideSettings.RecoveryDuration
            : recoveryDuration;

    private float ChargeCooldown =>
        overrideSettings != null
            ? overrideSettings.ChargeCooldown
            : chargeCooldown;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        monsterHealth = GetComponent<MonsterHealth>();
        chase = GetComponent<MonsterChase>();
        knockback = GetComponent<MonsterKnockback>();
        appearance =
            GetComponent<MonsterSanityAppearance>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        ResolveTarget();
        ResolvePlayerMental();
    }

    private void OnEnable()
    {
        ResetState();
    }

    private void OnDisable()
    {
        StopMovement();
        ReleaseChase();
    }
}
