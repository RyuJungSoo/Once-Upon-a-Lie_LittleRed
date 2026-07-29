using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public sealed class TeaCupBarrageAttackTests
{
    private static readonly Type TeaCupBarrageAttackType =
        Type.GetType("TeaCupBarrageAttack, Assembly-CSharp");
    private static readonly Type MonsterChaseType =
        Type.GetType("MonsterChase, Assembly-CSharp");

    [Test]
    public void DefaultPatternUsesRapidFiveLaneSweep()
    {
        Assert.That(TeaCupBarrageAttackType, Is.Not.Null);

        GameObject teaCup =
            new GameObject("TeaCup Barrage Attack Test");

        try
        {
            Component attack =
                teaCup.AddComponent(TeaCupBarrageAttackType);

            Assert.That(
                GetProperty<float>(attack, "FireInterval"),
                Is.EqualTo(0.18f)
            );
            Assert.That(
                GetProperty<float>(attack, "SweepAngle"),
                Is.EqualTo(30f)
            );
            Assert.That(
                GetProperty<int>(attack, "SweepLaneCount"),
                Is.EqualTo(5)
            );
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(teaCup);
        }
    }

    [Test]
    public void SweepDirectionsMoveBackAndForthAcrossConfiguredArc()
    {
        Assert.That(TeaCupBarrageAttackType, Is.Not.Null);

        MethodInfo calculateDirection =
            TeaCupBarrageAttackType.GetMethod(
                "CalculateSweepDirection",
                BindingFlags.Static |
                BindingFlags.NonPublic
            );

        Assert.That(calculateDirection, Is.Not.Null);

        float[] expectedAngles =
        {
            -15f,
            -7.5f,
            0f,
            7.5f,
            15f,
            7.5f,
            0f,
            -7.5f
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
                        5,
                        30f
                    }
                );

            Assert.That(
                Vector2.SignedAngle(Vector2.right, direction),
                Is.EqualTo(expectedAngles[index]).Within(0.001f)
            );
        }
    }

    [Test]
    public void BarrageKeepsTeaCupStationary()
    {
        Assert.That(TeaCupBarrageAttackType, Is.Not.Null);
        Assert.That(MonsterChaseType, Is.Not.Null);

        GameObject teaCup =
            new GameObject("TeaCup Stationary Test");

        try
        {
            Component attack =
                teaCup.AddComponent(TeaCupBarrageAttackType);
            Component chase =
                teaCup.GetComponent(MonsterChaseType);
            Rigidbody2D body =
                teaCup.GetComponent<Rigidbody2D>();
            MethodInfo awake =
                TeaCupBarrageAttackType.GetMethod(
                    "Awake",
                    BindingFlags.Instance |
                    BindingFlags.NonPublic
                );
            MethodInfo holdPosition =
                TeaCupBarrageAttackType.GetMethod(
                    "HoldPosition",
                    BindingFlags.Instance |
                    BindingFlags.NonPublic
                );

            Assert.That(chase, Is.Not.Null);
            Assert.That(body, Is.Not.Null);
            Assert.That(awake, Is.Not.Null);
            Assert.That(holdPosition, Is.Not.Null);

            awake.Invoke(attack, null);
            body.linearVelocity = new Vector2(3f, -2f);
            ((Behaviour)chase).enabled = true;
            holdPosition.Invoke(attack, null);

            Assert.That(((Behaviour)chase).enabled, Is.False);
            Assert.That(body.linearVelocity, Is.EqualTo(Vector2.zero));
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(teaCup);
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
