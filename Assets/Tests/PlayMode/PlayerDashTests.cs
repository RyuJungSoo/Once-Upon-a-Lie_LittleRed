using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class PlayerDashTests
{
    private static readonly Type PlayerDashType =
        Type.GetType("PlayerDash, Assembly-CSharp");

    private static readonly Type PlayerMovementType =
        Type.GetType("PlayerMovement, Assembly-CSharp");

    private GameObject playerObject;
    private Component playerDash;
    private Component playerMovement;
    private Rigidbody2D playerBody;

    [SetUp]
    public void SetUp()
    {
        Assert.That(PlayerDashType, Is.Not.Null);
        Assert.That(PlayerMovementType, Is.Not.Null);

        playerObject = new GameObject("Player Dash Test Player");
        playerObject.SetActive(false);
        playerDash = playerObject.AddComponent(PlayerDashType);
        playerMovement = playerObject.AddComponent(
            PlayerMovementType
        );
        playerBody = playerObject.GetComponent<Rigidbody2D>();

        SetField(playerDash, "dashDistance", 2.5f);
        SetField(playerDash, "dashInterval", 0.5f);
        SetField(
            playerDash,
            "dashDuration",
            Time.fixedDeltaTime * 2f
        );
        LogAssert.Expect(
            LogType.Error,
            "PlayerDash references are not fully assigned."
        );
        LogAssert.Expect(
            LogType.Error,
            "PlayerInput Input Actions is not assigned to PlayerMovement."
        );
        playerObject.SetActive(true);
    }

    [TearDown]
    public void TearDown()
    {
        UnityEngine.Object.DestroyImmediate(playerObject);
    }

    [Test]
    public void TryDashSnapsToDiagonalAndMovesExactDistance()
    {
        Vector2 startingPosition = new(1f, 2f);
        playerBody.position = startingPosition;

        bool didDash = (bool)Invoke(
            playerDash,
            "TryDash",
            new Vector2(3f, 4f)
        );

        Vector2 expectedDirection = new Vector2(1f, 1f).normalized;
        Vector2 expectedPosition =
            startingPosition + expectedDirection * 2.5f;

        Assert.That(didDash, Is.True);
        Assert.That(playerBody.position, Is.EqualTo(startingPosition));

        Invoke(playerDash, "FixedUpdate");

        Assert.That(
            Vector2.Distance(
                playerBody.position,
                Vector2.Lerp(
                    startingPosition,
                    expectedPosition,
                    0.5f
                )
            ),
            Is.LessThan(0.0001f)
        );

        Invoke(playerDash, "FixedUpdate");

        Assert.That(
            Vector2.Distance(playerBody.position, expectedPosition),
            Is.LessThan(0.0001f)
        );
    }

    [Test]
    public void TryDashIgnoresZeroDirection()
    {
        Vector2 startingPosition = new(-2f, 3f);
        playerBody.position = startingPosition;

        bool didDash = (bool)Invoke(
            playerDash,
            "TryDash",
            Vector2.zero
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
        CompleteDash();
        Vector2 positionAfterFirstDash = playerBody.position;

        bool immediateDash = (bool)Invoke(
            playerDash,
            "TryDash",
            Vector2.up
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
            Vector2.up
        );

        Assert.That(dashAfterInterval, Is.True);
    }

    [Test]
    public void KeyboardInputAndFacingProvideDashDirection()
    {
        SetField(
            playerMovement,
            "moveInput",
            new Vector2(1f, 1f)
        );

        Vector2 keyboardDirection = (Vector2)Invoke(
            playerMovement,
            "GetDashDirection"
        );

        Assert.That(
            Vector2.Distance(
                keyboardDirection,
                new Vector2(1f, 1f).normalized
            ),
            Is.LessThan(0.0001f)
        );

        SetField(playerMovement, "moveInput", Vector2.zero);
        Vector2 facingDirection = (Vector2)Invoke(
            playerMovement,
            "GetDashDirection"
        );

        Assert.That(facingDirection, Is.EqualTo(Vector2.down));
    }

    private void CompleteDash()
    {
        Invoke(playerDash, "FixedUpdate");
        Invoke(playerDash, "FixedUpdate");
        Invoke(playerDash, "FixedUpdate");
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
