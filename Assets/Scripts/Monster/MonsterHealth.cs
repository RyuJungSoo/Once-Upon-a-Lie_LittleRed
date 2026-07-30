using System;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(BoxCollider2D))]
public sealed class MonsterHealth : MonoBehaviour
{
    [SerializeField] private MonsterStats stats;

    public MonsterStats Stats => stats;
    public int MaxHealth => stats != null ? stats.MaxHealth : 1;
    public float Damage => stats != null ? stats.Damage : 0f;
    public float MoveSpeed => stats != null ? stats.MoveSpeed : 0f;
    public int CurrentHealth { get; private set; }
    public bool IsDead { get; private set; }

    public event Action<int, int> HealthChanged;
    public event Action<MonsterHealth> Died;

    private void OnEnable()
    {
        CurrentHealth = MaxHealth;
        IsDead = false;
    }

    public void TakeDamage(int amount)
    {
        if (amount <= 0 || IsDead)
        {
            return;
        }

        int previousHealth = CurrentHealth;
        CurrentHealth = Mathf.Max(0, CurrentHealth - amount);
        HealthChanged?.Invoke(previousHealth, CurrentHealth);

        if (CurrentHealth > 0)
        {
            return;
        }

        IsDead = true;
        Died?.Invoke(this);
        DropExperienceCrystal();
        DropRecoveryItems();
        Destroy(gameObject);
    }

    private void DropExperienceCrystal()
    {
        if (stats == null ||
            stats.ExperienceCrystalDropPrefab == null)
        {
            return;
        }

        Instantiate(
            stats.ExperienceCrystalDropPrefab,
            transform.position,
            Quaternion.identity
        );
    }

    private void DropRecoveryItems()
    {
        if (stats == null)
        {
            return;
        }

        TryDropItem(
            stats.RedBerryDropPrefab,
            stats.RedBerryDropChancePercent,
            new Vector3(0.75f, 0f, 0f)
        );
        TryDropItem(
            stats.StarCandyDropPrefab,
            stats.StarCandyDropChancePercent,
            new Vector3(-0.375f, 0.65f, 0f)
        );
        TryDropItem(
            stats.PieDropPrefab,
            stats.PieDropChancePercent,
            new Vector3(-0.375f, -0.65f, 0f)
        );
    }

    private void TryDropItem(
        GameObject itemPrefab,
        float dropChancePercent,
        Vector3 spawnOffset
    )
    {
        if (itemPrefab == null ||
            dropChancePercent <= 0f)
        {
            return;
        }

        if (dropChancePercent < 100f &&
            UnityEngine.Random.value * 100f >=
            dropChancePercent)
        {
            return;
        }

        Instantiate(
            itemPrefab,
            transform.position + spawnOffset,
            Quaternion.identity
        );
    }
}
