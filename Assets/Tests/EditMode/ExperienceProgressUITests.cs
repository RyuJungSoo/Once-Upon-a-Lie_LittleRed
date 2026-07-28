using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public sealed class ExperienceProgressUITests
{
    private static readonly Type GameManagerType =
        Type.GetType("GameManager, Assembly-CSharp");

    private static readonly Type PlayerExperienceType =
        Type.GetType("PlayerExperience, Assembly-CSharp");

    private static readonly Type UIManagerType =
        Type.GetType("UIManager, Assembly-CSharp");

    private static readonly Type ImageType =
        Type.GetType("UnityEngine.UI.Image, UnityEngine.UI");

    private static readonly Type TextType =
        Type.GetType(
            "TMPro.TextMeshProUGUI, Unity.TextMeshPro"
        );

    private GameObject managerObject;
    private GameObject uiObject;
    private Component gameManager;
    private Component playerExperience;
    private Component uiManager;
    private Component experienceGauge;
    private Component levelText;

    [SetUp]
    public void SetUp()
    {
        Assert.That(GameManagerType, Is.Not.Null);
        Assert.That(PlayerExperienceType, Is.Not.Null);
        Assert.That(UIManagerType, Is.Not.Null);
        Assert.That(ImageType, Is.Not.Null);
        Assert.That(TextType, Is.Not.Null);

        managerObject = new GameObject(
            "Experience UI Test Manager"
        );
        managerObject.SetActive(false);
        playerExperience = managerObject.AddComponent(
            PlayerExperienceType
        );
        gameManager = managerObject.AddComponent(
            GameManagerType
        );

        uiObject = new GameObject(
            "Experience UI Test Canvas"
        );
        uiObject.SetActive(false);
        uiManager = uiObject.AddComponent(UIManagerType);

        GameObject gaugeObject = new GameObject(
            "Experience Gauge",
            typeof(RectTransform),
            typeof(CanvasRenderer)
        );
        gaugeObject.transform.SetParent(
            uiObject.transform,
            false
        );
        experienceGauge =
            gaugeObject.AddComponent(ImageType);

        GameObject textObject = new GameObject(
            "Level Text",
            typeof(RectTransform),
            typeof(CanvasRenderer)
        );
        textObject.transform.SetParent(
            uiObject.transform,
            false
        );
        levelText = textObject.AddComponent(TextType);

        SetField(
            uiManager,
            "experienceGauge",
            experienceGauge
        );
        SetField(uiManager, "levelText", levelText);

        Invoke(playerExperience, "Awake");
        Invoke(gameManager, "Awake");
        Invoke(uiManager, "Awake");

        managerObject.SetActive(true);
        uiObject.SetActive(true);

        Invoke(playerExperience, "ResetExperience");
        Invoke(uiManager, "TryBindProgressUI");
    }

    [TearDown]
    public void TearDown()
    {
        UnityEngine.Object.DestroyImmediate(uiObject);
        UnityEngine.Object.DestroyImmediate(managerObject);
    }

    [Test]
    public void ExactThresholdDelaysHudLevelUntilFull()
    {
        SetExperience(95);
        AddExperience(5);

        Assert.That(AuthoritativeLevel, Is.EqualTo(2));
        Assert.That(
            FillAmount,
            Is.EqualTo(0.95f).Within(0.0001f)
        );
        Assert.That(LevelLabel, Is.EqualTo("Lv. 1"));

        Advance(0.079f);

        Assert.That(FillAmount, Is.LessThan(1f));
        Assert.That(LevelLabel, Is.EqualTo("Lv. 1"));

        Advance(0.001f);

        Assert.That(FillAmount, Is.EqualTo(1f));
        Assert.That(LevelLabel, Is.EqualTo("Lv. 2"));

        Advance(0.10f);

        Assert.That(FillAmount, Is.Zero);
        Assert.That(LevelLabel, Is.EqualTo("Lv. 2"));
    }

    [Test]
    public void OverflowAppearsOnlyAfterFullHoldAndReset()
    {
        SetExperience(95);
        AddExperience(17);
        Advance(0.08f);

        Assert.That(FillAmount, Is.EqualTo(1f));
        Assert.That(LevelLabel, Is.EqualTo("Lv. 2"));

        Advance(0.10f);

        Assert.That(FillAmount, Is.Zero);

        Advance(0.08f);

        Assert.That(
            FillAmount,
            Is.EqualTo(0.12f).Within(0.0001f)
        );
        Assert.That(LevelLabel, Is.EqualTo("Lv. 2"));
    }

    [Test]
    public void MultiLevelGainPresentsEveryFullInOrder()
    {
        AddExperience(312);

        for (int level = 2; level <= 4; level++)
        {
            Advance(0.35f);

            Assert.That(FillAmount, Is.EqualTo(1f));
            Assert.That(
                LevelLabel,
                Is.EqualTo($"Lv. {level}")
            );

            Advance(0.10f);

            Assert.That(FillAmount, Is.Zero);
        }

        Advance(0.08f);

        Assert.That(
            FillAmount,
            Is.EqualTo(0.12f).Within(0.0001f)
        );
        Assert.That(LevelLabel, Is.EqualTo("Lv. 4"));
    }

    [Test]
    public void SameFrameAndActiveGainsAreNotLostOrDuplicated()
    {
        SetExperience(10);
        AddExperience(40);
        AddExperience(20);
        Advance(1f);

        AssertAuthoritativeAndHud(1, 70, 0.70f);

        SetExperience(10);
        AddExperience(40);
        Advance(0.05f);
        AddExperience(20);
        Advance(1f);

        AssertAuthoritativeAndHud(1, 70, 0.70f);
    }

    [Test]
    public void DirectChangeAndStageStartCancelStaleQueue()
    {
        SetExperience(10);
        AddExperience(40);
        SetExperience(80);
        Advance(0.01f);

        AssertAuthoritativeAndHud(1, 80, 0.80f);

        AddExperience(10);
        Invoke(uiManager, "HandleStageStarted", 1);

        AssertAuthoritativeAndHud(1, 90, 0.90f);
        Assert.That(
            GetField<bool>(
                uiManager,
                "isExperiencePresentationActive"
            ),
            Is.False
        );

        Invoke(playerExperience, "ResetExperience");

        AssertAuthoritativeAndHud(1, 0, 0f);
    }

    [Test]
    public void DisableAndRebindCancelQueueWithoutDuplicateHandlers()
    {
        SetExperience(10);
        AddExperience(40);

        Invoke(uiManager, "OnDisable");
        Invoke(uiManager, "TryBindProgressUI");
        Invoke(uiManager, "UnbindProgressUI");
        Invoke(uiManager, "TryBindProgressUI");

        AssertAuthoritativeAndHud(1, 50, 0.50f);

        AddExperience(5);
        Advance(0.08f);

        AssertAuthoritativeAndHud(1, 55, 0.55f);
    }

    private int AuthoritativeLevel =>
        GetProperty<int>(
            gameManager,
            "CurrentPlayerLevel"
        );

    private int AuthoritativeExperience =>
        GetProperty<int>(
            playerExperience,
            "CurrentExperience"
        );

    private float FillAmount =>
        GetProperty<float>(
            experienceGauge,
            "fillAmount"
        );

    private string LevelLabel =>
        GetProperty<string>(levelText, "text");

    private void SetExperience(int amount)
    {
        Invoke(
            playerExperience,
            "SetExperience",
            amount
        );
    }

    private void AddExperience(int amount)
    {
        Invoke(
            playerExperience,
            "AddExperience",
            amount
        );
    }

    private void Advance(float deltaTime)
    {
        Invoke(
            uiManager,
            "AdvanceExperiencePresentation",
            deltaTime
        );
    }

    private void AssertAuthoritativeAndHud(
        int expectedLevel,
        int expectedExperience,
        float expectedFill
    )
    {
        Assert.That(
            AuthoritativeLevel,
            Is.EqualTo(expectedLevel)
        );
        Assert.That(
            AuthoritativeExperience,
            Is.EqualTo(expectedExperience)
        );
        Assert.That(
            FillAmount,
            Is.EqualTo(expectedFill).Within(0.0001f)
        );
        Assert.That(
            LevelLabel,
            Is.EqualTo($"Lv. {expectedLevel}")
        );
    }

    private static T GetField<T>(
        object target,
        string fieldName
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
        return (T)field.GetValue(target);
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
