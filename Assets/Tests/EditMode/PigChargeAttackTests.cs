using System;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class PigChargeAttackTests
{
    private static readonly Type AimedChargeAttackType =
        Type.GetType(
            "MonsterAimedChargeAttack, Assembly-CSharp"
        );

    private static readonly Type MonsterAttackType =
        Type.GetType("MonsterAttack, Assembly-CSharp");

    private static readonly Type MonsterChaseType =
        Type.GetType("MonsterChase, Assembly-CSharp");

    [Test]
    public void PigPrefabUsesReusableAutomaticChargeAttack()
    {
        Assert.That(AimedChargeAttackType, Is.Not.Null);
        Assert.That(
            Type.GetType("PigChargeAttack, Assembly-CSharp"),
            Is.Null
        );
        Assert.That(MonsterAttackType, Is.Not.Null);
        Assert.That(MonsterChaseType, Is.Not.Null);

        GameObject prefab =
            AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Sprites/Monster/Pig/Pig.prefab"
            );

        Assert.That(prefab, Is.Not.Null);
        Assert.That(
            prefab.GetComponent(AimedChargeAttackType),
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
            prefab.GetComponent(AimedChargeAttackType);
        SerializedObject serializedPattern =
            new SerializedObject(pattern);

        Assert.That(
            serializedPattern
                .FindProperty("chargeRange")
                .floatValue,
            Is.EqualTo(4f)
        );
        Assert.That(
            serializedPattern
                .FindProperty("aimDuration")
                .floatValue,
            Is.EqualTo(0.65f)
        );
        Assert.That(
            serializedPattern
                .FindProperty("chargeSpeed")
                .floatValue,
            Is.EqualTo(8f)
        );
        Assert.That(
            serializedPattern
                .FindProperty("chargeDuration")
                .floatValue,
            Is.EqualTo(0.75f)
        );
        Assert.That(
            serializedPattern
                .FindProperty("recoveryDuration")
                .floatValue,
            Is.EqualTo(0.65f)
        );
        Assert.That(
            serializedPattern
                .FindProperty("chargeCooldown")
                .floatValue,
            Is.EqualTo(1.5f)
        );
        Assert.That(
            serializedPattern
                .FindProperty("automaticActivation")
                .boolValue,
            Is.True
        );
    }

    [Test]
    public void ChargeDirectionStaysLockedAfterAiming()
    {
        Assert.That(AimedChargeAttackType, Is.Not.Null);

        GameObject prefab =
            AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Sprites/Monster/Pig/Pig.prefab"
            );
        GameObject pig =
            (GameObject)PrefabUtility.InstantiatePrefab(
                prefab
            );
        GameObject target =
            new GameObject("Test Pig Charge Target");

        try
        {
            Component pattern =
                pig.GetComponent(AimedChargeAttackType);
            Rigidbody2D body =
                pig.GetComponent<Rigidbody2D>();

            pig.transform.position = Vector3.zero;
            target.transform.position =
                new Vector3(3f, 4f, 0f);

            Invoke(pattern, "Awake");
            Invoke(
                pattern,
                "SetTarget",
                target.transform
            );
            Invoke(pattern, "BeginAiming");
            Invoke(pattern, "UpdateAimDirection");
            Invoke(pattern, "BeginCharge");

            Vector2 lockedDirection =
                GetProperty<Vector2>(
                    pattern,
                    "ChargeDirection"
                );
            Vector2 initialVelocity =
                body.linearVelocity;

            target.transform.position =
                new Vector3(-4f, -3f, 0f);

            Invoke(pattern, "ApplyChargeMovement");

            Assert.That(
                lockedDirection.x,
                Is.EqualTo(0.6f).Within(0.001f)
            );
            Assert.That(
                lockedDirection.y,
                Is.EqualTo(0.8f).Within(0.001f)
            );
            Assert.That(
                GetProperty<Vector2>(
                    pattern,
                    "ChargeDirection"
                ),
                Is.EqualTo(lockedDirection)
            );
            Assert.That(
                body.linearVelocity,
                Is.EqualTo(initialVelocity)
            );
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(target);
            UnityEngine.Object.DestroyImmediate(pig);
        }
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
