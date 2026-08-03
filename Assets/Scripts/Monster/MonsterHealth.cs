using System;
using UnityEngine;

public enum MonsterDamageSource
{
    Unspecified,
    PlayerBullet,
    SelfDestruct
}

[DisallowMultipleComponent]
[RequireComponent(typeof(BoxCollider2D))]
public sealed class MonsterHealth : MonoBehaviour
{
    [SerializeField] private MonsterStats stats;

    private Func<MonsterHealth, bool> poolReleaseHandler;

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
        ResetForSpawn();
    }

    internal void ResetForSpawn()
    {
        CurrentHealth = MaxHealth;
        IsDead = false;
    }

    public void TakeDamage(int amount)
    {
        TakeDamage(
            amount,
            MonsterDamageSource.Unspecified
        );
    }

    public void TakeDamage(
        int amount,
        MonsterDamageSource damageSource
    )
    {
        if (amount <= 0 || IsDead)
        {
            return;
        }

        int previousHealth = CurrentHealth;
        CurrentHealth = Mathf.Max(0, CurrentHealth - amount);
        bool wasKilled = CurrentHealth == 0;

        if (wasKilled)
        {
            IsDead = true;
            DropDeathRewards(damageSource);
        }

        HealthChanged?.Invoke(previousHealth, CurrentHealth);

        if (wasKilled)
        {
            Died?.Invoke(this);

            if (poolReleaseHandler == null ||
                !poolReleaseHandler(this))
            {
                Destroy(gameObject);
            }
        }
    }

    internal void SetPoolReleaseHandler(
        Func<MonsterHealth, bool> releaseHandler
    )
    {
        poolReleaseHandler = releaseHandler;
    }

    private void DropDeathRewards(
        MonsterDamageSource damageSource
    )
    {
        DropExperienceCrystal();

        if (damageSource == MonsterDamageSource.PlayerBullet)
        {
            DropRecoveryItems();
        }
    }

    private void DropExperienceCrystal()
    {
        if (stats == null ||
            stats.ExperienceCrystalDropPrefab == null)
        {
            return;
        }

        SpawnDrop(
            stats.ExperienceCrystalDropPrefab,
            transform.position
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

        SpawnDrop(
            itemPrefab,
            transform.position + spawnOffset
        );
    }

    private GameObject SpawnDrop(
        GameObject itemPrefab,
        Vector3 position
    )
    {
        GameObject drop = ItemPool.Spawn(
            itemPrefab,
            position,
            Quaternion.identity,
            gameObject.scene
        );
        ItemRenderOrder.Assign(drop);

        return drop;
    }
}
