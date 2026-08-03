using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public sealed class ItemPoolPlayModeTests
{
    private static readonly Type ItemPoolType =
        Type.GetType("ItemPool, Assembly-CSharp");

    private static readonly Type ExperiencePickupType =
        Type.GetType(
            "ExperienceCrystalPickup, Assembly-CSharp"
        );

    private static readonly Type MentalRecoveryItemType =
        Type.GetType(
            "MentalRecoveryItem, Assembly-CSharp"
        );

    private static readonly Type PlayerExperienceType =
        Type.GetType("PlayerExperience, Assembly-CSharp");

    private static readonly Type PlayerMentalType =
        Type.GetType("PlayerMental, Assembly-CSharp");

    private static readonly Type PlayerLevelStatsType =
        Type.GetType("PlayerLevelStats, Assembly-CSharp");

    private GameObject firstPrefab;
    private GameObject secondPrefab;
    private readonly List<GameObject> spawnedItems =
        new List<GameObject>();
    private readonly List<Scene> createdScenes =
        new List<Scene>();
    private readonly List<GameObject> supportObjects =
        new List<GameObject>();

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        foreach (GameObject item in spawnedItems)
        {
            UnityEngine.Object.DestroyImmediate(item);
        }

        UnityEngine.Object.DestroyImmediate(firstPrefab);
        UnityEngine.Object.DestroyImmediate(secondPrefab);

        foreach (GameObject supportObject in supportObjects)
        {
            UnityEngine.Object.DestroyImmediate(supportObject);
        }

        foreach (GameObject gameObject in
                 Resources.FindObjectsOfTypeAll<GameObject>())
        {
            if (gameObject.name == "[ItemPool]")
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        foreach (Scene scene in createdScenes)
        {
            if (scene.IsValid() && scene.isLoaded)
            {
                yield return SceneManager.UnloadSceneAsync(
                    scene
                );
            }
        }
    }

    [Test]
    public void ReleasedItemIsReusedForTheSamePrefab()
    {
        Assert.That(ItemPoolType, Is.Not.Null);

        firstPrefab = new GameObject("First Item Prefab");
        firstPrefab.SetActive(false);

        GameObject firstSpawn = Spawn(
            firstPrefab,
            new Vector3(1f, 2f, 0f)
        );

        Assert.That(firstSpawn.activeSelf, Is.True);
        Assert.That(Release(firstSpawn), Is.True);
        Assert.That(firstSpawn.activeSelf, Is.False);

        GameObject secondSpawn = Spawn(
            firstPrefab,
            new Vector3(4f, 5f, 0f)
        );

        Assert.That(secondSpawn, Is.SameAs(firstSpawn));
        Assert.That(
            secondSpawn.transform.position,
            Is.EqualTo(new Vector3(4f, 5f, 0f))
        );
    }

    [Test]
    public void DifferentPrefabsUseDifferentPools()
    {
        Assert.That(ItemPoolType, Is.Not.Null);

        firstPrefab = new GameObject("First Item Prefab");
        secondPrefab = new GameObject("Second Item Prefab");
        firstPrefab.SetActive(false);
        secondPrefab.SetActive(false);

        GameObject firstSpawn = Spawn(
            firstPrefab,
            Vector3.zero
        );
        Assert.That(Release(firstSpawn), Is.True);

        GameObject secondSpawn = Spawn(
            secondPrefab,
            Vector3.one
        );

        Assert.That(secondSpawn, Is.Not.SameAs(firstSpawn));
    }

    [Test]
    public void ExperiencePickupCanBeCollectedAfterReuse()
    {
        Assert.That(ExperiencePickupType, Is.Not.Null);
        Assert.That(PlayerExperienceType, Is.Not.Null);

        GameObject player = new GameObject(
            "Test Experience Player"
        );
        supportObjects.Add(player);
        Component playerExperience =
            player.AddComponent(PlayerExperienceType);

        firstPrefab = new GameObject(
            "Experience Item Prefab"
        );
        firstPrefab.SetActive(false);
        firstPrefab.AddComponent<BoxCollider2D>();
        firstPrefab.AddComponent(ExperiencePickupType);

        GameObject firstSpawn = Spawn(
            firstPrefab,
            Vector3.zero
        );
        Component firstPickup =
            firstSpawn.GetComponent(
                ExperiencePickupType
            );

        Assert.That(
            (bool)Invoke(
                firstPickup,
                "TryCollect",
                playerExperience
            ),
            Is.True
        );
        Assert.That(Release(firstSpawn), Is.True);

        GameObject secondSpawn = Spawn(
            firstPrefab,
            Vector3.one
        );
        Component secondPickup =
            secondSpawn.GetComponent(
                ExperiencePickupType
            );

        Assert.That(secondSpawn, Is.SameAs(firstSpawn));
        Assert.That(
            (bool)Invoke(
                secondPickup,
                "TryCollect",
                playerExperience
            ),
            Is.True
        );
    }

    [Test]
    public void MentalRecoveryItemCanBeAppliedAfterReuse()
    {
        Assert.That(MentalRecoveryItemType, Is.Not.Null);
        Assert.That(PlayerMentalType, Is.Not.Null);
        Assert.That(PlayerLevelStatsType, Is.Not.Null);

        GameObject player = new GameObject(
            "Test Mental Player"
        );
        player.SetActive(false);
        supportObjects.Add(player);

        Component playerMental =
            player.AddComponent(PlayerMentalType);
        Component levelStats = player.GetComponent(
            PlayerLevelStatsType
        );
        Invoke(levelStats, "RecalculateStats", 1);
        player.SetActive(true);
        Invoke(playerMental, "ResetMental");

        firstPrefab = new GameObject(
            "Mental Recovery Item Prefab"
        );
        firstPrefab.SetActive(false);
        firstPrefab.AddComponent<BoxCollider2D>();
        Component prefabItem =
            firstPrefab.AddComponent(
                MentalRecoveryItemType
            );
        SetField(
            prefabItem,
            "mentalRestoreRatio",
            0.15f
        );

        GameObject firstSpawn = Spawn(
            firstPrefab,
            Vector3.zero
        );
        Component firstItem =
            firstSpawn.GetComponent(
                MentalRecoveryItemType
            );

        Assert.That(
            (bool)Invoke(
                firstItem,
                "TryApply",
                playerMental
            ),
            Is.True
        );
        Assert.That(Release(firstSpawn), Is.True);

        GameObject secondSpawn = Spawn(
            firstPrefab,
            Vector3.one
        );
        Component secondItem =
            secondSpawn.GetComponent(
                MentalRecoveryItemType
            );

        Assert.That(secondSpawn, Is.SameAs(firstSpawn));
        Assert.That(
            (bool)Invoke(
                secondItem,
                "TryApply",
                playerMental
            ),
            Is.True
        );
    }

    [UnityTest]
    public IEnumerator ReleasedItemSurvivesSceneUnloadAndMovesToDestinationScene()
    {
        Assert.That(ItemPoolType, Is.Not.Null);

        firstPrefab = new GameObject("Scene Item Prefab");
        firstPrefab.SetActive(false);

        Scene firstScene = SceneManager.CreateScene(
            "Item Pool First Test Scene"
        );
        createdScenes.Add(firstScene);

        GameObject firstSpawn = Spawn(
            firstPrefab,
            Vector3.zero,
            firstScene
        );
        GameObject activeSceneItem = Spawn(
            firstPrefab,
            Vector3.right,
            firstScene
        );

        Assert.That(firstSpawn.scene, Is.EqualTo(firstScene));
        Assert.That(Release(firstSpawn), Is.True);

        yield return SceneManager.UnloadSceneAsync(firstScene);

        Assert.That(activeSceneItem == null, Is.True);
        Assert.That(GetManagedInstanceCount(), Is.EqualTo(1));

        Scene secondScene = SceneManager.CreateScene(
            "Item Pool Second Test Scene"
        );
        createdScenes.Add(secondScene);

        GameObject secondSpawn = Spawn(
            firstPrefab,
            Vector3.one,
            secondScene
        );

        Assert.That(secondSpawn, Is.SameAs(firstSpawn));
        Assert.That(secondSpawn.scene, Is.EqualTo(secondScene));
    }

    private static int GetManagedInstanceCount()
    {
        GameObject poolObject = Array.Find(
            Resources.FindObjectsOfTypeAll<GameObject>(),
            gameObject => gameObject.name == "[ItemPool]"
        );
        Assert.That(poolObject, Is.Not.Null);

        Component pool = poolObject.GetComponent(
            ItemPoolType
        );
        FieldInfo field = ItemPoolType.GetField(
            "prefabByInstance",
            BindingFlags.Instance |
            BindingFlags.NonPublic
        );

        Assert.That(field, Is.Not.Null);
        IDictionary instances =
            (IDictionary)field.GetValue(pool);
        return instances.Count;
    }

    private GameObject Spawn(
        GameObject prefab,
        Vector3 position
    )
    {
        MethodInfo method = ItemPoolType?.GetMethod(
            "Spawn",
            BindingFlags.Static |
            BindingFlags.Public,
            null,
            new[]
            {
                typeof(GameObject),
                typeof(Vector3),
                typeof(Quaternion)
            },
            null
        );

        Assert.That(method, Is.Not.Null);
        GameObject item = (GameObject)method.Invoke(
            null,
            new object[]
            {
                prefab,
                position,
                Quaternion.identity
            }
        );
        spawnedItems.Add(item);
        return item;
    }

    private GameObject Spawn(
        GameObject prefab,
        Vector3 position,
        Scene destinationScene
    )
    {
        MethodInfo method = ItemPoolType?.GetMethod(
            "Spawn",
            BindingFlags.Static |
            BindingFlags.Public,
            null,
            new[]
            {
                typeof(GameObject),
                typeof(Vector3),
                typeof(Quaternion),
                typeof(Scene)
            },
            null
        );

        Assert.That(method, Is.Not.Null);
        GameObject item = (GameObject)method.Invoke(
            null,
            new object[]
            {
                prefab,
                position,
                Quaternion.identity,
                destinationScene
            }
        );
        spawnedItems.Add(item);
        return item;
    }

    private static bool Release(GameObject instance)
    {
        MethodInfo method = ItemPoolType?.GetMethod(
            "Release",
            BindingFlags.Static |
            BindingFlags.Public
        );

        Assert.That(method, Is.Not.Null);
        return (bool)method.Invoke(
            null,
            new object[] { instance }
        );
    }

    private static object Invoke(
        object target,
        string methodName,
        params object[] arguments
    )
    {
        MethodInfo method = target
            .GetType()
            .GetMethod(
                methodName,
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.NonPublic
            );

        Assert.That(method, Is.Not.Null);
        return method.Invoke(target, arguments);
    }

    private static void SetField(
        object target,
        string fieldName,
        object value
    )
    {
        FieldInfo field = target
            .GetType()
            .GetField(
                fieldName,
                BindingFlags.Instance |
                BindingFlags.NonPublic
            );

        Assert.That(field, Is.Not.Null);
        field.SetValue(target, value);
    }
}
