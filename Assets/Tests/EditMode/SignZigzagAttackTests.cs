using System;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class SignZigzagAttackTests
{
    private static readonly Type SignAttackType =
        Type.GetType(
            "SignZigzagAttack, Assembly-CSharp"
        );

    private static readonly Type MonsterAttackType =
        Type.GetType("MonsterAttack, Assembly-CSharp");

    private static readonly Type MonsterChaseType =
        Type.GetType("MonsterChase, Assembly-CSharp");

    private static readonly Type MonsterHealthType =
        Type.GetType("MonsterHealth, Assembly-CSharp");

    [Test]
    public void SignPrefabUsesContinuousZigzagAttack()
    {
        Assert.That(SignAttackType, Is.Not.Null);
        Assert.That(MonsterAttackType, Is.Not.Null);
        Assert.That(MonsterChaseType, Is.Not.Null);

        GameObject prefab =
            AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Sprites/Monster/Sign/Sign.prefab"
            );

        Assert.That(prefab, Is.Not.Null);
        Assert.That(
            prefab.GetComponent(SignAttackType),
            Is.Not.Null
        );
        Assert.That(
            prefab.GetComponent(MonsterAttackType),
            Is.Null
        );
        Assert.That(
            prefab.GetComponent(MonsterChaseType),
            Is.Not.Null
        );

        Component pattern =
            prefab.GetComponent(SignAttackType);
        SerializedObject serializedPattern =
            new SerializedObject(pattern);

        AssertPositive(
            serializedPattern,
            "speedMultiplier"
        );
        AssertPositive(
            serializedPattern,
            "zigzagInterval"
        );
        AssertPositive(
            serializedPattern,
            "zigzagStrength"
        );

        Component health =
            prefab.GetComponent(MonsterHealthType);
        object stats = GetProperty(health, "Stats");
        object contactAttack = GetProperty(
            stats,
            "ContactAttack"
        );

        Assert.That(
            (float)GetProperty(
                contactAttack,
                "AttackCooldown"
            ),
            Is.GreaterThan(0f)
        );
        Assert.That(
            (float)GetProperty(
                contactAttack,
                "AttackAnimationDuration"
            ),
            Is.GreaterThanOrEqualTo(0f)
        );
        Assert.That(
            serializedPattern.FindProperty(
                "attackCooldown"
            ),
            Is.Null
        );
        Assert.That(
            serializedPattern.FindProperty(
                "attackAnimationDuration"
            ),
            Is.Null
        );

        Assert.That(
            serializedPattern.FindProperty(
                "chargeRange"
            ),
            Is.Null
        );
        Assert.That(
            serializedPattern.FindProperty(
                "aimDuration"
            ),
            Is.Null
        );
        Assert.That(
            serializedPattern.FindProperty(
                "recoveryDuration"
            ),
            Is.Null
        );
    }

    [Test]
    public void MovementVelocityAlternatesWithoutChargeState()
    {
        Assert.That(SignAttackType, Is.Not.Null);

        GameObject prefab =
            AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Sprites/Monster/Sign/Sign.prefab"
            );
        GameObject sign =
            (GameObject)PrefabUtility.InstantiatePrefab(
                prefab
            );
        GameObject target =
            new GameObject(
                "Test Sign Zigzag Target"
            );

        try
        {
            Component pattern =
                sign.GetComponent(SignAttackType);
            Rigidbody2D body =
                sign.GetComponent<Rigidbody2D>();

            sign.transform.position = Vector3.zero;
            target.transform.position =
                new Vector3(5f, 0f, 0f);

            Invoke(pattern, "Awake");
            Invoke(
                pattern,
                "SetTarget",
                target.transform
            );

            Invoke(
                pattern,
                "ApplyZigzagMovement",
                0
            );
            Vector2 firstVelocity =
                body.linearVelocity;

            Invoke(
                pattern,
                "ApplyZigzagMovement",
                1
            );
            Vector2 secondVelocity =
                body.linearVelocity;

            Assert.That(
                firstVelocity.x,
                Is.GreaterThan(0f)
            );
            Assert.That(
                secondVelocity.x,
                Is.GreaterThan(0f)
            );
            Assert.That(
                firstVelocity.y,
                Is.GreaterThan(0f)
            );
            Assert.That(
                secondVelocity.y,
                Is.LessThan(0f)
            );
            Assert.That(
                firstVelocity.magnitude,
                Is.EqualTo(secondVelocity.magnitude)
                    .Within(0.001f)
            );

            Assert.That(
                SignAttackType.GetMethod(
                    "BeginCharge",
                    BindingFlags.Instance |
                    BindingFlags.NonPublic
                ),
                Is.Null
            );
            Assert.That(
                SignAttackType.GetMethod(
                    "BeginAiming",
                    BindingFlags.Instance |
                    BindingFlags.NonPublic
                ),
                Is.Null
            );
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(
                target
            );
            UnityEngine.Object.DestroyImmediate(
                sign
            );
        }
    }

    private static void AssertPositive(
        SerializedObject serializedObject,
        string propertyName
    )
    {
        SerializedProperty property =
            serializedObject.FindProperty(
                propertyName
            );

        Assert.That(property, Is.Not.Null);
        Assert.That(
            property.floatValue,
            Is.GreaterThan(0f)
        );
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

    private static object GetProperty(
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
        return property.GetValue(target);
    }
}
