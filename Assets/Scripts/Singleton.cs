using UnityEngine;


/// 매니저 컴포넌트에서 공통으로 사용하는 제네릭 싱글톤 베이스 클래스.
/// <typeparam name="T">싱글톤으로 사용할 매니저 타입</typeparam>
public abstract class Singleton<T> : MonoBehaviour
    where T : Singleton<T>
{

    /// 현재 활성화된 싱글톤 인스턴스.
    public static T Instance { get; private set; }


    /// 현재 싱글톤 인스턴스가 존재하는지 여부.
    public static bool HasInstance => Instance != null;
    ///  현재 객체가 등록된 싱글톤인지 확인 여부.
       protected bool IsSingletonInstance => Instance == this;

    [Header("Singleton Settings")]
    [SerializeField]
    [Tooltip("체크된 경우에만 씬이 변경되어도 오브젝트를 유지합니다.")]
    private bool dontDestroyOnLoad;

    protected virtual void Awake()
    {
        // 이미 다른 인스턴스가 존재한다면 새로 생성된 오브젝트 제거
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = (T)this;

        // 옵션이 체크된 경우에만 씬 전환 후에도 유지
        if (dontDestroyOnLoad)
        {
            // DontDestroyOnLoad는 루트 오브젝트에 적용해야 하므로
            // 부모가 있다면 루트로 이동
            if (transform.parent != null)
            {
                transform.SetParent(null);
            }

            DontDestroyOnLoad(gameObject);
        }

        OnSingletonInitialized();
    }

    private void OnDestroy()
    {
        // 현재 인스턴스 자신이 파괴될 때만 참조 해제
        if (Instance == this)
        {
            Instance = null;
        }

        OnSingletonDestroyed();
    }


    /// 싱글톤 등록이 완료된 뒤 호출된다.
    /// 상속받은 매니저의 초기화 로직은 여기서 처리한다.
    protected virtual void OnSingletonInitialized()
    {
    }

    /// 싱글톤 오브젝트가 파괴될 때 호출된다.
    protected virtual void OnSingletonDestroyed()
    {
    }
}