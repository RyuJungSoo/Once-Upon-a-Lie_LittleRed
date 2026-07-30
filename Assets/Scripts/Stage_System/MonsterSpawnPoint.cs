using UnityEngine;

[DisallowMultipleComponent]
public sealed class MonsterSpawnPoint : MonoBehaviour
{
    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 0.25f, 0.15f, 0.9f);
        Gizmos.DrawWireSphere(transform.position, 0.45f);
        Gizmos.DrawLine(
            transform.position + Vector3.left * 0.65f,
            transform.position + Vector3.right * 0.65f
        );
        Gizmos.DrawLine(
            transform.position + Vector3.down * 0.65f,
            transform.position + Vector3.up * 0.65f
        );
    }
}
