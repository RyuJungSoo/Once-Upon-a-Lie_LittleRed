using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
[RequireComponent(typeof(MonsterSpawner))]
public sealed class StageDirector : MonoBehaviour
{
    [SerializeField]
    private StageDefinition stageDefinition;

    private readonly HashSet<MonsterHealth> livingBosses = new();
    private List<StageWaveDefinition> orderedWaves = new();
    private MonsterSpawner spawner;
    private StageWaveDefinition activeWave;
    private int activeWaveIndex = -1;
    private float elapsedTime;
    private float spawnCountdown;
    private bool completed;

    public StageDefinition Definition => stageDefinition;
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
        if (stageDefinition == null)
        {
            Debug.LogError(
                $"{nameof(StageDirector)}: StageDefinition이 연결되지 않았습니다.",
                this
            );
            enabled = false;
            return;
        }

        orderedWaves = stageDefinition.CreateOrderedWaveList();

        if (orderedWaves.Count == 0)
        {
            Debug.LogError(
                $"{stageDefinition.name}에 웨이브가 없습니다.",
                stageDefinition
            );
            enabled = false;
            return;
        }

        ActivateDueWaves();
    }

    private void Update()
    {
        if (completed || !CanAdvanceStage())
        {
            return;
        }

        elapsedTime += Time.deltaTime;
        ActivateDueWaves();

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

    public void Configure(StageDefinition definition)
    {
        stageDefinition = definition;
    }

    private bool CanAdvanceStage()
    {
        return !GameManager.HasInstance ||
            GameManager.Instance.IsPlaying;
    }

    private void ActivateDueWaves()
    {
        int nextWaveIndex = activeWaveIndex + 1;

        while (
            nextWaveIndex < orderedWaves.Count &&
            orderedWaves[nextWaveIndex].StartTime <= elapsedTime
        )
        {
            ActivateWave(nextWaveIndex);
            nextWaveIndex++;
        }
    }

    private void ActivateWave(int waveIndex)
    {
        activeWaveIndex = waveIndex;
        activeWave = orderedWaves[waveIndex];
        spawnCountdown = 0f;
        WaveStarted?.Invoke(activeWave);

        Debug.Log(
            $"[{stageDefinition.StageName}] {activeWave.WaveName} 시작 " +
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
            if (stageDefinition.IsFinalStage)
            {
                GameManager.Instance.Victory();
            }
            else
            {
                GameManager.Instance.StageClear();
            }
        }

        if (
            stageDefinition.LoadNextSceneAutomatically &&
            !string.IsNullOrWhiteSpace(stageDefinition.NextSceneName)
        )
        {
            StartCoroutine(LoadNextSceneRoutine());
        }
    }

    private IEnumerator LoadNextSceneRoutine()
    {
        yield return new WaitForSecondsRealtime(
            stageDefinition.NextSceneDelay
        );

        if (
            GameManager.HasInstance &&
            !stageDefinition.IsFinalStage
        )
        {
            GameManager.Instance.StartNextStage();
        }

        SceneManager.LoadScene(stageDefinition.NextSceneName);
    }
}
