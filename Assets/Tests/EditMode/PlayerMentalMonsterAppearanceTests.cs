using System;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class PlayerMentalMonsterAppearanceTests
{
    private const string RabbitControllerPath =
        "Assets/Sprites/Monster/Rabbit/Rabbit.controller";

    private static readonly Type PlayerMentalType =
        Type.GetType("PlayerMental, Assembly-CSharp");

    private static readonly Type MonsterAppearanceType =
        Type.GetType("MonsterSanityAppearance, Assembly-CSharp");

    private static readonly Type PlayerLevelStatsType =
        Type.GetType("PlayerLevelStats, Assembly-CSharp");

    private GameObject mentalObject;
    private GameObject monsterObject;
    private Component playerMental;
    private Component monsterAppearance;
    private Animator monsterAnimator;

    [SetUp]
    public void SetUp()
    {
        Assert.That(PlayerMentalType, Is.Not.Null);
        Assert.That(MonsterAppearanceType, Is.Not.Null);
        Assert.That(PlayerLevelStatsType, Is.Not.Null);

        mentalObject = new GameObject("Test Player Mental");
        mentalObject.SetActive(false);
        playerMental = mentalObject.AddComponent(PlayerMentalType);

        Component levelStats = mentalObject.GetComponent(
            PlayerLevelStatsType
        );
        Invoke(levelStats, "RecalculateStats", 1);

        mentalObject.SetActive(true);
        Invoke(playerMental, "ResetMental");

        monsterObject = new GameObject("Test Monster");
        monsterObject.SetActive(false);
        monsterAppearance = monsterObject.AddComponent(
            MonsterAppearanceType
        );
        monsterAnimator = monsterObject.GetComponent<Animator>();
        Invoke(monsterAppearance, "Awake");
        monsterAnimator.runtimeAnimatorController =
            AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(
                RabbitControllerPath
            );

        monsterObject.SetActive(true);
        Invoke(
            monsterAppearance,
            "SetPlayerMental",
            playerMental
        );
    }

    [TearDown]
    public void TearDown()
    {
        UnityEngine.Object.DestroyImmediate(monsterObject);
        UnityEngine.Object.DestroyImmediate(mentalObject);
    }

    [TestCase(100f, "High", 2)]
    [TestCase(67f, "High", 2)]
    [TestCase(66f, "Medium", 1)]
    [TestCase(34f, "Medium", 1)]
    [TestCase(33f, "Low", 0)]
    [TestCase(0f, "Low", 0)]
    public void MentalStateChangeUpdatesMonsterAnimatorStage(
        float mental,
        string expectedState,
        int expectedAnimatorStage
    )
    {
        Invoke(playerMental, "SetMental", mental);

        object mentalState = GetProperty(
            playerMental,
            "CurrentMentalState"
        );
        object appearanceState = GetProperty(
            monsterAppearance,
            "CurrentMentalState"
        );
        object mentalSource = GetProperty(
            monsterAppearance,
            "MentalSource"
        );

        Assert.That(mentalState.ToString(), Is.EqualTo(expectedState));
        Assert.That(appearanceState.ToString(), Is.EqualTo(expectedState));
        Assert.That(mentalSource, Is.SameAs(playerMental));
        Assert.That(
            monsterAnimator.GetInteger("SanityStage"),
            Is.EqualTo(expectedAnimatorStage)
        );
    }

    private static object GetProperty(
        object target,
        string propertyName
    )
    {
        PropertyInfo property = target
            .GetType()
            .GetProperty(
                propertyName,
                BindingFlags.Instance | BindingFlags.Public
            );

        Assert.That(property, Is.Not.Null);
        return property.GetValue(target);
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
