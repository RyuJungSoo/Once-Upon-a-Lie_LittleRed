using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public sealed class MothFanAttackTests
{
    private static readonly Type MothFanAttackType =
        Type.GetType("MothFanAttack, Assembly-CSharp");

    [Test]
    public void FanDirectionsSpanConfiguredArc()
    {
        Assert.That(MothFanAttackType, Is.Not.Null);

        MethodInfo calculateDirection =
            MothFanAttackType.GetMethod(
                "CalculateShotDirection",
                BindingFlags.Static |
                BindingFlags.NonPublic
            );

        Assert.That(calculateDirection, Is.Not.Null);

        float[] expectedAngles =
        {
            -30f,
            -15f,
            0f,
            15f,
            30f
        };

        for (int index = 0; index < expectedAngles.Length; index++)
        {
            Vector2 direction =
                (Vector2)calculateDirection.Invoke(
                    null,
                    new object[]
                    {
                        Vector2.right,
                        index,
                        expectedAngles.Length,
                        60f
                    }
                );

            Assert.That(
                Vector2.SignedAngle(Vector2.right, direction),
                Is.EqualTo(expectedAngles[index]).Within(0.001f)
            );
        }
    }

    [Test]
    public void DefaultPatternUsesFiveProjectilesAcrossSixtyDegrees()
    {
        Assert.That(MothFanAttackType, Is.Not.Null);

        GameObject moth = new GameObject("Moth Fan Attack Test");

        try
        {
            Component attack =
                moth.AddComponent(MothFanAttackType);

            Assert.That(
                GetProperty<int>(attack, "ProjectileCount"),
                Is.EqualTo(5)
            );
            Assert.That(
                GetProperty<float>(attack, "SpreadAngle"),
                Is.EqualTo(60f)
            );
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(moth);
        }
    }

    private static T GetProperty<T>(
        object target,
        string propertyName
    )
    {
        PropertyInfo property =
            target.GetType().GetProperty(propertyName);

        Assert.That(property, Is.Not.Null);
        return (T)property.GetValue(target);
    }
}
