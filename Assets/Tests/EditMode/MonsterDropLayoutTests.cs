using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public sealed class MonsterDropLayoutTests
{
    [TearDown]
    public void TearDown()
    {
        ItemPoolTestCleanup.DestroyPoolObjects();
    }

    private const float MinimumDropSeparation = 0.7f;

    private static readonly Type MonsterStatsType =
        Type.GetType("MonsterStats, Assembly-CSharp");

    private static readonly Type MonsterHealthType =
        Type.GetType("MonsterHealth, Assembly-CSharp");

    private static readonly Type ItemRenderOrderType =
        Type.GetType("ItemRenderOrder, Assembly-CSharp");

    [Test]
    public void ExperienceAndRecoveryDropsHaveStableRenderOrder()
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
            AssertDropsHaveDistinctSortingGroups(
                spawnedDrops
            );
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

    [Test]
    public void ItemLayerSceneObjectsReceiveStableRenderOrder()
    {
        Scene testScene = EditorSceneManager.NewScene(
            NewSceneSetup.EmptyScene,
            NewSceneMode.Single
        );

        try
        {
            GameObject first = CreateSceneItem("First Item");
            GameObject second = CreateSceneItem("Second Item");

            InvokeStatic(
                ItemRenderOrderType,
                "AssignSceneItems",
                testScene
            );

            AssertDropsHaveDistinctSortingGroups(
                new[] { first, second }
            );
        }
        finally
        {
            EditorSceneManager.NewScene(
                NewSceneSetup.EmptyScene,
                NewSceneMode.Single
            );
        }
    }

    private static GameObject CreateSceneItem(string name)
    {
        GameObject item = new GameObject(name);
        item.layer = LayerMask.NameToLayer("Item");
        item.AddComponent<SpriteRenderer>();
        return item;
    }

    private static void AssertDropsHaveDistinctSortingGroups(
        IReadOnlyList<GameObject> drops
    )
    {
        SortingGroup[] sortingGroups = drops
            .Select(drop => drop.GetComponent<SortingGroup>())
            .ToArray();

        Assert.That(
            sortingGroups.All(group => group != null),
            Is.True,
            "Every runtime drop needs a root SortingGroup."
        );
        Assert.That(
            sortingGroups
                .Select(group => group.sortingOrder)
                .Distinct()
                .Count(),
            Is.EqualTo(drops.Count),
            "Overlapping drops need unique render-order ties."
        );
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

    private static object InvokeStatic(
        Type type,
        string methodName,
        params object[] arguments
    )
    {
        Assert.That(type, Is.Not.Null);
        MethodInfo method = type.GetMethod(
            methodName,
            BindingFlags.Static |
            BindingFlags.Public |
            BindingFlags.NonPublic
        );
        Assert.That(method, Is.Not.Null);
        return method.Invoke(null, arguments);
    }
}
