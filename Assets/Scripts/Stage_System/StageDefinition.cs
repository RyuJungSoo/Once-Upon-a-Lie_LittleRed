using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "StageDefinition",
    menuName = "Once Upon a Lie/Stage System/Stage Definition"
)]
public sealed class StageDefinition : ScriptableObject
{
    [Header("Stage")]
    [SerializeField]
    private string stageName = "Stage";

    [SerializeField]
    private bool finalStage;

    [Header("Completion")]
    [SerializeField]
    private bool loadNextSceneAutomatically = true;

    [SerializeField]
    private string nextSceneName;

    [SerializeField, Min(0f)]
    private float nextSceneDelay = 3f;

    [Header("Waves")]
    [SerializeField]
    private List<StageWaveDefinition> waves = new();

    public string StageName => stageName;
    public bool IsFinalStage => finalStage;
    public bool LoadNextSceneAutomatically => loadNextSceneAutomatically;
    public string NextSceneName => nextSceneName;
    public float NextSceneDelay => nextSceneDelay;
    public IReadOnlyList<StageWaveDefinition> Waves => waves;

    public List<StageWaveDefinition> CreateOrderedWaveList()
    {
        List<StageWaveDefinition> orderedWaves = new(waves);
        orderedWaves.RemoveAll(wave => wave == null);
        orderedWaves.Sort(
            (left, right) => left.StartTime.CompareTo(right.StartTime)
        );
        return orderedWaves;
    }

#if UNITY_EDITOR
    public void ConfigureForEditor(
        string stageName,
        bool finalStage,
        string nextSceneName,
        float nextSceneDelay,
        List<StageWaveDefinition> waves
    )
    {
        this.stageName = stageName;
        this.finalStage = finalStage;
        loadNextSceneAutomatically = true;
        this.nextSceneName = nextSceneName;
        this.nextSceneDelay = Mathf.Max(0f, nextSceneDelay);
        this.waves = waves ?? new List<StageWaveDefinition>();
    }
#endif
}
