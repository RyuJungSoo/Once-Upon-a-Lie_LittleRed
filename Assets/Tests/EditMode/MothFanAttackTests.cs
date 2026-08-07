using System;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class MothFanAttackTests
{
    private static readonly Type MothFanAttackType =
        Type.GetType("MothFanAttack, Assembly-CSharp");
    private static readonly Type MonsterChaseType =
        Type.GetType("MonsterChase, Assembly-CSharp");
    private static readonly Type MonsterHealthType =
        Type.GetType("MonsterHealth, Assembly-CSharp");

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

    [Test]
    public void AttackHoldPreventsRigidbodyDisplacementUntilChaseResumes()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/Sprites/Monster/Moth/Moth.prefab"
        );
        GameObject moth = UnityEngine.Object.Instantiate(prefab);
        GameObject player = new GameObject("Moth Collision Test Player");
        SimulationMode2D previousSimulationMode =
            Physics2D.simulationMode;

        try
        {
            Component attack = moth.GetComponent(MothFanAttackType);
            Behaviour chase =
                (Behaviour)moth.GetComponent(MonsterChaseType);
            Component health = moth.GetComponent(MonsterHealthType);
            Rigidbody2D body = moth.GetComponent<Rigidbody2D>();
            object stats = GetProperty<object>(health, "Stats");
            object settings = GetProperty<object>(
                stats,
                "RangedAttack"
            );
            float attackRange = GetProperty<float>(
                settings,
                "AttackRange"
            );
            float resumePadding = GetProperty<float>(
                settings,
                "ResumeRangePadding"
            );
            RigidbodyConstraints2D originalConstraints =
                body.constraints;

            Invoke(attack, "Awake");
            SetField(chase, "target", player.transform);

            player.transform.position =
                moth.transform.position +
                Vector3.right * (attackRange - 0.1f);

            Invoke(attack, "Update");

            Assert.That(chase.enabled, Is.False);
            Assert.That(
                body.constraints &
                RigidbodyConstraints2D.FreezePosition,
                Is.EqualTo(RigidbodyConstraints2D.FreezePosition)
            );

            Physics2D.simulationMode = SimulationMode2D.Script;
            Vector2 positionBeforeImpulse = body.position;

            body.linearVelocity = Vector2.left * 5f;
            Physics2D.Simulate(0.1f);

            Assert.That(body.position, Is.EqualTo(positionBeforeImpulse));

            player.transform.position =
                moth.transform.position +
                Vector3.right *
                (attackRange + resumePadding + 0.1f);

            Invoke(attack, "Update");

            Assert.That(chase.enabled, Is.True);
            Assert.That(body.constraints, Is.EqualTo(originalConstraints));
        }
        finally
        {
            Physics2D.simulationMode = previousSimulationMode;
            UnityEngine.Object.DestroyImmediate(player);
            UnityEngine.Object.DestroyImmediate(moth);
        }
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
                BindingFlags.Instance | BindingFlags.NonPublic
            );

        Assert.That(field, Is.Not.Null);
        field.SetValue(target, value);
    }

    private static object Invoke(
        object target,
        string methodName
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
        return method.Invoke(target, null);
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
