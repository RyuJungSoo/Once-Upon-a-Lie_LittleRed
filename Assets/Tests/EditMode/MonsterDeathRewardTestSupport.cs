using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

internal static class MonsterDeathRewardTestSupport
{
    internal const string BlanketPrefabPath =
        "Assets/Sprites/Monster/Blanket/Blanket.prefab";

    internal static readonly Type MonsterStatsType =
        Type.GetType("MonsterStats, Assembly-CSharp");

    internal static readonly Type MonsterHealthType =
        Type.GetType("MonsterHealth, Assembly-CSharp");

    internal static readonly Type MonsterDamageSourceType =
        Type.GetType(
            "MonsterDamageSource, Assembly-CSharp"
        );

    internal static readonly Type BlanketContactRestraintType =
        Type.GetType(
            "BlanketContactRestraint, Assembly-CSharp"
        );

    internal static Component CreateConfiguredMonster(
        out ScriptableObject stats,
        out GameObject monster
    )
    {
        Assert.That(MonsterStatsType, Is.Not.Null);
        Assert.That(MonsterHealthType, Is.Not.Null);
        Assert.That(MonsterDamageSourceType, Is.Not.Null);

        stats = ScriptableObject.CreateInstance(
            MonsterStatsType
        );
        ConfigureGuaranteedDrops(stats);
        ConfigureExperienceDrop(stats);

        monster = new GameObject("Test Reward Monster");
        monster.SetActive(false);

        Component health = monster.AddComponent(
            MonsterHealthType
        );
        SerializedObject serializedHealth =
            new SerializedObject(health);
        serializedHealth
            .FindProperty("stats")
            .objectReferenceValue = stats;
        serializedHealth.ApplyModifiedPropertiesWithoutUndo();

        monster.SetActive(true);
        return health;
    }

    internal static object CreateDamageSource(
        string damageSourceName
    )
    {
        if (damageSourceName == "Invalid")
        {
            return Enum.ToObject(
                MonsterDamageSourceType,
                int.MaxValue
            );
        }

        return Enum.Parse(
            MonsterDamageSourceType,
            damageSourceName
        );
    }

    internal static bool ContainsSelfDestructComponent(
        string prefabPath
    )
    {
        GameObject prefab =
            AssetDatabase.LoadAssetAtPath<GameObject>(
                prefabPath
            );

        return prefab != null &&
               prefab.GetComponentsInChildren(
                   BlanketContactRestraintType,
                   true
               ).Length > 0;
    }

    internal static HashSet<int> CaptureRuntimeDropIds()
    {
        return FindRuntimeDrops()
            .Select(drop => drop.GetInstanceID())
            .ToHashSet();
    }

    internal static void AssertNewDropCounts(
        HashSet<int> existingDropIds,
        int expectedRecoveryItemCount
    )
    {
        GameObject[] spawnedDrops = FindRuntimeDrops()
            .Where(drop =>
                !existingDropIds.Contains(
                    drop.GetInstanceID()
                ))
            .ToArray();

        Assert.That(
            spawnedDrops.Count(IsExperienceCrystal),
            Is.EqualTo(1)
        );
        Assert.That(
            spawnedDrops.Count(IsRecoveryItem),
            Is.EqualTo(expectedRecoveryItemCount)
        );
    }

    internal static void DestroyNewDrops(
        HashSet<int> existingDropIds
    )
    {
        foreach (GameObject drop in FindRuntimeDrops())
        {
            if (!existingDropIds.Contains(
                    drop.GetInstanceID()
                ))
            {
                UnityEngine.Object.DestroyImmediate(drop);
            }
        }
    }

    internal static void ExpectEditModeDestroyError()
    {
        LogAssert.Expect(
            LogType.Error,
            new Regex(
                "Destroy may not be called from edit mode!"
            )
        );
    }

    internal static object Invoke(
        object target,
        string methodName,
        params object[] arguments
    )
    {
        MethodInfo method = target
            .GetType()
            .GetMethods(
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.NonPublic
            )
            .SingleOrDefault(candidate =>
                candidate.Name == methodName &&
                candidate.GetParameters().Length ==
                arguments.Length
            );

        Assert.That(method, Is.Not.Null);
        return method.Invoke(target, arguments);
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
        serializedStats.ApplyModifiedPropertiesWithoutUndo();
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

    private static void ConfigureExperienceDrop(
        ScriptableObject stats
    )
    {
        SerializedObject serializedStats =
            new SerializedObject(stats);
        SerializedProperty experiencePrefab =
            serializedStats.FindProperty(
                "experienceCrystalDropPrefab"
            );

        Assert.That(experiencePrefab, Is.Not.Null);
        experiencePrefab.objectReferenceValue =
            AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Sprites/Item/ExpCrystal_low.prefab"
            );
        serializedStats.ApplyModifiedPropertiesWithoutUndo();
    }

    private static GameObject[] FindRuntimeDrops()
    {
        return Resources
            .FindObjectsOfTypeAll<GameObject>()
            .Where(drop =>
                drop.scene.IsValid() &&
                (IsExperienceCrystal(drop) ||
                 IsRecoveryItem(drop)))
            .ToArray();
    }

    private static bool IsExperienceCrystal(
        GameObject gameObject
    )
    {
        return gameObject.name.StartsWith(
            "ExpCrystal_",
            StringComparison.Ordinal
        );
    }

    private static bool IsRecoveryItem(
        GameObject gameObject
    )
    {
        return gameObject.name.StartsWith(
                   "RedBerry_",
                   StringComparison.Ordinal
               ) ||
               gameObject.name.StartsWith(
                   "StarCandy_",
                   StringComparison.Ordinal
               ) ||
               gameObject.name.StartsWith(
                   "Pie_",
                   StringComparison.Ordinal
               );
    }
}
