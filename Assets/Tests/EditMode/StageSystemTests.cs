using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public sealed class StageSystemTests
{
    [TearDown]
    public void TearDown()
    {
        ItemPoolTestCleanup.DestroyPoolObjects();
    }

    private static readonly Type SpawnEntryType =
        Type.GetType("MonsterSpawnEntry, Assembly-CSharp");
    private static readonly Type WaveType =
        Type.GetType("StageWaveDefinition, Assembly-CSharp");
    private static readonly Type DirectorType =
        Type.GetType("StageDirector, Assembly-CSharp");
    private static readonly Type SpawnerType =
        Type.GetType("MonsterSpawner, Assembly-CSharp");
    private static readonly Type SpawnPointType =
        Type.GetType("MonsterSpawnPoint, Assembly-CSharp");
    private static readonly Type HealthType =
        Type.GetType("MonsterHealth, Assembly-CSharp");

    [Test]
    public void ChoosePrefab_UsesConfiguredWeights()
    {
        GameObject common = new("Common");
        GameObject rare = new("Rare");
        object commonEntry = CreateSpawnEntry(common, 3f);
        object rareEntry = CreateSpawnEntry(rare, 1f);
        Array entries = Array.CreateInstance(SpawnEntryType, 2);
        entries.SetValue(commonEntry, 0);
        entries.SetValue(rareEntry, 1);
        object wave = Activator.CreateInstance(
            WaveType,
            "Weighted",
            0f,
            1f,
            10,
            1,
            false,
            entries
        );
        MethodInfo choosePrefab = WaveType.GetMethod("ChoosePrefab");

        Assert.That(
            choosePrefab.Invoke(wave, new object[] { 0.1f }),
            Is.SameAs(common)
        );
        Assert.That(
            choosePrefab.Invoke(wave, new object[] { 0.9f }),
            Is.SameAs(rare)
        );

        UnityEngine.Object.DestroyImmediate(common);
        UnityEngine.Object.DestroyImmediate(rare);
    }

    [Test]
    public void StageDirector_AdvancesWavesAfterEachWaveTime()
    {
        Array noEntries = Array.CreateInstance(SpawnEntryType, 0);
        object firstWave = Activator.CreateInstance(
            WaveType,
            "First",
            10f,
            1f,
            10,
            1,
            false,
            noEntries
        );
        object secondWave = Activator.CreateInstance(
            WaveType,
            "Second",
            5f,
            1f,
            10,
            1,
            false,
            noEntries
        );
        IList waves = (IList)Activator.CreateInstance(
            typeof(List<>).MakeGenericType(WaveType)
        );
        waves.Add(firstWave);
        waves.Add(secondWave);

        GameObject root = new("StageSystem");

        try
        {
            Component director = root.AddComponent(DirectorType);
            DirectorType.GetField(
                "waves",
                BindingFlags.NonPublic | BindingFlags.Instance
            ).SetValue(director, waves);

            MethodInfo start = DirectorType.GetMethod(
                "Start",
                BindingFlags.NonPublic | BindingFlags.Instance
            );
            MethodInfo advanceWaveTimer = DirectorType.GetMethod(
                "AdvanceWaveTimer",
                BindingFlags.NonPublic | BindingFlags.Instance
            );
            PropertyInfo activeWaveIndex =
                DirectorType.GetProperty("ActiveWaveIndex");
            PropertyInfo waveTime =
                WaveType.GetProperty("WaveTime");

            Assert.That(waveTime, Is.Not.Null);
            Assert.That(
                waveTime.GetValue(firstWave),
                Is.EqualTo(10f)
            );
            Assert.That(advanceWaveTimer, Is.Not.Null);

            start.Invoke(director, null);
            Assert.That(
                activeWaveIndex.GetValue(director),
                Is.EqualTo(0)
            );

            advanceWaveTimer.Invoke(
                director,
                new object[] { 9.99f }
            );
            Assert.That(
                activeWaveIndex.GetValue(director),
                Is.EqualTo(0)
            );

            advanceWaveTimer.Invoke(
                director,
                new object[] { 0.01f }
            );
            Assert.That(
                activeWaveIndex.GetValue(director),
                Is.EqualTo(1)
            );

            advanceWaveTimer.Invoke(
                director,
                new object[] { 4.99f }
            );
            Assert.That(
                activeWaveIndex.GetValue(director),
                Is.EqualTo(1)
            );
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    [Test]
    public void MonsterSpawner_DiscoversAddedAndRemovedSpawnPoints()
    {
        GameObject root = new("StageSystem");
        Component spawner = root.AddComponent(SpawnerType);
        GameObject first = CreateSpawnPoint(root.transform, "SpawnPoint1");
        GameObject second = CreateSpawnPoint(root.transform, "SpawnPoint2");
        PropertyInfo spawnPointCount =
            SpawnerType.GetProperty("SpawnPointCount");

        Assert.That(spawnPointCount.GetValue(spawner), Is.EqualTo(2));

        UnityEngine.Object.DestroyImmediate(second);

        Assert.That(spawnPointCount.GetValue(spawner), Is.EqualTo(1));

        UnityEngine.Object.DestroyImmediate(first);
        UnityEngine.Object.DestroyImmediate(root);
    }

    [Test]
    public void MonsterSpawner_ReusesDefeatedMonsterFromSamePrefab()
    {
        GameObject root = new("StageSystem");
        Component spawner = root.AddComponent(SpawnerType);
        CreateSpawnPoint(root.transform, "SpawnPoint");
        GameObject prefab = new("MonsterPrefab");
        prefab.AddComponent(HealthType);
        MethodInfo spawnRandom =
            SpawnerType.GetMethod("SpawnRandom");
        PropertyInfo aliveCount =
            SpawnerType.GetProperty("AliveCount");
        MethodInfo takeDamage = HealthType.GetMethod(
            "TakeDamage",
            new[] { typeof(int) }
        );

        Component first = (Component)spawnRandom.Invoke(
            spawner,
            new object[] { prefab }
        );

        Assert.That(first, Is.Not.Null);
        Assert.That(
            aliveCount.GetValue(spawner),
            Is.EqualTo(1)
        );

        takeDamage.Invoke(first, new object[] { 1 });

        Assert.That(first.gameObject.activeSelf, Is.False);
        Assert.That(
            aliveCount.GetValue(spawner),
            Is.EqualTo(0)
        );

        Component second = (Component)spawnRandom.Invoke(
            spawner,
            new object[] { prefab }
        );

        Assert.That(second, Is.SameAs(first));
        Assert.That(second.gameObject.activeSelf, Is.True);
        Assert.That(
            HealthType.GetProperty("CurrentHealth").GetValue(second),
            Is.EqualTo(
                HealthType.GetProperty("MaxHealth").GetValue(second)
            )
        );
        Assert.That(
            aliveCount.GetValue(spawner),
            Is.EqualTo(1)
        );

        UnityEngine.Object.DestroyImmediate(second.gameObject);
        UnityEngine.Object.DestroyImmediate(prefab);
        UnityEngine.Object.DestroyImmediate(root);
    }

    [Test]
    public void StageSceneTransfer_CollectsDistinctPlayerAndCameraRoots()
    {
        Type transferType =
            Type.GetType("StageSceneTransfer, Assembly-CSharp");

        Assert.That(transferType, Is.Not.Null);

        GameObject playerRoot = new("PlayerRoot");
        GameObject player = new("Player");
        player.transform.SetParent(playerRoot.transform);

        GameObject cameraRoot = new("CameraRoot");
        GameObject cameraObject = new("Main Camera");
        cameraObject.transform.SetParent(cameraRoot.transform);
        cameraObject.AddComponent<Camera>();

        GameObject virtualCameraRoot = new("VirtualCameraRoot");
        GameObject virtualCamera = new("CinemachineCamera");
        virtualCamera.transform.SetParent(
            virtualCameraRoot.transform
        );

        MethodInfo collectRoots = transferType.GetMethod(
            "CollectTransferRoots",
            BindingFlags.NonPublic | BindingFlags.Static
        );

        Assert.That(collectRoots, Is.Not.Null);

        IEnumerable roots = (IEnumerable)collectRoots.Invoke(
            null,
            new object[]
            {
                player,
                cameraObject,
                virtualCamera
            }
        );

        CollectionAssert.AreEquivalent(
            new[]
            {
                playerRoot,
                cameraRoot,
                virtualCameraRoot
            },
            roots
        );

        UnityEngine.Object.DestroyImmediate(playerRoot);
        UnityEngine.Object.DestroyImmediate(cameraRoot);
        UnityEngine.Object.DestroyImmediate(virtualCameraRoot);
    }

    private static GameObject CreateSpawnPoint(
        Transform parent,
        string name
    )
    {
        GameObject point = new(name);
        point.transform.SetParent(parent);
        point.AddComponent(SpawnPointType);
        return point;
    }

    private static object CreateSpawnEntry(
        GameObject prefab,
        float weight
    )
    {
        return Activator.CreateInstance(
            SpawnEntryType,
            prefab,
            weight
        );
    }
}
