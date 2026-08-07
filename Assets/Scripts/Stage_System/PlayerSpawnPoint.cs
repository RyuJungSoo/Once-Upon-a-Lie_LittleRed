using System;
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public sealed class PlayerSpawnPoint : MonoBehaviour
{
    public static event Action<Scene> PlayerSpawned;

    public void Spawn(Transform player)
    {
        if (player == null)
        {
            Debug.LogError(
                $"[{nameof(PlayerSpawnPoint)}] " +
                "스폰할 Player가 없습니다.",
                this
            );
            return;
        }

        player.SetPositionAndRotation(
            transform.position,
            transform.rotation
        );

        if (player.TryGetComponent(
                out Rigidbody2D playerBody))
        {
            playerBody.position = transform.position;
            playerBody.rotation =
                transform.eulerAngles.z;
            playerBody.linearVelocity = Vector2.zero;
            playerBody.angularVelocity = 0f;
        }

        PlayerSpawned?.Invoke(gameObject.scene);
    }

    public static bool TrySpawn(
        Transform player,
        Scene scene
    )
    {
        if (player == null ||
            !scene.IsValid() ||
            !scene.isLoaded)
        {
            return false;
        }

        foreach (GameObject root in
                 scene.GetRootGameObjects())
        {
            PlayerSpawnPoint spawnPoint =
                root.GetComponentInChildren<PlayerSpawnPoint>(
                    true
                );

            if (spawnPoint == null)
            {
                continue;
            }

            spawnPoint.Spawn(player);
            return true;
        }

        Debug.LogError(
            $"[{nameof(PlayerSpawnPoint)}] " +
            $"{scene.name} 씬에서 PlayerSpawnPoint를 " +
            "찾을 수 없습니다."
        );
        return false;
    }
}
