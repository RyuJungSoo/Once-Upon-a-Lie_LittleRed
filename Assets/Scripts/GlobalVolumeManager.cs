using System;
using UnityEngine;
using UnityEngine.Rendering;

[DisallowMultipleComponent]
public sealed class GlobalVolumeManager :
    Singleton<GlobalVolumeManager>
{
    [Header("Mental Volume Source")]
    [SerializeField] private Volume globalVolume;
    [SerializeField] private PlayerMental playerMental;

    private MentalVolumeEffect[] mentalEffects =
        Array.Empty<MentalVolumeEffect>();
    private PlayerMental subscribedPlayerMental;
    private VolumeProfile runtimeProfile;

    protected override void Awake()
    {
        base.Awake();

        if (!IsSingletonInstance)
        {
            return;
        }

        if(GameManager.HasInstance)
            playerMental = GameManager.Instance.gameObject.GetComponent<PlayerMental>();
        InitializeEffects();
    }

    private void OnEnable()
    {
        if (!IsSingletonInstance)
        {
            return;
        }

        if (runtimeProfile == null)
        {
            InitializeEffects();
        }

        BindPlayerMental();
    }

    private void Start()
    {
        if (!IsSingletonInstance)
        {
            return;
        }

        if (runtimeProfile == null)
        {
            InitializeEffects();
        }

        if (subscribedPlayerMental == null)
        {
            BindPlayerMental();
        }

        ApplyCurrentMental();
    }

    private void OnDisable()
    {
        UnbindPlayerMental();
    }

    private void InitializeEffects()
    {
        if (globalVolume == null)
        {
            Debug.LogWarning(
                $"{nameof(GlobalVolumeManager)}에 " +
                "Global Volume이 연결되지 않았어.",
                this
            );
            return;
        }

        runtimeProfile = globalVolume.profile;

        if (runtimeProfile == null)
        {
            Debug.LogWarning(
                "Global Volume에 Volume Profile이 없어.",
                globalVolume
            );
            return;
        }

        mentalEffects =
            GetComponents<MentalVolumeEffect>();

        if (mentalEffects.Length == 0)
        {
            Debug.LogWarning(
                $"{nameof(GlobalVolumeManager)}에 정신력 기반 " +
                "Volume Effect 컴포넌트가 없어.",
                this
            );
            return;
        }

        foreach (MentalVolumeEffect effect in mentalEffects)
        {
            effect.Initialize(runtimeProfile);
        }
    }

    private void BindPlayerMental()
    {
        ResolvePlayerMental();

        if (playerMental == null ||
            subscribedPlayerMental == playerMental)
        {
            return;
        }

        UnbindPlayerMental();

        playerMental.OnMentalChanged +=
            HandleMentalChanged;
        subscribedPlayerMental = playerMental;
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
            playerMental =
                FindFirstObjectByType<PlayerMental>();
        }
    }

    private void UnbindPlayerMental()
    {
        if (subscribedPlayerMental == null)
        {
            return;
        }

        subscribedPlayerMental.OnMentalChanged -=
            HandleMentalChanged;
        subscribedPlayerMental = null;
    }

    private void ApplyCurrentMental()
    {
        if (subscribedPlayerMental == null ||
            subscribedPlayerMental.MaxMental <= 0f)
        {
            ApplyEffects(0f);
            return;
        }

        HandleMentalChanged(
            subscribedPlayerMental.CurrentMental,
            subscribedPlayerMental.MaxMental
        );
    }

    private void HandleMentalChanged(
        float currentMental,
        float maxMental
    )
    {
        float safeMaxMental = Mathf.Max(1f, maxMental);
        float mentalRatio = Mathf.Clamp01(
            currentMental / safeMaxMental
        );

        ApplyEffects(1f - mentalRatio);
    }

    private void ApplyEffects(float dangerRatio)
    {
        if (runtimeProfile == null)
        {
            InitializeEffects();
        }

        foreach (MentalVolumeEffect effect in mentalEffects)
        {
            if (effect != null && effect.isActiveAndEnabled)
            {
                effect.Apply(dangerRatio);
            }
        }
    }
}
