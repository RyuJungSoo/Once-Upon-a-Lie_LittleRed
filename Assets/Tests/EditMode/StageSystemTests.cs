using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public sealed class StageSystemTests
{
    private static readonly Type SpawnEntryType =
        Type.GetType("MonsterSpawnEntry, Assembly-CSharp");
    private static readonly Type WaveType =
        Type.GetType("StageWaveDefinition, Assembly-CSharp");
    private static readonly Type SpawnerType =
        Type.GetType("MonsterSpawner, Assembly-CSharp");
    private static readonly Type SpawnPointType =
        Type.GetType("MonsterSpawnPoint, Assembly-CSharp");

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
