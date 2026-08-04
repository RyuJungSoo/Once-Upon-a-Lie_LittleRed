using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(MonsterSpawner))]
public sealed class StageDirector : MonoBehaviour
{
    [Header("Stage")]
    [SerializeField]
    private string stageName = "Stage";

    [SerializeField]
    private bool finalStage;

    [Header("Waves")]
    [SerializeField]
    private List<StageWaveDefinition> waves = new();

    private readonly HashSet<MonsterHealth> livingBosses = new();
    private List<StageWaveDefinition> runtimeWaves = new();
    private MonsterSpawner spawner;
    private StageWaveDefinition activeWave;
    private int activeWaveIndex = -1;
    private float elapsedTime;
    private float waveElapsedTime;
    private float spawnCountdown;
    private bool completed;

    public string StageName => stageName;
    public bool IsFinalStage => finalStage;
    public IReadOnlyList<StageWaveDefinition> Waves => waves;
    public StageWaveDefinition ActiveWave => activeWave;
    public int ActiveWaveIndex => activeWaveIndex;
    public float ElapsedTime => elapsedTime;
    public bool IsCompleted => completed;

    public event Action<StageWaveDefinition> WaveStarted;
    public event Action StageCompleted;

    private void Awake()
    {
        spawner = GetComponent<MonsterSpawner>();
    }

    private void Start()
    {
        runtimeWaves = CreateWaveList();

        if (runtimeWaves.Count == 0)
        {
            Debug.LogError(
                $"{stageName}에 웨이브가 없습니다.",
                this
            );
            enabled = false;
            return;
        }

        ActivateWave(0);
    }

    private void Update()
    {
        if (completed || !CanAdvanceStage())
        {
            return;
        }

        AdvanceWaveTimer(Time.deltaTime);

        if (activeWave == null || activeWave.IsBossWave)
        {
            return;
        }

        spawnCountdown -= Time.deltaTime;

        if (spawnCountdown <= 0f)
        {
            SpawnRegularTick();
            spawnCountdown = activeWave.SpawnInterval;
        }
    }

    private List<StageWaveDefinition> CreateWaveList()
    {
        List<StageWaveDefinition> configuredWaves = new(waves);
        configuredWaves.RemoveAll(wave => wave == null);
        return configuredWaves;
    }

    private bool CanAdvanceStage()
    {
        return !GameManager.HasInstance ||
            GameManager.Instance.IsPlaying;
    }

    private void AdvanceWaveTimer(float deltaTime)
    {
        float safeDeltaTime = Mathf.Max(0f, deltaTime);
        elapsedTime += safeDeltaTime;
        waveElapsedTime += safeDeltaTime;

        while (
            activeWaveIndex + 1 < runtimeWaves.Count &&
            waveElapsedTime >= activeWave.WaveTime
        )
        {
            waveElapsedTime -= activeWave.WaveTime;
            ActivateWave(activeWaveIndex + 1);
        }
    }

    private void ActivateWave(int waveIndex)
    {
        activeWaveIndex = waveIndex;
        activeWave = runtimeWaves[waveIndex];
        spawnCountdown = 0f;
        WaveStarted?.Invoke(activeWave);

        Debug.Log(
            $"[{stageName}] {activeWave.WaveName} 시작 " +
            $"({elapsedTime:0.0}초)",
            this
        );

        if (activeWave.IsBossWave)
        {
            SpawnBossWave();
        }
    }

    private void SpawnRegularTick()
    {
        int availableSlots = activeWave.MaxAlive - spawner.AliveCount;
        int spawnCount = Mathf.Min(
            activeWave.SpawnCountPerTick,
            availableSlots
        );

        for (int index = 0; index < spawnCount; index++)
        {
            GameObject prefab = activeWave.ChoosePrefab(
                UnityEngine.Random.value
            );
            spawner.SpawnRandom(prefab);
        }
    }

    private void SpawnBossWave()
    {
        livingBosses.Clear();

        for (
            int index = 0;
            index < activeWave.SpawnCountPerTick;
            index++
        )
        {
            GameObject prefab = activeWave.ChoosePrefab(
                UnityEngine.Random.value
            );
            MonsterHealth boss = spawner.SpawnRandom(prefab);

            if (boss == null || !livingBosses.Add(boss))
            {
                continue;
            }

            boss.Died += OnBossDied;
        }

        if (livingBosses.Count == 0)
        {
            Debug.LogError(
                $"{activeWave.WaveName}에서 보스를 스폰하지 못했습니다.",
                this
            );
        }
    }

    private void OnBossDied(MonsterHealth boss)
    {
        boss.Died -= OnBossDied;
        livingBosses.Remove(boss);

        if (livingBosses.Count == 0)
        {
            CompleteStage();
        }
    }

    private void CompleteStage()
    {
        if (completed)
        {
            return;
        }

        completed = true;
        StageCompleted?.Invoke();

        if (GameManager.HasInstance)
        {
            if (finalStage)
            {
                GameManager.Instance.Victory();
            }
            else
            {
                GameManager.Instance.StageClear();
            }
        }
    }
}
