using System;
using Unity.Cinemachine;
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
    [Tooltip("게임오버 후 Retry할 때 이동할 첫 번째 스테이지 씬입니다.")]
    [SerializeField]
    private string firstStageSceneName = "Stage1_Scene";

    [SerializeField]
    private int currentStageIndex;

    [Header("Player Runtime Components")]
    [SerializeField]
    private PlayerExperience playerExperience;

    [Header("Player Level")]
    [Tooltip("새 게임을 시작할 때의 플레이어 레벨입니다.")]
    [SerializeField, Min(1)]
    private int startPlayerLevel = 1;

    [Tooltip("현재 플레이어 레벨입니다.")]
    [SerializeField, Min(1)]
    private int currentPlayerLevel = 1;


    public PlayerExperience PlayerExperience =>
        playerExperience;

    public EGameState CurrentState =>
        currentState;

    public int CurrentStageIndex =>
        currentStageIndex;

    public int CurrentPlayerLevel =>
        currentPlayerLevel;

    public bool IsDebug =>
        isDebug;

    public bool IsPlaying =>
        currentState == EGameState.Playing;

    public bool IsPaused =>
        currentState == EGameState.Paused;


    public event Action<EGameState> OnGameStateChanged;
    public event Action<int> OnStageStarted;
    public event Action<int> OnStageCleared;
    public event Action<int> OnPlayerLevelChanged;
    public event Action OnGameOver;
    public event Action OnVictory;


    protected override void Awake()
    {
        base.Awake();

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
        SceneManager.sceneLoaded +=
            OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -=
            OnSceneLoaded;
    }


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

            PersistentRuntimeCleanupController persistentRuntimeCleanupController = GetComponent<PersistentRuntimeCleanupController>();
            persistentRuntimeCleanupController
            ?.CleanupAndDestroySelf();        
            return;
        }

        // 현재 레벨 기준으로 탄알과 정신력을 초기화합니다.
        ResetPlayerRuntimeForScene();
    }


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


    public void StartPrologue()
    {
        Time.timeScale = 1f;
        ChangeState(EGameState.Prologue);
    }


    public void StartGame()
    {
        Time.timeScale = 1f;

        ResetRunProgress();

        ChangeState(EGameState.Playing);

        OnStageStarted?.Invoke(
            currentStageIndex
        );
    }


    public void StartCurrentStage()
    {
        Time.timeScale = 1f;

        ChangeState(EGameState.Playing);

        OnStageStarted?.Invoke(
            currentStageIndex
        );
    }


    public void StageClear()
    {
        if (!IsPlaying)
        {
            return;
        }

        ChangeState(EGameState.StageClear);

        OnStageCleared?.Invoke(
            currentStageIndex
        );

        if (UIManager.HasInstance)
        {
            UIManager.Instance
                .ShowStageClearUI();
        }
    }


    public void StartNextStage()
    {
        currentStageIndex++;

        ChangeState(EGameState.Playing);
        PlayCurrentStageBgm();

        OnStageStarted?.Invoke(
            currentStageIndex
        );
    }


    public void LevelUp()
    {
        SetPlayerLevel(
            currentPlayerLevel + 1
        );
    }


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


    private void ResetPlayerLevel()
    {
        currentPlayerLevel =
            startPlayerLevel;

        OnPlayerLevelChanged?.Invoke(
            currentPlayerLevel
        );
    }


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
            UIManager.Instance
                .ShowGameOverUI();
        }
    }


    public void RetryFromBeginning()
    {
        Time.timeScale = 1f;

        ResetRunProgress();
        ChangeState(EGameState.Prologue);

        if (string.IsNullOrWhiteSpace(
                firstStageSceneName))
        {
            Debug.LogError(
                $"{nameof(GameManager)}: " +
                "첫 번째 스테이지 씬 이름이 비어 있습니다.",
                this
            );

            return;
        }

        SceneManager.LoadScene(
            firstStageSceneName
        );
    }


    public void ReturnToMainMenu()
    {
        Time.timeScale = 1f;

        SceneManager.LoadScene(
            MainMenuSceneName
        );
    }


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
            UIManager.Instance
                .ShowStageClearUI();
        }
    }


    private void ChangeState(
        EGameState newState
    )
    {
        if (currentState == newState)
        {
            return;
        }

        currentState = newState;

        OnGameStateChanged?.Invoke(
            currentState
        );
    }


    public void PauseGame()
    {
        if (currentState != EGameState.Playing)
        {
            return;
        }

        stateBeforePause = currentState;

        ChangeState(EGameState.Paused);
        Time.timeScale = 0f;
    }


    public void ResumeGame()
    {
        if (currentState != EGameState.Paused)
        {
            return;
        }

        Time.timeScale = 1f;

        EGameState resumeState =
            stateBeforePause == EGameState.Paused
                ? EGameState.Playing
                : stateBeforePause;

        ChangeState(resumeState);
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
                    "현재 스테이지에 대응하는 BGM이 없습니다. " +
                    $"Stage Index: {currentStageIndex}",
                    this
                );

                return;
        }

        SoundManager.Instance.PlayBGM(
            bgmType
        );
    }


    private void PlayBgmForLoadedScene(
        Scene scene
    )
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
            playerExperience
                .ResetExperience();
        }

        if (UIManager.HasInstance)
        {
            UIManager.Instance
                .HideResultUI();
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

        // 반드시 Ammo와 Mental보다 먼저 계산합니다.
        levelStats.RecalculateStats(
            currentPlayerLevel
        );

        PlayerAmmo playerAmmo =
            levelStats.GetComponent<PlayerAmmo>();

        if (playerAmmo != null)
        {
            // 현재 레벨 기준 최대 탄알로 초기화합니다.
            playerAmmo.ResetAmmo();
        }

        PlayerMental playerMental =
            levelStats.GetComponent<PlayerMental>();

        if (playerMental != null)
        {
            // 현재 레벨 기준 최대 정신력으로 초기화합니다.
            playerMental.ResetMental();

            MentalCameraShake cameraShake =
                FindFirstObjectByType<MentalCameraShake>();

            if (cameraShake != null)
            {
                cameraShake.ResetForMental(
                    playerMental
                );
            }
        }

        if (UIManager.HasInstance)
        {
            UIManager.Instance
                .HideResultUI();
        }
    }
}
