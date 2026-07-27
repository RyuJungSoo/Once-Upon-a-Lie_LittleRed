using System;
using UnityEngine;

[Serializable]
public sealed class CutsceneAssetData
{
    [SerializeField] private string groupId;
    [SerializeField] private Sprite sprite;

    public string GroupId => groupId;
    public Sprite Sprite => sprite;
}