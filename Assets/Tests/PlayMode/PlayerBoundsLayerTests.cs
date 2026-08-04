using NUnit.Framework;
using UnityEngine;

public sealed class PlayerBoundsLayerTests
{
    [Test]
    public void PlayerBoundsOnlyCollidesWithPlayerLayer()
    {
        int playerLayer = LayerMask.NameToLayer("Player");
        int playerBoundsLayer = LayerMask.NameToLayer("PlayerBounds");
        int monsterLayer = LayerMask.NameToLayer("Default");

        Assert.That(playerLayer, Is.GreaterThanOrEqualTo(0));
        Assert.That(
            playerBoundsLayer,
            Is.GreaterThanOrEqualTo(0),
            "PlayerBounds layer must exist."
        );
        Assert.That(
            Physics2D.GetIgnoreLayerCollision(
                playerLayer,
                playerBoundsLayer
            ),
            Is.False,
            "Player must collide with PlayerBounds."
        );
        Assert.That(
            Physics2D.GetIgnoreLayerCollision(
                monsterLayer,
                playerBoundsLayer
            ),
            Is.True,
            "Default-layer monsters must pass through PlayerBounds."
        );
    }

    [Test]
    public void PlayerStopsWhileMonsterPassesThroughBoundary()
    {
        SimulationMode2D previousMode = Physics2D.simulationMode;
        GameObject boundaryObject = null;
        GameObject playerObject = null;
        GameObject monsterObject = null;

        try
        {
            Physics2D.simulationMode = SimulationMode2D.Script;

            boundaryObject = new GameObject("PlayerBounds QA Edge");
            boundaryObject.layer = LayerMask.NameToLayer("PlayerBounds");
            EdgeCollider2D boundary =
                boundaryObject.AddComponent<EdgeCollider2D>();
            boundary.points = new[]
            {
                new Vector2(0f, -1f),
                new Vector2(0f, 3f)
            };

            playerObject = CreateMover(
                "Player QA Body",
                LayerMask.NameToLayer("Player"),
                new Vector2(-1f, 0f)
            );
            monsterObject = CreateMover(
                "Monster QA Body",
                LayerMask.NameToLayer("Default"),
                new Vector2(-1f, 2f)
            );

            Physics2D.SyncTransforms();
            for (int step = 0; step < 30; step++)
            {
                Physics2D.Simulate(0.02f);
            }

            Assert.That(
                playerObject.transform.position.x,
                Is.LessThan(0f),
                "Player must remain inside PlayerBounds."
            );
            Assert.That(
                monsterObject.transform.position.x,
                Is.GreaterThan(0f),
                "Monster must cross PlayerBounds."
            );
        }
        finally
        {
            Physics2D.simulationMode = previousMode;
            Object.DestroyImmediate(boundaryObject);
            Object.DestroyImmediate(playerObject);
            Object.DestroyImmediate(monsterObject);
        }
    }

    private static GameObject CreateMover(
        string name,
        int layer,
        Vector2 position
    )
    {
        GameObject mover = new(name)
        {
            layer = layer
        };
        mover.transform.position = position;
        mover.AddComponent<CircleCollider2D>().radius = 0.25f;
        Rigidbody2D body = mover.AddComponent<Rigidbody2D>();
        body.gravityScale = 0f;
        body.linearVelocity = Vector2.right * 5f;
        return mover;
    }
}
