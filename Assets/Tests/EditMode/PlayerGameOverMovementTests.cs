using System;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class PlayerGameOverMovementTests
{
    private static readonly Type GameManagerType =
        Type.GetType("GameManager, Assembly-CSharp");

    private static readonly Type PlayerExperienceType =
        Type.GetType("PlayerExperience, Assembly-CSharp");

    private static readonly Type PlayerMentalType =
        Type.GetType("PlayerMental, Assembly-CSharp");

    private static readonly Type PlayerMovementType =
        Type.GetType("PlayerMovement, Assembly-CSharp");

    private GameObject managerObject;
    private GameObject playerObject;
    private Component gameManager;
    private Component playerMental;
    private Component playerMovement;
    private Rigidbody2D playerBody;

    [SetUp]
    public void SetUp()
    {
        Assert.That(GameManagerType, Is.Not.Null);
        Assert.That(PlayerExperienceType, Is.Not.Null);
        Assert.That(PlayerMentalType, Is.Not.Null);
        Assert.That(PlayerMovementType, Is.Not.Null);

        managerObject = new GameObject(
            "Game Over Movement Test Manager"
        );
        managerObject.SetActive(false);
        Component playerExperience = managerObject.AddComponent(
            PlayerExperienceType
        );
        gameManager = managerObject.AddComponent(GameManagerType);

        Invoke(playerExperience, "Awake");
        Invoke(gameManager, "Awake");
        managerObject.SetActive(true);
        Invoke(gameManager, "StartGame");

        playerObject = new GameObject(
            "Game Over Movement Test Player"
        );
        playerObject.SetActive(false);
        playerMovement = playerObject.AddComponent(
            PlayerMovementType
        );
        playerMental = playerObject.AddComponent(PlayerMentalType);
        playerBody = playerObject.GetComponent<Rigidbody2D>();

        SetField(playerMovement, "body", playerBody);
        SetField(
            playerMovement,
            "inputActions",
            AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(
                "Assets/Scripts/Player/PlayerInput.inputactions"
            )
        );

        Animator animator = playerObject.GetComponent<Animator>();
        animator.runtimeAnimatorController =
            AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(
                "Assets/Sprites/Red/Red.controller"
            );

        Component levelStats = playerObject.GetComponent(
            Type.GetType("PlayerLevelStats, Assembly-CSharp")
        );
        Invoke(levelStats, "RecalculateStats", 1);
        Invoke(playerMental, "Awake");
        Invoke(playerMental, "ResetMental");
        playerObject.SetActive(true);
    }

    [TearDown]
    public void TearDown()
    {
        Time.timeScale = 1f;
        UnityEngine.Object.DestroyImmediate(playerObject);
        UnityEngine.Object.DestroyImmediate(managerObject);
    }

    [Test]
    public void MentalDepletionStopsInputKnockbackAndVelocity()
    {
        Vector2 startingPosition = new Vector2(2f, 3f);
        playerObject.transform.position = startingPosition;
        playerBody.position = startingPosition;
        playerBody.linearVelocity = new Vector2(5f, -4f);
        SetField(playerMovement, "moveInput", Vector2.right);
        SetField(playerMovement, "knockbackDirection", Vector2.up);
        SetField(playerMovement, "knockbackTimeRemaining", 1f);

        Assert.That(playerBody.position, Is.EqualTo(startingPosition));

        Invoke(playerMental, "SetMental", 0f);

        Assert.That(
            GetProperty<object>(gameManager, "CurrentState").ToString(),
            Is.EqualTo("GameOver")
        );

        Invoke(playerMovement, "FixedUpdate");

        Assert.That(playerBody.position, Is.EqualTo(startingPosition));
        Assert.That(playerBody.linearVelocity, Is.EqualTo(Vector2.zero));
        Assert.That(
            GetField<Vector2>(playerMovement, "moveInput"),
            Is.EqualTo(Vector2.zero)
        );
        Assert.That(
            GetField<float>(
                playerMovement,
                "knockbackTimeRemaining"
            ),
            Is.EqualTo(0f)
        );
    }

    [Test]
    public void PauseStopsMotionWithoutDiscardingPendingKnockback()
    {
        playerBody.linearVelocity = Vector2.right * 5f;
        SetField(playerMovement, "moveInput", Vector2.right);
        SetField(playerMovement, "knockbackDirection", Vector2.up);
        SetField(playerMovement, "knockbackTimeRemaining", 1f);

        Invoke(gameManager, "PauseGame");
        Invoke(playerMovement, "Update");

        Assert.That(playerBody.linearVelocity, Is.EqualTo(Vector2.zero));
        Assert.That(
            GetField<Vector2>(playerMovement, "moveInput"),
            Is.EqualTo(Vector2.zero)
        );
        Assert.That(
            GetField<float>(
                playerMovement,
                "knockbackTimeRemaining"
            ),
            Is.EqualTo(1f)
        );

        Invoke(gameManager, "ResumeGame");
        Invoke(playerMovement, "FixedUpdate");

        Assert.That(
            GetField<float>(
                playerMovement,
                "knockbackTimeRemaining"
            ),
            Is.LessThan(1f)
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

    private static T GetField<T>(
        object target,
        string fieldName
    )
    {
        FieldInfo field = target.GetType().GetField(
            fieldName,
            BindingFlags.Instance |
            BindingFlags.NonPublic
        );

        Assert.That(field, Is.Not.Null, fieldName);
        return (T)field.GetValue(target);
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
