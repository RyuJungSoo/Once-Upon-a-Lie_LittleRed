using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class MonsterSpawner : MonoBehaviour
{
    [SerializeField]
    private Transform spawnPointRoot;

    private readonly List<MonsterSpawnPoint> spawnPoints = new();
    private readonly List<MonsterHealth> aliveMonsters = new();

    public int SpawnPointCount
    {
        get
        {
            RefreshSpawnPoints();
            return spawnPoints.Count;
        }
    }

    public int AliveCount
    {
        get
        {
            aliveMonsters.RemoveAll(monster => monster == null);
            return aliveMonsters.Count;
        }
    }

    private void Awake()
    {
        if (spawnPointRoot == null)
        {
            spawnPointRoot = transform;
        }

        RefreshSpawnPoints();
    }

    public void RefreshSpawnPoints()
    {
        spawnPoints.Clear();

        Transform root = spawnPointRoot != null
            ? spawnPointRoot
            : transform;

        root.GetComponentsInChildren(true, spawnPoints);
        spawnPoints.RemoveAll(point => point == null);
    }

    public MonsterHealth SpawnRandom(GameObject prefab)
    {
        if (prefab == null)
        {
            Debug.LogWarning(
                $"{nameof(MonsterSpawner)}: 스폰할 프리팹이 비어 있습니다.",
                this
            );
            return null;
        }

        List<MonsterSpawnPoint> activePoints = spawnPoints.FindAll(
            point => point != null && point.gameObject.activeInHierarchy
        );

        if (activePoints.Count == 0)
        {
            RefreshSpawnPoints();
            activePoints = spawnPoints.FindAll(
                point => point != null && point.gameObject.activeInHierarchy
            );
        }

        if (activePoints.Count == 0)
        {
            Debug.LogError(
                $"{nameof(MonsterSpawner)}: 활성화된 SpawnPoint가 없습니다.",
                this
            );
            return null;
        }

        MonsterSpawnPoint spawnPoint = activePoints[
            Random.Range(0, activePoints.Count)
        ];
        GameObject monsterObject = Instantiate(
            prefab,
            spawnPoint.transform.position,
            Quaternion.identity
        );
        MonsterHealth health = monsterObject.GetComponent<MonsterHealth>();

        if (health == null)
        {
            Debug.LogError(
                $"{prefab.name} 프리팹에 {nameof(MonsterHealth)}가 없습니다.",
                monsterObject
            );
            Destroy(monsterObject);
            return null;
        }

        aliveMonsters.Add(health);
        return health;
    }
}
