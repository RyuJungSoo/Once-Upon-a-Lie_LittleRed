using UnityEngine;

public class UIToggleController : MonoBehaviour
{
    [Header("Target UI")]
    [SerializeField]
    private GameObject targetUI;

    [Header("Pause Option")]
    [Tooltip("활성화하면 UI를 열 때 게임을 일시정지합니다.")]
    [SerializeField]
    private bool pauseFunction;

    public void OpenUI()
    {
        if (targetUI == null)
        {
            Debug.LogWarning(
                $"{name}: Target UI가 연결되지 않았습니다.",
                this
            );
            return;
        }

        targetUI.SetActive(true);

        if (!pauseFunction)
        {
            return;
        }

        if (!GameManager.HasInstance)
        {
            Debug.LogWarning(
                $"{name}: GameManager를 찾을 수 없습니다.",
                this
            );
            return;
        }

        GameManager.Instance.PauseGame();
    }

    public void CloseUI()
    {
        if (targetUI == null)
        {
            Debug.LogWarning(
                $"{name}: Target UI가 연결되지 않았습니다.",
                this
            );
            return;
        }

        if (pauseFunction &&
            GameManager.HasInstance &&
            GameManager.Instance.IsPaused)
        {
            GameManager.Instance.ResumeGame();
        }

        targetUI.SetActive(false);
    }

    public void ToggleUI()
    {
        if (targetUI == null)
        {
            Debug.LogWarning(
                $"{name}: Target UI가 연결되지 않았습니다.",
                this
            );
            return;
        }

        if (targetUI.activeSelf)
        {
            CloseUI();
        }
        else
        {
            OpenUI();
        }
    }

    private void OnDisable()
    {
        if (!pauseFunction)
        {
            return;
        }

        if (GameManager.HasInstance &&
            GameManager.Instance.IsPaused)
        {
            GameManager.Instance.ResumeGame();
        }
    }
}