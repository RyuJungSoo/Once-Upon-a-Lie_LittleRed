using UnityEngine;

[DisallowMultipleComponent]
public class TutorialUI : MonoBehaviour
{
    [Header("Tutorial UI")]
    [SerializeField]
    private GameObject tutorialPanel;

    [Header("Settings")]
    [SerializeField]
    private bool pauseGameWhileOpen = true;

    private bool hasStarted;

    private void Awake()
    {
        if (tutorialPanel == null)
        {
            tutorialPanel = gameObject;
        }
    }

    private void Start()
    {
        OpenTutorial();
    }

    private void OnDisable()
    {
        if (!hasStarted &&
            pauseGameWhileOpen)
        {
            Time.timeScale = 1f;
        }
    }

    public void OpenTutorial()
    {
        hasStarted = false;

        if (tutorialPanel != null)
        {
            tutorialPanel.SetActive(true);
        }

        if (pauseGameWhileOpen)
        {
            Time.timeScale = 0f;
        }
    }

    public void OnClickStart()
    {
        if (hasStarted)
        {
            return;
        }

        hasStarted = true;
        Time.timeScale = 1f;

        if (GameManager.HasInstance)
        {
            GameManager.Instance.StartCurrentStage();
        }
        else
        {
            Debug.LogWarning(
                $"{nameof(TutorialUI)}: " +
                "GameManager를 찾을 수 없습니다.",
                this
            );
        }

        if (tutorialPanel != null)
        {
            tutorialPanel.SetActive(false);
        }
    }
}