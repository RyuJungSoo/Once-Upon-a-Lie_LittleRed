using UnityEngine;

[DisallowMultipleComponent]
public class TutorialUI : MonoBehaviour
{
    [Header("Tutorial UI")]
    [Tooltip("화면 전체를 감싸는 튜토리얼 UI 최상위 오브젝트입니다.")]
    [SerializeField]
    private GameObject tutorialPanel;

    private bool isStartingStage;


    private void Awake()
    {
        if (tutorialPanel == null)
        {
            tutorialPanel = gameObject;
        }
    }


    private void Start()
    {
        HandleTutorialOnSceneStart();
    }


    /// <summary>
    /// 씬이 시작되었을 때 튜토리얼 표시 여부를 자동으로 결정합니다.
    /// </summary>
    private void HandleTutorialOnSceneStart()
    {
        if (!GameManager.HasInstance)
        {
            Debug.LogError(
                $"{nameof(TutorialUI)}: " +
                "GameManager를 찾을 수 없습니다.",
                this
            );

            return;
        }

        GameManager gameManager =
            GameManager.Instance;

        /*
         * 디버그 모드로 스테이지 씬을 직접 실행했거나
         * 이미 Playing 상태라면 튜토리얼을 띄우지 않습니다.
         */
        if (gameManager.IsDebug ||
            gameManager.CurrentState == EGameState.Playing)
        {
            CloseTutorial();
            return;
        }

        /*
         * 이번 앱 실행 중 튜토리얼을 이미 봤다면
         * UI를 생략하고 바로 현재 스테이지를 시작합니다.
         */
        if (gameManager.HasShownTutorial)
        {
            StartStageWithoutTutorial();
            return;
        }

        OpenTutorial();
    }


    /// <summary>
    /// 최초 실행 시 튜토리얼 UI를 표시합니다.
    /// </summary>
    private void OpenTutorial()
    {
        isStartingStage = false;

        if (tutorialPanel != null)
        {
            tutorialPanel.SetActive(true);
        }
    }


    /// <summary>
    /// 튜토리얼 Start 버튼에서 호출합니다.
    /// </summary>
    public void OnClickStart()
    {
        if (isStartingStage)
        {
            return;
        }

        if (!GameManager.HasInstance)
        {
            Debug.LogError(
                $"{nameof(TutorialUI)}: " +
                "GameManager를 찾을 수 없습니다.",
                this
            );

            return;
        }

        isStartingStage = true;

        GameManager.Instance
            .MarkTutorialAsShown();

        GameManager.Instance
            .StartCurrentStage();

        CloseTutorial();
    }


    /// <summary>
    /// 튜토리얼을 이미 본 경우 UI를 생략하고
    /// 현재 스테이지를 바로 시작합니다.
    /// </summary>
    private void StartStageWithoutTutorial()
    {
        if (isStartingStage)
        {
            return;
        }

        isStartingStage = true;

        GameManager.Instance
            .StartCurrentStage();

        CloseTutorial();
    }


    /// <summary>
    /// 튜토리얼 UI를 비활성화합니다.
    /// </summary>
    private void CloseTutorial()
    {
        if (tutorialPanel != null)
        {
            tutorialPanel.SetActive(false);
        }
    }
}