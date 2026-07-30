using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum EGameState
{
    None,
    Prologue,
    Playing,
    Paused,
    StageClear,
    GameOver,
    Victory
}

public class GameManager : Singleton<GameManager>
{
    private const string MainMenuSceneName = "MainMenu";

    [Header("Debug")]
    [Tooltip("활성화하면 MainMenu가 아닌 씬을 직접 실행했을 때 Playing 상태로 시작합니다.")]
    [SerializeField]
    private bool isDebug;

    [Header("Game State")]
    [SerializeField]
    private EGameState currentState = EGameState.None;
    private EGameState stateBeforePause = EGameState.Playing;

    [Header("Stage")]
    [SerializeField]
    private int currentStageIndex;

    [Header("Player Runtime Components")]
    [SerializeField]
    private PlayerExperience playerExperience;

    public PlayerExperience PlayerExperience =>
        playerExperience;

    [Header("Player Level")]
    [Tooltip("새 게임을 시작할 때의 플레이어 레벨입니다.")]
    [SerializeField, Min(1)]
    private int startPlayerLevel = 1;

    [Tooltip("현재 플레이어 레벨입니다.")]
    [SerializeField, Min(1)]
    private int currentPlayerLevel = 1;


    /// 현재 게임 상태.
    public EGameState CurrentState => currentState;

    /// 현재 스테이지 번호.
    /// 0부터 시작합니다.
    public int CurrentStageIndex => currentStageIndex;

    /// 현재 플레이어 레벨.
    public int CurrentPlayerLevel => currentPlayerLevel;

    /// 현재 디버그 모드인지 여부.
    public bool IsDebug => isDebug;

    /// 현재 정상적인 게임 진행 상태인지 여부.
    public bool IsPlaying =>
        currentState == EGameState.Playing;

    /// 현재 일시정지 상태인지 여부.
    public bool IsPaused =>
    currentState == EGameState.Paused;


    /// 게임 상태가 변경되었을 때 호출됩니다.
    public event Action<EGameState> OnGameStateChanged;

    /// 스테이지가 시작되었을 때 호출됩니다.
    /// int 값으로 시작된 스테이지 인덱스를 전달합니다.
    public event Action<int> OnStageStarted;

    /// 스테이지를 클리어했을 때 호출됩니다.
    /// int 값으로 클리어한 스테이지 인덱스를 전달합니다.
    public event Action<int> OnStageCleared;

    /// 플레이어 레벨이 변경되었을 때 호출됩니다.
    /// int 값으로 변경된 플레이어 레벨을 전달합니다.
    public event Action<int> OnPlayerLevelChanged;

    /// 게임 오버가 발생했을 때 호출됩니다.
    public event Action OnGameOver;

    /// 최종 승리가 발생했을 때 호출됩니다.
    public event Action OnVictory;


    protected override void Awake()
    {
        base.Awake();

        // 중복 생성된 싱글톤이라면 아래 초기화를 실행하지 않습니다.
        if (Instance != this)
        {
            return;
        }

        InitializeRuntimeComponents();

        startPlayerLevel = Mathf.Max(
            1,
            startPlayerLevel
        );

        currentPlayerLevel = Mathf.Max(
            1,
            currentPlayerLevel
        );

        Time.timeScale = 1f;

        InitializeGameState(
            SceneManager.GetActiveScene()
        );
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }


    /// 새로운 씬이 로드되었을 때 실행됩니다.
    private void OnSceneLoaded(
        Scene scene,
        LoadSceneMode loadSceneMode
    )
    {
        Time.timeScale = 1f;

        InitializeGameState(scene);
        PlayBgmForLoadedScene(scene);

        if (scene.name == MainMenuSceneName)
        {
            ResetRunProgress();
            return;
        }

        ResetPlayerRuntimeForScene();
    }


    /// 현재 씬과 디버그 설정을 기준으로 게임 상태를 초기화합니다.
    private void InitializeGameState(Scene scene)
    {
        bool isMainMenu =
            scene.name == MainMenuSceneName;

        if (isDebug && !isMainMenu)
        {
            ChangeState(EGameState.Playing);
            return;
        }

        if (isMainMenu)
        {
            ChangeState(EGameState.None);
        }
    }


    /// 프롤로그를 시작합니다.
    public void StartPrologue()
    {
        ChangeState(EGameState.Prologue);
    }


    /// 새 게임을 첫 번째 스테이지부터 시작합니다.
    public void StartGame()
    {
        Time.timeScale = 1f;

        ResetRunProgress();

        ChangeState(EGameState.Playing);

        OnStageStarted?.Invoke(
            currentStageIndex
        );
    }


    /// 현재 스테이지를 시작합니다.
    /// 씬마다 GameManager가 유지될 때 사용할 수 있습니다.
    public void StartCurrentStage()
    {
        Time.timeScale = 1f;

        ChangeState(EGameState.Playing);
        OnStageStarted?.Invoke(currentStageIndex);
    }


    /// 현재 스테이지를 클리어 처리합니다.
    public void StageClear()
    {
        if (!IsPlaying)
        {
            return;
        }

        ChangeState(EGameState.StageClear);
        OnStageCleared?.Invoke(currentStageIndex);
        if (UIManager.HasInstance)
        {
            UIManager.Instance.ShowStageClearUI();
        }
    }


    /// 다음 스테이지를 시작합니다.
    public void StartNextStage()
    {
        currentStageIndex++;

        ChangeState(EGameState.Playing);
        PlayCurrentStageBgm();

        OnStageStarted?.Invoke(currentStageIndex);
    }


    /// 플레이어의 레벨을 1 증가시킵니다.
    public void LevelUp()
    {
        SetPlayerLevel(
            currentPlayerLevel + 1
        );
    }


    /// 플레이어의 레벨을 지정한 값으로 변경합니다.
    public void SetPlayerLevel(int newLevel)
    {
        newLevel = Mathf.Max(
            1,
            newLevel
        );

        if (currentPlayerLevel == newLevel)
        {
            return;
        }

        currentPlayerLevel = newLevel;

        OnPlayerLevelChanged?.Invoke(
            currentPlayerLevel
        );
    }


    /// 플레이어의 레벨을 시작 레벨로 초기화합니다.
    private void ResetPlayerLevel()
    {
        currentPlayerLevel = startPlayerLevel;

        OnPlayerLevelChanged?.Invoke(
            currentPlayerLevel
        );
    }


    /// 게임 오버를 처리합니다.
    public void GameOver()
    {
        if (currentState == EGameState.GameOver ||
            currentState == EGameState.Victory)
        {
            return;
        }

        ChangeState(EGameState.GameOver);
        OnGameOver?.Invoke();
        
        if (UIManager.HasInstance)
        {
            UIManager.Instance.ShowGameOverUI();
        }
    }


    /// 최종 승리를 처리합니다.
    public void Victory()
    {
        if (currentState == EGameState.Victory)
        {
            return;
        }

        ChangeState(EGameState.Victory);
        OnVictory?.Invoke();
        if (UIManager.HasInstance)
        {
            UIManager.Instance.ShowStageClearUI();
        }
    }


    /// 게임 상태를 변경하고 변경 이벤트를 발생시킵니다.
    private void ChangeState(EGameState newState)
    {
        if (currentState == newState)
        {
            return;
        }

        currentState = newState;

        OnGameStateChanged?.Invoke(currentState);
    }

    /// Playing 상태의 게임을 일시정지합니다.
    public void PauseGame()
    {
        if (currentState != EGameState.Playing)
        {
            return;
        }

        ChangeState(EGameState.Paused);
        Time.timeScale = 0f;
    }


    /// 일시정지를 해제하고 Playing 상태로 돌아갑니다.
    public void ResumeGame()
    {
        if (currentState != EGameState.Paused)
        {
            return;
        }

        Time.timeScale = 1f;
        ChangeState(EGameState.Playing);
    }

    private void InitializeRuntimeComponents()
    {
        if (playerExperience == null)
        {
            playerExperience =
                GetComponent<PlayerExperience>();
        }

        if (playerExperience == null)
        {
            Debug.LogError(
                $"{nameof(GameManager)} 오브젝트에 " +
                $"{nameof(PlayerExperience)} 컴포넌트가 없습니다.",
                this
            );
        }
    }

    private void PlayCurrentStageBgm()
    {
        if (!SoundManager.HasInstance)
        {
            return;
        }

        EBGMType bgmType;

        switch (currentStageIndex)
        {
            case 0:
                bgmType = EBGMType.Stage1;
                break;

            case 1:
                bgmType = EBGMType.Stage2;
                break;

            case 2:
                bgmType = EBGMType.Stage3;
                break;

            default:
                Debug.LogWarning(
                    $"{nameof(GameManager)}: " +
                    $"현재 스테이지에 대응하는 BGM이 없습니다. " +
                    $"Stage Index: {currentStageIndex}",
                    this
                );

                return;
        }

        SoundManager.Instance.PlayBGM(bgmType);
    }

    private void PlayBgmForLoadedScene(Scene scene)
    {
        if (!SoundManager.HasInstance)
        {
            return;
        }

        if (scene.name == MainMenuSceneName)
        {
            SoundManager.Instance.PlayBGM(
                EBGMType.MainMenu
            );

            return;
        }

        PlayCurrentStageBgm();
    }

    private void ResetRunProgress()
    {
        currentStageIndex = 0;

        ResetPlayerLevel();

        if (playerExperience == null)
        {
            playerExperience =
                GetComponent<PlayerExperience>();
        }

        if (playerExperience != null)
        {
            playerExperience.ResetExperience();
        }

        if (UIManager.HasInstance)
        {
            UIManager.Instance.HideResultUI();
        }
    }

    private void ResetPlayerRuntimeForScene()
    {
        PlayerLevelStats levelStats =
            FindFirstObjectByType<PlayerLevelStats>();

        if (levelStats == null)
        {
            Debug.LogWarning(
                $"{nameof(GameManager)}: " +
                "현재 씬에서 PlayerLevelStats를 " +
                "찾을 수 없습니다.",
                this
            );

            return;
        }

        // 반드시 Ammo와 Mental보다 먼저 계산
        levelStats.RecalculateStats(
            currentPlayerLevel
        );

        PlayerAmmo playerAmmo =
            levelStats.GetComponent<PlayerAmmo>();

        if (playerAmmo != null)
        {
            playerAmmo.ResetAmmo();
        }

        PlayerMental playerMental =
            levelStats.GetComponent<PlayerMental>();

        if (playerMental != null)
        {
            playerMental.ResetMental();
        }

        if (UIManager.HasInstance)
        {
            UIManager.Instance.HideResultUI();
        }
    }
}