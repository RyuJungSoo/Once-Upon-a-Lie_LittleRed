using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class ExperienceCrystalTests
{
    private static readonly Type GameManagerType =
        Type.GetType("GameManager, Assembly-CSharp");

    private static readonly Type PlayerExperienceType =
        Type.GetType("PlayerExperience, Assembly-CSharp");

    private static readonly Type ExperiencePickupType =
        Type.GetType(
            "ExperienceCrystalPickup, Assembly-CSharp"
        );

    private static readonly Type MonsterHealthType =
        Type.GetType("MonsterHealth, Assembly-CSharp");

    private GameObject managerObject;
    private Component gameManager;
    private Component playerExperience;

    [SetUp]
    public void SetUp()
    {
        Assert.That(GameManagerType, Is.Not.Null);
        Assert.That(PlayerExperienceType, Is.Not.Null);
        Assert.That(ExperiencePickupType, Is.Not.Null);
        Assert.That(MonsterHealthType, Is.Not.Null);

        managerObject = new GameObject(
            "Test Experience Manager"
        );
        managerObject.SetActive(false);

        playerExperience = managerObject.AddComponent(
            PlayerExperienceType
        );
        gameManager = managerObject.AddComponent(
            GameManagerType
        );

        Invoke(playerExperience, "Awake");
        Invoke(gameManager, "Awake");
        managerObject.SetActive(true);
        Invoke(playerExperience, "ResetExperience");
    }

    [TearDown]
    public void TearDown()
    {
        UnityEngine.Object.DestroyImmediate(
            managerObject
        );

        foreach (GameObject crystal in
                 FindRuntimeCrystals())
        {
            UnityEngine.Object.DestroyImmediate(
                crystal
            );
        }
    }

    [Test]
    public void ExperienceUsesFixedOneHundredPerLevel()
    {
        List<int> gainedLevels =
            new List<int>();
        EventInfo levelGainedEvent =
            PlayerExperienceType.GetEvent(
                "OnLevelGained"
            );
        Action<int> handler =
            level => gainedLevels.Add(level);

        levelGainedEvent.AddEventHandler(
            playerExperience,
            handler
        );

        Invoke(
            playerExperience,
            "AddExperience",
            312
        );

        Assert.That(
            GetProperty<int>(
                playerExperience,
                "RequiredExperience"
            ),
            Is.EqualTo(100)
        );
        Assert.That(
            GetProperty<int>(
                playerExperience,
                "CurrentExperience"
            ),
            Is.EqualTo(12)
        );
        Assert.That(
            GetProperty<int>(
                gameManager,
                "CurrentPlayerLevel"
            ),
            Is.EqualTo(4)
        );
        Assert.That(
            gainedLevels,
            Is.EqualTo(new[] { 2, 3, 4 })
        );
    }

    [TestCase("ExpCrystal_low", 5)]
    [TestCase("ExpCrystal_medium", 8)]
    [TestCase("ExpCrystal_high", 12)]
    public void CrystalPrefabHasConfiguredExperience(
        string prefabName,
        int expectedExperience
    )
    {
        string path =
            $"Assets/Sprites/Item/{prefabName}.prefab";
        GameObject prefab =
            AssetDatabase.LoadAssetAtPath<GameObject>(
                path
            );

        Assert.That(prefab, Is.Not.Null);

        Component pickup = prefab.GetComponent(
            ExperiencePickupType
        );
        BoxCollider2D pickupCollider =
            prefab.GetComponent<BoxCollider2D>();

        Assert.That(pickup, Is.Not.Null);
        Assert.That(pickupCollider, Is.Not.Null);
        Assert.That(pickupCollider.isTrigger, Is.True);
        Assert.That(
            GetProperty<int>(
                pickup,
                "ExperienceAmount"
            ),
            Is.EqualTo(expectedExperience)
        );
    }

    [Test]
    public void CrystalPickupAddsConfiguredExperience()
    {
        GameObject pickupObject =
            new GameObject("Test Experience Crystal");
        pickupObject.SetActive(false);
        pickupObject.AddComponent<BoxCollider2D>();

        Component pickup =
            pickupObject.AddComponent(
                ExperiencePickupType
            );
        SerializedObject serializedPickup =
            new SerializedObject(pickup);
        serializedPickup
            .FindProperty("experienceAmount")
            .intValue = 12;
        serializedPickup
            .ApplyModifiedPropertiesWithoutUndo();

        pickupObject.SetActive(true);

        try
        {
            bool collected = (bool)Invoke(
                pickup,
                "TryCollect",
                playerExperience
            );

            Assert.That(collected, Is.True);
            Assert.That(
                GetProperty<int>(
                    playerExperience,
                    "CurrentExperience"
                ),
                Is.EqualTo(12)
            );
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(
                pickupObject
            );
        }
    }

    [Test]
    public void EveryMonsterStatsHasCrystalDrop()
    {
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
            UnityEngine.Object drop =
                serializedStats
                    .FindProperty(
                        "experienceCrystalDropPrefab"
                    )
                    .objectReferenceValue;

            Assert.That(
                drop,
                Is.Not.Null,
                $"{path} has no experience crystal drop."
            );
        }
    }

    [Test]
    public void MonsterHealthSpawnsConfiguredCrystal()
    {
        GameObject monsterPrefab =
            AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Sprites/Monster/Bird/Bird.prefab"
            );
        GameObject monster =
            (GameObject)PrefabUtility.InstantiatePrefab(
                monsterPrefab
            );

        try
        {
            Component health = monster.GetComponent(
                MonsterHealthType
            );
            int beforeCount =
                FindRuntimeCrystals().Length;

            Invoke(
                health,
                "DropExperienceCrystal"
            );

            Assert.That(
                FindRuntimeCrystals().Length,
                Is.EqualTo(beforeCount + 1)
            );
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(
                monster
            );
        }
    }

    private static GameObject[] FindRuntimeCrystals()
    {
        return Resources
            .FindObjectsOfTypeAll<GameObject>()
            .Where(gameObject =>
                gameObject.scene.IsValid() &&
                gameObject.name.StartsWith(
                    "ExpCrystal_",
                    StringComparison.Ordinal
                ))
            .ToArray();
    }

    private static T GetProperty<T>(
        object target,
        string propertyName
    )
    {
        PropertyInfo property = target
            .GetType()
            .GetProperty(
                propertyName,
                BindingFlags.Instance |
                BindingFlags.Public
            );

        Assert.That(property, Is.Not.Null);
        return (T)property.GetValue(target);
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
