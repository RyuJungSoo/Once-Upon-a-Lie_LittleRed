using System;
using System.Reflection;
using NUnit.Framework;

public sealed class ExperienceProgressPresentationTests
{
    private static readonly Type PresentationType =
        Type.GetType(
            "ExperienceProgressPresentation, Assembly-CSharp"
        );

    [Test]
    public void ResetSynchronizesSnapshotAndIgnoresMalformedInput()
    {
        object presentation = CreatePresentation();

        Invoke(presentation, "Reset", 25, 100, 3);
        Invoke(presentation, "EnqueueExperience", 0);
        Invoke(presentation, "EnqueueExperience", -5);
        Invoke(presentation, "Advance", -1f);

        AssertFill(presentation, 0.25f);
        Assert.That(
            GetProperty<int>(presentation, "DisplayedLevel"),
            Is.EqualTo(3)
        );
        AssertPhase(presentation, "Idle");
    }

    [Test]
    public void ExactThresholdAppliesLevelOnlyAtFullThenResets()
    {
        object presentation = CreatePresentation();

        Invoke(presentation, "Reset", 95, 100, 1);
        Invoke(presentation, "EnqueueExperience", 5);
        Invoke(presentation, "EnqueueLevel", 2);

        Invoke(presentation, "Advance", 0.079f);

        Assert.That(
            GetProperty<float>(presentation, "FillAmount"),
            Is.LessThan(1f)
        );
        Assert.That(
            GetProperty<int>(presentation, "DisplayedLevel"),
            Is.EqualTo(1)
        );
        AssertPhase(presentation, "Filling");

        Invoke(presentation, "Advance", 0.001f);

        AssertFill(presentation, 1f);
        Assert.That(
            GetProperty<int>(presentation, "DisplayedLevel"),
            Is.EqualTo(2)
        );
        AssertPhase(presentation, "HoldingFull");

        Invoke(presentation, "Advance", 0.099f);

        AssertFill(presentation, 1f);
        AssertPhase(presentation, "HoldingFull");

        Invoke(presentation, "Advance", 0.001f);

        AssertFill(presentation, 0f);
        Assert.That(
            GetProperty<int>(presentation, "DisplayedLevel"),
            Is.EqualTo(2)
        );
        AssertPhase(presentation, "Idle");
    }

    [Test]
    public void OverflowFillsOnlyAfterFullHold()
    {
        object presentation = CreatePresentation();

        Invoke(presentation, "Reset", 95, 100, 1);
        Invoke(presentation, "EnqueueExperience", 17);
        Invoke(presentation, "EnqueueLevel", 2);

        Invoke(presentation, "Advance", 0.08f);

        AssertFill(presentation, 1f);
        Assert.That(
            GetProperty<int>(presentation, "DisplayedLevel"),
            Is.EqualTo(2)
        );
        AssertPhase(presentation, "HoldingFull");

        Invoke(presentation, "Advance", 0.10f);

        AssertFill(presentation, 0f);
        AssertPhase(presentation, "Filling");

        Invoke(presentation, "Advance", 0.08f);

        AssertFill(presentation, 0.12f);
        Assert.That(
            GetProperty<int>(presentation, "DisplayedLevel"),
            Is.EqualTo(2)
        );
        AssertPhase(presentation, "Idle");
    }

    [Test]
    public void LargeDeltaCarriesAcrossFillHoldAndOverflow()
    {
        object presentation = CreatePresentation();

        Invoke(presentation, "Reset", 95, 100, 1);
        Invoke(presentation, "EnqueueExperience", 17);
        Invoke(presentation, "EnqueueLevel", 2);

        Invoke(presentation, "Advance", 0.26f);

        AssertFill(presentation, 0.12f);
        Assert.That(
            GetProperty<int>(presentation, "DisplayedLevel"),
            Is.EqualTo(2)
        );
        AssertPhase(presentation, "Idle");
    }

    [Test]
    public void MultipleLevelsReachFullAndApplyLevelsInOrder()
    {
        object presentation = CreatePresentation();

        Invoke(presentation, "Reset", 0, 100, 1);
        Invoke(presentation, "EnqueueExperience", 312);
        Invoke(presentation, "EnqueueLevel", 2);
        Invoke(presentation, "EnqueueLevel", 3);
        Invoke(presentation, "EnqueueLevel", 4);

        for (int level = 2; level <= 4; level++)
        {
            Invoke(presentation, "Advance", 0.35f);

            AssertFill(presentation, 1f);
            Assert.That(
                GetProperty<int>(
                    presentation,
                    "DisplayedLevel"
                ),
                Is.EqualTo(level)
            );
            AssertPhase(presentation, "HoldingFull");

            Invoke(presentation, "Advance", 0.10f);
        }

        Invoke(presentation, "Advance", 0.08f);

        AssertFill(presentation, 0.12f);
        Assert.That(
            GetProperty<int>(presentation, "DisplayedLevel"),
            Is.EqualTo(4)
        );
        AssertPhase(presentation, "Idle");
    }

    [Test]
    public void SameFrameAndActiveGainsAreQueued()
    {
        object presentation = CreatePresentation();

        Invoke(presentation, "Reset", 10, 100, 1);
        Invoke(presentation, "EnqueueExperience", 40);
        Invoke(presentation, "EnqueueExperience", 20);

        AdvanceUntilIdle(presentation);

        AssertFill(presentation, 0.70f);

        Invoke(presentation, "Reset", 10, 100, 1);
        Invoke(presentation, "EnqueueExperience", 40);
        Invoke(presentation, "Advance", 0.10f);
        Invoke(presentation, "EnqueueExperience", 20);

        AdvanceUntilIdle(presentation);

        AssertFill(presentation, 0.70f);
        Assert.That(
            GetProperty<int>(presentation, "DisplayedLevel"),
            Is.EqualTo(1)
        );
        AssertPhase(presentation, "Idle");
    }

    [Test]
    public void InvalidProjectedLevelCancelsToLatestAuthoritativeState()
    {
        object presentation = CreatePresentation();

        Invoke(presentation, "Reset", 20, 100, 1);
        Invoke(presentation, "EnqueueExperience", 10);
        Invoke(presentation, "EnqueueLevel", 4);

        AssertFill(presentation, 0.30f);
        Assert.That(
            GetProperty<int>(presentation, "DisplayedLevel"),
            Is.EqualTo(4)
        );
        AssertPhase(presentation, "Idle");
    }

    private static object CreatePresentation()
    {
        Assert.That(PresentationType, Is.Not.Null);
        return Activator.CreateInstance(PresentationType);
    }

    private static void AdvanceUntilIdle(object target)
    {
        for (int i = 0; i < 20; i++)
        {
            if (GetProperty<object>(
                    target,
                    "CurrentPhase"
                ).ToString() == "Idle")
            {
                return;
            }

            Invoke(target, "Advance", 1f);
        }

        Assert.Fail("Presentation did not return to Idle.");
    }

    private static void AssertFill(
        object target,
        float expected
    )
    {
        Assert.That(
            GetProperty<float>(target, "FillAmount"),
            Is.EqualTo(expected).Within(0.0001f)
        );
    }

    private static void AssertPhase(
        object target,
        string expected
    )
    {
        Assert.That(
            GetProperty<object>(
                target,
                "CurrentPhase"
            ).ToString(),
            Is.EqualTo(expected)
        );
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
                BindingFlags.Public
            );

        Assert.That(method, Is.Not.Null);
        return method.Invoke(target, arguments);
    }
}
