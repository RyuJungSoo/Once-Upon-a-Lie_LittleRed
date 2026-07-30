using System;
using UnityEngine;

[Serializable]
public sealed class MonsterSpawnEntry
{
    [SerializeField]
    private GameObject prefab;

    [SerializeField, Min(0f)]
    private float weight = 1f;

    public GameObject Prefab => prefab;
    public float Weight => weight;

    public MonsterSpawnEntry(GameObject prefab, float weight = 1f)
    {
        this.prefab = prefab;
        this.weight = Mathf.Max(0f, weight);
    }
}
