using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class PlayerDashTests
{
    private static readonly Type PlayerDashType =
        Type.GetType("PlayerDash, Assembly-CSharp");

    private GameObject playerObject;
    private Component playerDash;
    private Rigidbody2D playerBody;
    private GameObject cameraObject;

    [SetUp]
    public void SetUp()
    {
        Assert.That(PlayerDashType, Is.Not.Null);

        playerObject = new GameObject("Player Dash Test Player");
        playerObject.SetActive(false);
        playerDash = playerObject.AddComponent(PlayerDashType);
        playerBody = playerObject.GetComponent<Rigidbody2D>();

        cameraObject = new GameObject("Player Dash Test Camera");
        Camera aimCamera = cameraObject.AddComponent<Camera>();

        SetField(playerDash, "aimCamera", aimCamera);
        SetField(playerDash, "dashDistance", 2.5f);
        SetField(playerDash, "dashInterval", 0.5f);
        LogAssert.Expect(
            LogType.Error,
            "PlayerDash references are not fully assigned."
        );
        playerObject.SetActive(true);
    }

    [TearDown]
    public void TearDown()
    {
        UnityEngine.Object.DestroyImmediate(playerObject);
        UnityEngine.Object.DestroyImmediate(cameraObject);
    }

    [Test]
    public void TryDashMovesExactDistanceTowardWorldPosition()
    {
        Vector2 startingPosition = new(1f, 2f);
        playerBody.position = startingPosition;

        bool didDash = (bool)Invoke(
            playerDash,
            "TryDash",
            new Vector2(4f, 6f)
        );

        Vector2 expectedDirection = new Vector2(3f, 4f).normalized;
        Vector2 expectedPosition =
            startingPosition + expectedDirection * 2.5f;

        Assert.That(didDash, Is.True);
        Assert.That(
            Vector2.Distance(playerBody.position, expectedPosition),
            Is.LessThan(0.0001f)
        );
    }

    [Test]
    public void TryDashIgnoresPointerAtPlayerPosition()
    {
        Vector2 startingPosition = new(-2f, 3f);
        playerBody.position = startingPosition;

        bool didDash = (bool)Invoke(
            playerDash,
            "TryDash",
            startingPosition
        );

        Assert.That(didDash, Is.False);
        Assert.That(playerBody.position, Is.EqualTo(startingPosition));
    }

    [Test]
    public void TryDashBlocksUntilIntervalExpires()
    {
        playerBody.position = Vector2.zero;

        bool firstDash = (bool)Invoke(
            playerDash,
            "TryDash",
            Vector2.right
        );
        Vector2 positionAfterFirstDash = playerBody.position;

        bool immediateDash = (bool)Invoke(
            playerDash,
            "TryDash",
            positionAfterFirstDash + Vector2.up
        );

        Assert.That(firstDash, Is.True);
        Assert.That(immediateDash, Is.False);
        Assert.That(
            playerBody.position,
            Is.EqualTo(positionAfterFirstDash)
        );

        SetField(playerDash, "nextDashTime", Time.time - 0.01f);

        bool dashAfterInterval = (bool)Invoke(
            playerDash,
            "TryDash",
            positionAfterFirstDash + Vector2.up
        );

        Assert.That(dashAfterInterval, Is.True);
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
