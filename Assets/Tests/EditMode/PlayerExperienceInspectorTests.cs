using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public sealed class PlayerExperienceInspectorTests
{
    private static readonly Type GameManagerType =
        Type.GetType("GameManager, Assembly-CSharp");

    private static readonly Type PlayerExperienceType =
        Type.GetType("PlayerExperience, Assembly-CSharp");

    private GameObject managerObject;
    private Component gameManager;
    private Component playerExperience;

    [SetUp]
    public void SetUp()
    {
        Assert.That(GameManagerType, Is.Not.Null);
        Assert.That(PlayerExperienceType, Is.Not.Null);

        managerObject = new GameObject(
            "Player Experience Inspector Test Manager"
        );
        managerObject.SetActive(false);
        playerExperience = managerObject.AddComponent(
            PlayerExperienceType
        );
        gameManager = managerObject.AddComponent(GameManagerType);

        SetField(
            playerExperience,
            "<RequiredExperience>k__BackingField",
            250
        );
    }

    [TearDown]
    public void TearDown()
    {
        UnityEngine.Object.DestroyImmediate(managerObject);
    }

    [Test]
    public void InspectorRequiredExperienceSurvivesRuntimeLifecycle()
    {
        Invoke(playerExperience, "Awake");
        Invoke(gameManager, "Awake");
        Invoke(playerExperience, "Start");

        AssertRequiredExperience(250);

        Invoke(gameManager, "StartGame");

        AssertRequiredExperience(250);

        Invoke(playerExperience, "AddExperience", 250);

        AssertRequiredExperience(250);
        Assert.That(
            GetProperty<int>(
                gameManager,
                "CurrentPlayerLevel"
            ),
            Is.EqualTo(2)
        );
    }

    private void AssertRequiredExperience(int expected)
    {
        Assert.That(
            GetProperty<int>(
                playerExperience,
                "RequiredExperience"
            ),
            Is.EqualTo(expected)
        );
    }

    private static void SetField(
        object target,
        string fieldName,
        object value
    )
    {
        FieldInfo field = target.GetType().GetField(
            fieldName,
            BindingFlags.Instance |
            BindingFlags.NonPublic
        );

        Assert.That(field, Is.Not.Null, fieldName);
        field.SetValue(target, value);
    }

    private static T GetProperty<T>(
        object target,
        string propertyName
    )
    {
        PropertyInfo property = target.GetType().GetProperty(
            propertyName,
            BindingFlags.Instance |
            BindingFlags.Public
        );

        Assert.That(property, Is.Not.Null, propertyName);
        return (T)property.GetValue(target);
    }

    private static object Invoke(
        object target,
        string methodName,
        params object[] arguments
    )
    {
        MethodInfo method = target.GetType().GetMethod(
            methodName,
            BindingFlags.Instance |
            BindingFlags.Public |
            BindingFlags.NonPublic
        );

        Assert.That(method, Is.Not.Null, methodName);
        return method.Invoke(target, arguments);
    }
}
