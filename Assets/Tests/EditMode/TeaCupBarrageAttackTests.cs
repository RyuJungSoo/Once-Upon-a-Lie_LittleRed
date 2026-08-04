using System;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
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
    public void SupportMovementMaintainsFiringDistance()
    {
        Assert.That(TeaCupBarrageAttackType, Is.Not.Null);
        MethodInfo calculateDirection =
            TeaCupBarrageAttackType.GetMethod(
                "CalculateSupportDirection",
                BindingFlags.Static |
                BindingFlags.NonPublic
            );

        Assert.That(calculateDirection, Is.Not.Null);

        AssertDirection(
            calculateDirection,
            new Vector2(5f, 0f),
            Vector2.right
        );
        AssertDirection(
            calculateDirection,
            new Vector2(2f, 0f),
            Vector2.left
        );
        AssertDirection(
            calculateDirection,
            new Vector2(3.5f, 0f),
            Vector2.up
        );
    }

    [Test]
    public void PrefabMovesSlowlyWhileBarrageRemainsActive()
    {
        GameObject prefab =
            AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Sprites/Monster/TeaCup/TeaCup.prefab"
            );
        Type gameManagerType =
            Type.GetType("GameManager, Assembly-CSharp");
        PropertyInfo singletonInstance =
            gameManagerType?.BaseType?.GetProperty(
                "Instance",
                BindingFlags.Static |
                BindingFlags.Public
            );
        MethodInfo singletonSetter =
            singletonInstance?.GetSetMethod(true);
        object previousGameManager =
            singletonInstance?.GetValue(null);
        GameObject target = new("TeaCup Support Target");
        GameObject teaCup = null;

        try
        {
            singletonSetter?.Invoke(
                null,
                new object[] { null }
            );
            teaCup = UnityEngine.Object.Instantiate(prefab);
            Component attack =
                teaCup.GetComponent(TeaCupBarrageAttackType);
            Component chase =
                teaCup.GetComponent(MonsterChaseType);
            Rigidbody2D body =
                teaCup.GetComponent<Rigidbody2D>();

            SetField(chase, "target", target.transform);
            Invoke(attack, "Awake");
            Invoke(attack, "OnEnable");
            target.transform.position =
                teaCup.transform.position + Vector3.right * 5f;
            Invoke(attack, "MoveForSupport");

            Assert.That(attack, Is.Not.Null);
            Assert.That(((Behaviour)attack).enabled, Is.True);
            Assert.That(((Behaviour)chase).enabled, Is.False);
            Assert.That(body.linearVelocity.x, Is.EqualTo(0.7f));
            Assert.That(body.linearVelocity.y, Is.Zero);
        }
        finally
        {
            if (teaCup != null)
            {
                UnityEngine.Object.DestroyImmediate(teaCup);
            }

            UnityEngine.Object.DestroyImmediate(target);
            singletonSetter?.Invoke(
                null,
                new[] { previousGameManager }
            );
        }
    }

    private static void AssertDirection(
        MethodInfo calculateDirection,
        Vector2 targetOffset,
        Vector2 expectedDirection
    )
    {
        Vector2 direction =
            (Vector2)calculateDirection.Invoke(
                null,
                new object[]
                {
                    targetOffset,
                    5f,
                    0.7f,
                    0.1f,
                    1f
                }
            );

        Assert.That(direction, Is.EqualTo(expectedDirection));
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

        Assert.That(field, Is.Not.Null);
        field.SetValue(target, value);
    }

    private static void Invoke(
        object target,
        string methodName
    )
    {
        MethodInfo method = target.GetType().GetMethod(
            methodName,
            BindingFlags.Instance |
            BindingFlags.NonPublic
        );

        Assert.That(method, Is.Not.Null);
        method.Invoke(target, null);
    }
}
