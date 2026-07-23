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

    public bool HasAmmo => CurrentAmmo > 0;
    public bool IsFull => CurrentAmmo >= MaxAmmo;

    public event Action<int, int> OnAmmoChanged;
    public event Action OnAmmoEmpty;
    public event Action OnReloaded;

    private PlayerLevelStats levelStats;
    private bool isInitialized;

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
    }

    public void ResetAmmo()
    {
        InitializeIfNeeded();

        CurrentAmmo = MaxAmmo;
        NotifyAmmoChanged();
    }

    public bool TryUseAmmo(int amount = 1)
    {
        InitializeIfNeeded();

        if (amount <= 0)
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
        }

        return true;
    }

    public void Reload()
    {
        InitializeIfNeeded();

        if (IsFull)
        {
            return;
        }

        CurrentAmmo = MaxAmmo;

        NotifyAmmoChanged();
        OnReloaded?.Invoke();
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