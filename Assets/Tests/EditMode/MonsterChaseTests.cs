using System;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public sealed class MonsterChaseTests
{
    private static readonly Type MonsterChaseType =
        Type.GetType("MonsterChase, Assembly-CSharp");

    private static readonly Type MonsterAttackType =
        Type.GetType("MonsterAttack, Assembly-CSharp");

    private static readonly Type PigChargeAttackType =
        Type.GetType("PigChargeAttack, Assembly-CSharp");

    private static readonly Type SignAttackType =
        Type.GetType(
            "SignZigzagAttack, Assembly-CSharp"
        );

    private static readonly Type MonsterAppearanceType =
        Type.GetType("MonsterSanityAppearance, Assembly-CSharp");

    private static readonly Type MonsterHealthType =
        Type.GetType("MonsterHealth, Assembly-CSharp");

    private GameObject playerObject;
    private GameObject monsterObject;
    private Component monsterChase;
    private Component monsterAttack;
    private Component monsterAppearance;

    [SetUp]
    public void SetUp()
    {
        Assert.That(MonsterChaseType, Is.Not.Null);
        Assert.That(MonsterAttackType, Is.Not.Null);
        Assert.That(PigChargeAttackType, Is.Not.Null);
        Assert.That(SignAttackType, Is.Not.Null);
        Assert.That(MonsterAppearanceType, Is.Not.Null);
        Assert.That(MonsterHealthType, Is.Not.Null);

        ResetCachedTarget();

        playerObject = new GameObject("Test Player")
        {
            tag = "Player"
        };

        monsterObject = new GameObject("Test Monster");
        monsterObject.SetActive(false);
        monsterObject.AddComponent<SpriteRenderer>();

        Animator animator = monsterObject.AddComponent<Animator>();
        animator.runtimeAnimatorController =
            AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(
                "Assets/Sprites/Monster/Rabbit/Rabbit.controller"
            );

        monsterObject.AddComponent<BoxCollider2D>();
        monsterObject.AddComponent(MonsterHealthType);
        monsterObject.AddComponent<Rigidbody2D>();
        monsterAppearance = monsterObject.AddComponent(
            MonsterAppearanceType
        );
        monsterChase = monsterObject.AddComponent(MonsterChaseType);
        monsterAttack = monsterObject.AddComponent(MonsterAttackType);

        Invoke(monsterAppearance, "Awake");
        Invoke(monsterChase, "Awake");
        Invoke(monsterAttack, "Awake");

        monsterObject.SetActive(true);
    }

    [TearDown]
    public void TearDown()
    {
        UnityEngine.Object.DestroyImmediate(monsterObject);
        UnityEngine.Object.DestroyImmediate(playerObject);
        ResetCachedTarget();
    }

    [Test]
    public void ResolveTargetUsesPlayerTaggedGameObject()
    {
        Invoke(monsterChase, "ResolveTarget");

        Transform target = (Transform)GetField(monsterChase, "target");

        Assert.That(target, Is.SameAs(playerObject.transform));
    }

    [Test]
    public void ChaseDisablesDuplicateTransformMovementDetection()
    {
        Assert.That(
            GetField(monsterAppearance, "detectMovementAutomatically"),
            Is.False
        );
    }

    [Test]
    public void MovementInputDoesNotOverrideAttackAndRestoresLatestState()
    {
        Type motionStateType = MonsterAppearanceType.GetNestedType(
            "MonsterMotionState",
            BindingFlags.Public
        );
        object attackState = Enum.Parse(motionStateType, "Attack");

        Invoke(monsterAppearance, "SetMotionState", attackState);
        Invoke(monsterAppearance, "SetMoving", false);

        Assert.That(
            GetProperty(monsterAppearance, "CurrentMotionState").ToString(),
            Is.EqualTo("Attack")
        );

        Invoke(monsterAppearance, "RestoreMovementMotionState");

        Assert.That(
            GetProperty(monsterAppearance, "CurrentMotionState").ToString(),
            Is.EqualTo("Idle")
        );

        Invoke(monsterAppearance, "SetMotionState", attackState);
        Invoke(monsterAppearance, "SetMoving", true);

        Assert.That(
            GetProperty(monsterAppearance, "CurrentMotionState").ToString(),
            Is.EqualTo("Attack")
        );

        Invoke(monsterAppearance, "RestoreMovementMotionState");

        Assert.That(
            GetProperty(monsterAppearance, "CurrentMotionState").ToString(),
            Is.EqualTo("Run")
        );
    }

    [Test]
    public void AttackAnimationRestoresLatestMovementState()
    {
        Type motionStateType = MonsterAppearanceType.GetNestedType(
            "MonsterMotionState",
            BindingFlags.Public
        );
        object attackState = Enum.Parse(motionStateType, "Attack");

        Invoke(monsterAppearance, "SetMoving", false);
        Invoke(monsterAppearance, "SetMotionState", attackState);
        SetField(monsterAttack, "attackAnimationEndTime", 0f);
        Invoke(monsterAttack, "Update");

        Assert.That(
            GetProperty(monsterAppearance, "CurrentMotionState").ToString(),
            Is.EqualTo("Idle")
        );

        Invoke(monsterAppearance, "SetMoving", true);
        Invoke(monsterAppearance, "SetMotionState", attackState);
        SetField(monsterAttack, "attackAnimationEndTime", 0f);
        Invoke(monsterAttack, "Update");

        Assert.That(
            GetProperty(monsterAppearance, "CurrentMotionState").ToString(),
            Is.EqualTo("Run")
        );
    }

    [TestCase("Bird")]
    [TestCase("Blanket")]
    [TestCase("DeerKing")]
    [TestCase("FlowerFairy")]
    [TestCase("Grandma")]
    [TestCase("Hunter")]
    [TestCase("Pig")]
    [TestCase("Rabbit")]
    [TestCase("RedString")]
    [TestCase("Sign")]
    [TestCase("TeaCup")]
    public void MonsterPrefabHasAttackPatternAndCompleteAnimator(
        string monsterName
    )
    {
        string folder = $"Assets/Sprites/Monster/{monsterName}";
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            $"{folder}/{monsterName}.prefab"
        );
        AnimatorController controller =
            AssetDatabase.LoadAssetAtPath<AnimatorController>(
                $"{folder}/{monsterName}.controller"
            );

        Assert.That(prefab, Is.Not.Null);
        Assert.That(controller, Is.Not.Null);
        Assert.That(prefab.GetComponent<Rigidbody2D>(), Is.Not.Null);
        Assert.That(prefab.GetComponent(MonsterChaseType), Is.Not.Null);

        if (monsterName == "Pig")
        {
            Assert.That(
                prefab.GetComponent(MonsterAttackType),
                Is.Null
            );
            Assert.That(
                prefab.GetComponent(PigChargeAttackType),
                Is.Not.Null
            );
        }
        else if (monsterName == "Sign")
        {
            Assert.That(
                prefab.GetComponent(MonsterAttackType),
                Is.Null
            );
            Assert.That(
                prefab.GetComponent(SignAttackType),
                Is.Not.Null
            );
        }
        else
        {
            Assert.That(
                prefab.GetComponent(MonsterAttackType),
                Is.Not.Null
            );
        }

        Assert.That(
            prefab.GetComponent<Animator>().runtimeAnimatorController,
            Is.SameAs(controller)
        );

        SerializedObject health = new SerializedObject(
            prefab.GetComponent(MonsterHealthType)
        );
        Assert.That(
            health.FindProperty("stats").objectReferenceValue,
            Is.Not.Null
        );

        Assert.That(
            Array.Exists(
                controller.parameters,
                parameter =>
                    parameter.name == "SanityStage" &&
                    parameter.type ==
                        AnimatorControllerParameterType.Int
            ),
            Is.True
        );
        Assert.That(
            Array.Exists(
                controller.parameters,
                parameter =>
                    parameter.name == "MotionState" &&
                    parameter.type ==
                        AnimatorControllerParameterType.Int
            ),
            Is.True
        );
        Assert.That(
            controller.layers[0].stateMachine.anyStateTransitions.Length,
            Is.EqualTo(9)
        );

        string[] motionNames = { "idle", "run", "attack" };
        string[] sanityNames = { "high", "medium", "low" };

        foreach (string motionName in motionNames)
        {
            foreach (string sanityName in sanityNames)
            {
                string expectedState =
                    $"Side_{motionName}_{sanityName}";
                ChildAnimatorState[] states =
                    controller.layers[0].stateMachine.states;

                Assert.That(
                    Array.Exists(
                        states,
                        state => state.state.name == expectedState
                    ),
                    Is.True,
                    $"{monsterName} is missing {expectedState}."
                );
            }
        }
    }

    private static void ResetCachedTarget()
    {
        FieldInfo field = MonsterChaseType.GetField(
            "cachedPlayerTarget",
            BindingFlags.Static | BindingFlags.NonPublic
        );

        Assert.That(field, Is.Not.Null);
        field.SetValue(null, null);
    }

    private static object GetField(object target, string fieldName)
    {
        FieldInfo field = target
            .GetType()
            .GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic
            );

        Assert.That(field, Is.Not.Null);
        return field.GetValue(target);
    }

    private static object GetProperty(object target, string propertyName)
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
