using UnityEngine;

public class UIToggleController : MonoBehaviour
{
    [Header("Target UI")]
    [SerializeField] private GameObject targetUI;

    [Header("Pause Option")]
    [SerializeField] private bool pauseFunction = false;

    public void OpenUI()
    {
        if (targetUI == null)
        {
            Debug.LogWarning($"{name}: Target UI가 연결되지 않았습니다.");
            return;
        }

        targetUI.SetActive(true);

        if (pauseFunction)
        {
            Time.timeScale = 0f;
        }
    }

    public void CloseUI()
    {
        if (targetUI == null)
        {
            Debug.LogWarning($"{name}: Target UI가 연결되지 않았습니다.");
            return;
        }

        targetUI.SetActive(false);

        if (pauseFunction)
        {
            Time.timeScale = 1f;
        }
    }

    public void ToggleUI()
    {
        if (targetUI == null)
        {
            Debug.LogWarning($"{name}: Target UI가 연결되지 않았습니다.");
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
        // Pause UI를 가진 오브젝트가 비활성화되면서 TimeScale이 0으로 남는 상황 방지
        if (pauseFunction)
        {
            Time.timeScale = 1f;
        }
    }
}