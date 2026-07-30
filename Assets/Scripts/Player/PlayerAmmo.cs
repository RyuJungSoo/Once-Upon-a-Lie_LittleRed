using System;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerLevelStats))]
public class PlayerAmmo : MonoBehaviour
{
    [field: Header("Runtime Ammo")]
    [field: SerializeField]
    public int CurrentAmmo { get; private set; }
    [field: SerializeField]
    public int MaxAmmo { get; private set; }

    [Header("Reload")]
    [SerializeField, Min(0.01f)]
    private float reloadDuration = 1.5f;

    public bool HasAmmo => CurrentAmmo > 0;
    public bool IsFull => CurrentAmmo >= MaxAmmo;
    public bool IsReloading { get; private set; }
    private float ReloadDuration => Mathf.Max(0.01f, reloadDuration);
    public float ReloadProgress =>
        IsReloading
            ? Mathf.Clamp01(reloadElapsedTime / ReloadDuration)
            : 0f;

    public event Action<int, int> OnAmmoChanged;
    public event Action OnAmmoEmpty;
    public event Action OnReloadStarted;
    public event Action<float> OnReloadProgressChanged;
    public event Action OnReloaded;
    public event Action OnReloadCanceled;

    private PlayerLevelStats levelStats;
    private bool isInitialized;
    private float reloadElapsedTime;

    private void Awake()
    {
        levelStats = GetComponent<PlayerLevelStats>();
    }

    private void OnEnable()
    {
        if (levelStats == null)
        {
            levelStats = GetComponent<PlayerLevelStats>();
        }

        levelStats.OnStatsChanged += HandleStatsChanged;
    }

    private void Start()
    {
        InitializeIfNeeded();
        NotifyAmmoChanged();
    }

    private void OnDisable()
    {
        if (levelStats != null)
        {
            levelStats.OnStatsChanged -= HandleStatsChanged;
        }

        CancelReload();
    }

    private void Update()
    {
        AdvanceReload(Time.deltaTime);
    }

    public void ResetAmmo()
    {
        InitializeIfNeeded();

        CancelReload();
        CurrentAmmo = MaxAmmo;
        NotifyAmmoChanged();
    }

    public bool TryUseAmmo(int amount = 1)
    {
        InitializeIfNeeded();

        if (amount <= 0 || IsReloading)
        {
            return false;
        }

        if (CurrentAmmo < amount)
        {
            return false;
        }

        CurrentAmmo -= amount;
        NotifyAmmoChanged();

        if (CurrentAmmo <= 0)
        {
            OnAmmoEmpty?.Invoke();
            TryStartReload();
        }

        return true;
    }

    public void Reload()
    {
        TryStartReload();
    }

    public bool TryStartReload()
    {
        InitializeIfNeeded();

        if (IsReloading || IsFull)
        {
            return false;
        }

        IsReloading = true;
        reloadElapsedTime = 0f;

        OnReloadStarted?.Invoke();
        if(SoundManager.HasInstance)
            SoundManager.Instance.PlaySFX(ESFXType.Reload);

        if (UIManager.HasInstance)
        {
            UIManager.Instance.StartReloadGauge();
        }

        return true;
    }

    public void CancelReload()
    {
        if (!IsReloading)
        {
            return;
        }

        IsReloading = false;
        reloadElapsedTime = 0f;

        if (UIManager.HasInstance)
        {
            UIManager.Instance.EndReloadGauge();
        }

        OnReloadCanceled?.Invoke();
    }

    public void SetAmmo(int amount)
    {
        InitializeIfNeeded();

        int newAmmo = Mathf.Clamp(
            amount,
            0,
            MaxAmmo
        );

        if (newAmmo == CurrentAmmo)
        {
            return;
        }

        CurrentAmmo = newAmmo;

        if (IsFull)
        {
            CancelReload();
        }

        NotifyAmmoChanged();

        if (CurrentAmmo <= 0)
        {
            OnAmmoEmpty?.Invoke();
        }
    }

    private void InitializeIfNeeded()
    {
        if (isInitialized)
        {
            return;
        }

        MaxAmmo = Mathf.Max(
            1,
            levelStats.MagazineCapacity
        );

        CurrentAmmo = MaxAmmo;
        isInitialized = true;
    }

    private void HandleStatsChanged()
    {
        if (!isInitialized)
        {
            InitializeIfNeeded();
            NotifyAmmoChanged();
            return;
        }

        int previousMaxAmmo = MaxAmmo;

        MaxAmmo = Mathf.Max(
            1,
            levelStats.MagazineCapacity
        );

        int increasedCapacity =
            MaxAmmo - previousMaxAmmo;

        if (increasedCapacity > 0)
        {
            // 탄창이 증가한 만큼 현재 탄환도 같이 증가시킵니다.
            CurrentAmmo = Mathf.Min(
                MaxAmmo,
                CurrentAmmo + increasedCapacity
            );
        }
        else
        {
            CurrentAmmo = Mathf.Clamp(
                CurrentAmmo,
                0,
                MaxAmmo
            );
        }

        NotifyAmmoChanged();
    }

    private void AdvanceReload(float deltaTime)
    {
        if (!IsReloading || deltaTime <= 0f)
        {
            return;
        }

        reloadElapsedTime += deltaTime;

        float progress = ReloadProgress;
        OnReloadProgressChanged?.Invoke(progress);

        if (UIManager.HasInstance)
        {
            UIManager.Instance.UpdateReloadGauge(progress);
        }

        if (progress < 1f)
        {
            return;
        }

        IsReloading = false;
        reloadElapsedTime = 0f;
        CurrentAmmo = MaxAmmo;

        NotifyAmmoChanged();

        if (UIManager.HasInstance)
        {
            UIManager.Instance.EndReloadGauge();
        }

        OnReloaded?.Invoke();
    }

    private void NotifyAmmoChanged()
    {
        OnAmmoChanged?.Invoke(
            CurrentAmmo,
            MaxAmmo
        );

        if (UIManager.HasInstance)
        {
            UIManager.Instance.UpdateAmmo(
                CurrentAmmo,
                MaxAmmo
            );
        }
    }
}
