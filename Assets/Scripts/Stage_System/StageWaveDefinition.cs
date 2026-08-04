using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

[Serializable]
public sealed class StageWaveDefinition
{
    [SerializeField]
    private string waveName = "Wave";

    [FormerlySerializedAs("startTime")]
    [SerializeField, Min(0.05f)]
    private float waveTime = 5f;

    [SerializeField, Min(0.05f)]
    private float spawnInterval = 1f;

    [SerializeField, Min(1)]
    private int maxAlive = 20;

    [SerializeField, Min(1)]
    private int spawnCountPerTick = 1;

    [SerializeField]
    private bool bossWave;

    [SerializeField]
    private List<MonsterSpawnEntry> monsters = new();

    public string WaveName => waveName;
    public float WaveTime => Mathf.Max(0.05f, waveTime);
    public float SpawnInterval => spawnInterval;
    public int MaxAlive => maxAlive;
    public int SpawnCountPerTick => spawnCountPerTick;
    public bool IsBossWave => bossWave;
    public IReadOnlyList<MonsterSpawnEntry> Monsters => monsters;

    public StageWaveDefinition(
        string waveName,
        float waveTime,
        float spawnInterval,
        int maxAlive,
        int spawnCountPerTick,
        bool bossWave,
        params MonsterSpawnEntry[] monsters
    )
    {
        this.waveName = waveName;
        this.waveTime = Mathf.Max(0.05f, waveTime);
        this.spawnInterval = Mathf.Max(0.05f, spawnInterval);
        this.maxAlive = Mathf.Max(1, maxAlive);
        this.spawnCountPerTick = Mathf.Max(1, spawnCountPerTick);
        this.bossWave = bossWave;
        this.monsters = new List<MonsterSpawnEntry>(monsters);
    }

    public GameObject ChoosePrefab(float roll)
    {
        float totalWeight = 0f;

        foreach (MonsterSpawnEntry entry in monsters)
        {
            if (entry?.Prefab != null)
            {
                totalWeight += entry.Weight;
            }
        }

        if (totalWeight <= 0f)
        {
            return null;
        }

        float targetWeight = Mathf.Clamp01(roll) * totalWeight;
        GameObject lastValidPrefab = null;

        foreach (MonsterSpawnEntry entry in monsters)
        {
            if (entry?.Prefab == null || entry.Weight <= 0f)
            {
                continue;
            }

            lastValidPrefab = entry.Prefab;
            targetWeight -= entry.Weight;

            if (targetWeight <= 0f)
            {
                return entry.Prefab;
            }
        }

        return lastValidPrefab;
    }
}
