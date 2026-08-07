using System;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class TutorialProgressStore
    : Singleton<TutorialProgressStore>
{
    [Header("Tutorial Progress")]
    [SerializeField]
    [Tooltip(
        "현재 게임 실행 중 튜토리얼 UI를 " +
        "이미 표시했는지 여부입니다."
    )]
    private bool hasShownTutorial;

    public bool HasShownTutorial =>
        hasShownTutorial;

    public event Action<bool>
        OnTutorialStateChanged;


    /// <summary>
    /// 튜토리얼을 표시한 것으로 기록합니다.
    /// </summary>
    public void MarkTutorialAsShown()
    {
        if (hasShownTutorial)
        {
            return;
        }

        hasShownTutorial = true;

        OnTutorialStateChanged?.Invoke(
            hasShownTutorial
        );
    }


    /// <summary>
    /// 개발 및 테스트용으로 튜토리얼 상태를 초기화합니다.
    /// </summary>
    public void ResetTutorialProgress()
    {
        if (!hasShownTutorial)
        {
            return;
        }

        hasShownTutorial = false;

        OnTutorialStateChanged?.Invoke(
            hasShownTutorial
        );
    }
}