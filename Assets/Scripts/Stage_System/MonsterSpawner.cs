using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class MonsterSpawner : MonoBehaviour
{
    [SerializeField]
    private Transform spawnPointRoot;

    private readonly List<MonsterSpawnPoint> spawnPoints = new();
    private readonly List<MonsterHealth> aliveMonsters = new();
    private readonly Dictionary<GameObject, Stack<MonsterHealth>> pools =
        new();
    private readonly Dictionary<MonsterHealth, GameObject> prefabByMonster =
        new();

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
            aliveMonsters.RemoveAll(
                monster =>
                    monster == null ||
                    !monster.gameObject.activeInHierarchy
            );
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
        MonsterHealth health = TakeFromPool(prefab);

        if (health == null)
        {
            GameObject monsterObject = Instantiate(
                prefab,
                spawnPoint.transform.position,
                Quaternion.identity
            );
            health = monsterObject.GetComponent<MonsterHealth>();

            if (health == null)
            {
                Debug.LogError(
                    $"{prefab.name} 프리팹에 " +
                    $"{nameof(MonsterHealth)}가 없습니다.",
                    monsterObject
                );
                Destroy(monsterObject);
                return null;
            }

            prefabByMonster.Add(health, prefab);
        }

        health.transform.SetPositionAndRotation(
            spawnPoint.transform.position,
            Quaternion.identity
        );
        health.SetPoolReleaseHandler(ReturnToPool);
        health.gameObject.SetActive(true);
        health.ResetForSpawn();
        aliveMonsters.Add(health);
        return health;
    }

    private MonsterHealth TakeFromPool(GameObject prefab)
    {
        if (!pools.TryGetValue(prefab, out Stack<MonsterHealth> pool))
        {
            return null;
        }

        while (pool.Count > 0)
        {
            MonsterHealth health = pool.Pop();

            if (health != null)
            {
                return health;
            }
        }

        return null;
    }

    private bool ReturnToPool(MonsterHealth health)
    {
        if (health == null ||
            !prefabByMonster.TryGetValue(
                health,
                out GameObject prefab
            ))
        {
            return false;
        }

        aliveMonsters.Remove(health);
        ResetPhysics(health);
        health.gameObject.SetActive(false);

        if (!pools.TryGetValue(prefab, out Stack<MonsterHealth> pool))
        {
            pool = new Stack<MonsterHealth>();
            pools.Add(prefab, pool);
        }

        pool.Push(health);
        return true;
    }

    private static void ResetPhysics(MonsterHealth health)
    {
        Rigidbody2D[] bodies =
            health.GetComponentsInChildren<Rigidbody2D>();

        foreach (Rigidbody2D body in bodies)
        {
            body.linearVelocity = Vector2.zero;
            body.angularVelocity = 0f;
        }
    }

    private void OnDestroy()
    {
        foreach (MonsterHealth health in prefabByMonster.Keys)
        {
            if (health != null)
            {
                health.SetPoolReleaseHandler(null);
            }
        }

        aliveMonsters.Clear();
        pools.Clear();
        prefabByMonster.Clear();
    }
}
