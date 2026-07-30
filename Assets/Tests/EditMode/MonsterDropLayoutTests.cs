using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class MonsterDropLayoutTests
{
    private const float MinimumDropSeparation = 0.7f;

    private static readonly Type MonsterStatsType =
        Type.GetType("MonsterStats, Assembly-CSharp");

    private static readonly Type MonsterHealthType =
        Type.GetType("MonsterHealth, Assembly-CSharp");

    [Test]
    public void ExperienceAndRecoveryDropsDoNotOverlapWhenSpawnedTogether()
    {
        Assert.That(MonsterStatsType, Is.Not.Null);
        Assert.That(MonsterHealthType, Is.Not.Null);

        HashSet<int> existingDropIds = FindRuntimeDrops()
            .Select(drop => drop.GetInstanceID())
            .ToHashSet();
        ScriptableObject stats =
            ScriptableObject.CreateInstance(MonsterStatsType);
        GameObject monster = new GameObject("Drop Layout Test Monster");
        monster.SetActive(false);
        monster.transform.position = new Vector3(3f, -2f, 0f);

        try
        {
            ConfigureGuaranteedDrops(stats);

            Component health = monster.AddComponent(MonsterHealthType);
            SerializedObject serializedHealth =
                new SerializedObject(health);
            serializedHealth.FindProperty("stats").objectReferenceValue =
                stats;
            serializedHealth.ApplyModifiedPropertiesWithoutUndo();

            monster.SetActive(true);
            Invoke(health, "DropExperienceCrystal");
            Invoke(health, "DropRecoveryItems");

            GameObject[] spawnedDrops = FindRuntimeDrops()
                .Where(drop =>
                    !existingDropIds.Contains(drop.GetInstanceID()))
                .ToArray();

            Assert.That(spawnedDrops, Has.Length.EqualTo(4));
            AssertDropsAreSeparated(spawnedDrops);
        }
        finally
        {
            foreach (GameObject drop in FindRuntimeDrops())
            {
                if (!existingDropIds.Contains(drop.GetInstanceID()))
                {
                    UnityEngine.Object.DestroyImmediate(drop);
                }
            }

            UnityEngine.Object.DestroyImmediate(monster);
            UnityEngine.Object.DestroyImmediate(stats);
        }
    }

    private static void AssertDropsAreSeparated(
        IReadOnlyList<GameObject> drops
    )
    {
        for (int first = 0; first < drops.Count; first++)
        {
            for (int second = first + 1; second < drops.Count; second++)
            {
                float distance = Vector3.Distance(
                    drops[first].transform.position,
                    drops[second].transform.position
                );

                Assert.That(
                    distance,
                    Is.GreaterThanOrEqualTo(MinimumDropSeparation),
                    $"{drops[first].name} at " +
                    $"{drops[first].transform.position} overlaps " +
                    $"{drops[second].name} at " +
                    $"{drops[second].transform.position}."
                );
            }
        }
    }

    private static void ConfigureGuaranteedDrops(
        ScriptableObject stats
    )
    {
        SerializedObject serializedStats = new SerializedObject(stats);
        serializedStats
            .FindProperty("experienceCrystalDropPrefab")
            .objectReferenceValue =
            LoadPrefab("ExpCrystal_low");
        ConfigureRecoveryDrop(
            serializedStats,
            "redBerry",
            "RedBerry_low"
        );
        ConfigureRecoveryDrop(
            serializedStats,
            "starCandy",
            "StarCandy_low"
        );
        ConfigureRecoveryDrop(
            serializedStats,
            "pie",
            "Pie_low"
        );
        serializedStats.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void ConfigureRecoveryDrop(
        SerializedObject serializedStats,
        string fieldPrefix,
        string prefabName
    )
    {
        serializedStats
            .FindProperty($"{fieldPrefix}DropPrefab")
            .objectReferenceValue = LoadPrefab(prefabName);
        serializedStats
            .FindProperty($"{fieldPrefix}DropChancePercent")
            .floatValue = 100f;
    }

    private static GameObject LoadPrefab(string prefabName)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            $"Assets/Sprites/Item/{prefabName}.prefab"
        );
        Assert.That(prefab, Is.Not.Null);
        return prefab;
    }

    private static GameObject[] FindRuntimeDrops()
    {
        return Resources.FindObjectsOfTypeAll<GameObject>()
            .Where(drop =>
                drop.scene.IsValid() &&
                (drop.name.StartsWith(
                     "ExpCrystal_",
                     StringComparison.Ordinal
                 ) ||
                 drop.name.StartsWith(
                     "RedBerry_",
                     StringComparison.Ordinal
                 ) ||
                 drop.name.StartsWith(
                     "StarCandy_",
                     StringComparison.Ordinal
                 ) ||
                 drop.name.StartsWith(
                     "Pie_",
                     StringComparison.Ordinal
                 )))
            .ToArray();
    }

    private static object Invoke(
        object target,
        string methodName
    )
    {
        MethodInfo method = target.GetType().GetMethod(
            methodName,
            BindingFlags.Instance |
            BindingFlags.Public |
            BindingFlags.NonPublic
        );
        Assert.That(method, Is.Not.Null);
        return method.Invoke(target, Array.Empty<object>());
    }
}
