using System;
using UnityEngine;

[Serializable]
public sealed class DialogueEntryData
{
    [SerializeField] private string id;
    [SerializeField] private string language;
    [SerializeField] private string groupId;
    [SerializeField] private int sequence;
    [SerializeField] private string speaker;

    [TextArea(3, 8)]
    [SerializeField] private string text;

    public string Id => id;
    public string Language => language;
    public string GroupId => groupId;
    public int Sequence => sequence;
    public string Speaker => speaker;
    public string Text => text;

    public DialogueEntryData(
        string id,
        string language,
        string groupId,
        int sequence,
        string speaker,
        string text)
    {
        this.id = id;
        this.language = language;
        this.groupId = groupId;
        this.sequence = sequence;
        this.speaker = speaker;
        this.text = text;
    }
}