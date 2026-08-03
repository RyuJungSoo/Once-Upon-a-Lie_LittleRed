using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class MonsterRecoveryItemDropTests
{
    [TearDown]
    public void TearDown()
    {
        ItemPoolTestCleanup.DestroyPoolObjects();
    }

    private static readonly Type MonsterStatsType =
        Type.GetType("MonsterStats, Assembly-CSharp");

    private static readonly Type MonsterHealthType =
        Type.GetType("MonsterHealth, Assembly-CSharp");

    [Test]
    public void EveryMonsterStatsConfiguresRecoveryItemDrops()
    {
        Assert.That(MonsterStatsType, Is.Not.Null);

        string[] statGuids =
            AssetDatabase.FindAssets(
                "t:MonsterStats",
                new[]
                {
                    "Assets/Sprites/Monster"
                }
            );

        Assert.That(statGuids.Length, Is.EqualTo(12));

        foreach (string guid in statGuids)
        {
            string path =
                AssetDatabase.GUIDToAssetPath(guid);
            ScriptableObject stats =
                AssetDatabase.LoadAssetAtPath
                    <ScriptableObject>(path);
            SerializedObject serializedStats =
                new SerializedObject(stats);

            AssertDropConfigured(
                serializedStats,
                path,
                "redBerry",
                "RedBerry_"
            );
            AssertDropConfigured(
                serializedStats,
                path,
                "starCandy",
                "StarCandy_"
            );
            AssertDropConfigured(
                serializedStats,
                path,
                "pie",
                "Pie_"
            );

            float redBerryChance =
                GetChance(
                    serializedStats,
                    "redBerry"
                );
            float starCandyChance =
                GetChance(
                    serializedStats,
                    "starCandy"
                );
            float pieChance =
                GetChance(
                    serializedStats,
                    "pie"
                );

            Assert.That(
                starCandyChance,
                Is.GreaterThan(redBerryChance),
                $"{path}: StarCandy should use the " +
                "normal drop chance."
            );
            Assert.That(
                redBerryChance,
                Is.GreaterThan(pieChance),
                $"{path}: Pie should be rarer than " +
                "RedBerry."
            );
        }
    }

    [Test]
    public void GuaranteedRecoveryItemDropsSpawnAllConfiguredItems()
    {
        Assert.That(MonsterStatsType, Is.Not.Null);
        Assert.That(MonsterHealthType, Is.Not.Null);

        ScriptableObject stats =
            ScriptableObject.CreateInstance(
                MonsterStatsType
            );
        GameObject monster =
            new GameObject("Test Drop Monster");
        monster.SetActive(false);

        HashSet<int> existingItemIds =
            FindRuntimeRecoveryItems()
                .Select(item => item.GetInstanceID())
                .ToHashSet();

        try
        {
            ConfigureGuaranteedDrops(stats);

            Component health = monster.AddComponent(
                MonsterHealthType
            );
            SerializedObject serializedHealth =
                new SerializedObject(health);
            serializedHealth
                .FindProperty("stats")
                .objectReferenceValue = stats;
            serializedHealth
                .ApplyModifiedPropertiesWithoutUndo();

            monster.SetActive(true);
            Invoke(
                health,
                "DropRecoveryItems"
            );

            GameObject[] spawnedItems =
                FindRuntimeRecoveryItems()
                    .Where(item =>
                        !existingItemIds.Contains(
                            item.GetInstanceID()
                        ))
                    .ToArray();

            Assert.That(spawnedItems.Length, Is.EqualTo(3));
            Assert.That(
                spawnedItems.Any(item =>
                    item.name.StartsWith(
                        "RedBerry_",
                        StringComparison.Ordinal
                    )),
                Is.True
            );
            Assert.That(
                spawnedItems.Any(item =>
                    item.name.StartsWith(
                        "StarCandy_",
                        StringComparison.Ordinal
                    )),
                Is.True
            );
            Assert.That(
                spawnedItems.Any(item =>
                    item.name.StartsWith(
                        "Pie_",
                        StringComparison.Ordinal
                    )),
                Is.True
            );
        }
        finally
        {
            foreach (GameObject item in
                     FindRuntimeRecoveryItems())
            {
                if (!existingItemIds.Contains(
                        item.GetInstanceID()
                    ))
                {
                    UnityEngine.Object.DestroyImmediate(
                        item
                    );
                }
            }

            UnityEngine.Object.DestroyImmediate(monster);
            UnityEngine.Object.DestroyImmediate(stats);
        }
    }

    private static void AssertDropConfigured(
        SerializedObject serializedStats,
        string assetPath,
        string fieldPrefix,
        string expectedPrefabPrefix
    )
    {
        SerializedProperty prefabProperty =
            serializedStats.FindProperty(
                $"{fieldPrefix}DropPrefab"
            );
        SerializedProperty chanceProperty =
            serializedStats.FindProperty(
                $"{fieldPrefix}DropChancePercent"
            );

        Assert.That(
            prefabProperty,
            Is.Not.Null,
            $"{assetPath}: missing {fieldPrefix} prefab."
        );
        Assert.That(
            chanceProperty,
            Is.Not.Null,
            $"{assetPath}: missing {fieldPrefix} chance."
        );
        Assert.That(
            prefabProperty.objectReferenceValue,
            Is.Not.Null,
            $"{assetPath}: {fieldPrefix} prefab is empty."
        );
        Assert.That(
            prefabProperty.objectReferenceValue.name,
            Does.StartWith(expectedPrefabPrefix)
        );
        Assert.That(
            chanceProperty.floatValue,
            Is.InRange(0f, 100f)
        );
    }

    private static float GetChance(
        SerializedObject serializedStats,
        string fieldPrefix
    )
    {
        return serializedStats
            .FindProperty(
                $"{fieldPrefix}DropChancePercent"
            )
            .floatValue;
    }

    private static void ConfigureGuaranteedDrops(
        ScriptableObject stats
    )
    {
        SerializedObject serializedStats =
            new SerializedObject(stats);

        ConfigureDrop(
            serializedStats,
            "redBerry",
            "Assets/Sprites/Item/RedBerry_low.prefab"
        );
        ConfigureDrop(
            serializedStats,
            "starCandy",
            "Assets/Sprites/Item/StarCandy_low.prefab"
        );
        ConfigureDrop(
            serializedStats,
            "pie",
            "Assets/Sprites/Item/Pie_low.prefab"
        );

        serializedStats
            .ApplyModifiedPropertiesWithoutUndo();
    }

    private static void ConfigureDrop(
        SerializedObject serializedStats,
        string fieldPrefix,
        string prefabPath
    )
    {
        SerializedProperty prefabProperty =
            serializedStats.FindProperty(
                $"{fieldPrefix}DropPrefab"
            );
        SerializedProperty chanceProperty =
            serializedStats.FindProperty(
                $"{fieldPrefix}DropChancePercent"
            );

        Assert.That(prefabProperty, Is.Not.Null);
        Assert.That(chanceProperty, Is.Not.Null);

        prefabProperty.objectReferenceValue =
            AssetDatabase.LoadAssetAtPath<GameObject>(
                prefabPath
            );
        chanceProperty.floatValue = 100f;
    }

    private static GameObject[]
        FindRuntimeRecoveryItems()
    {
        return Resources
            .FindObjectsOfTypeAll<GameObject>()
            .Where(item =>
                item.scene.IsValid() &&
                (item.name.StartsWith(
                     "RedBerry_",
                     StringComparison.Ordinal
                 ) ||
                 item.name.StartsWith(
                     "StarCandy_",
                     StringComparison.Ordinal
                 ) ||
                 item.name.StartsWith(
                     "Pie_",
                     StringComparison.Ordinal
                 )))
            .ToArray();
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
}
