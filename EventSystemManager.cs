using UnityEngine;
using UnityEngine.EventSystems;

public class EventSystemManager : MonoBehaviour
{
    private static EventSystemManager instance;
    private static EventSystem persistentEventSystem;

    [Header("EventSystem Settings")]
    public bool createIfMissing = true;
    public bool makePersistent = true;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            if (makePersistent)
            {
                DontDestroyOnLoad(gameObject);
            }

            EnsureEventSystem();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        EnsureEventSystem();
    }

    public static void EnsureEventSystem()
    {
        // 생성된 EventSystem이 있으면 그대로 사용
        if (persistentEventSystem != null)
        {
            return;
        }

        // 현재 씬에서 EventSystem 찾기
        EventSystem currentEventSystem = FindFirstObjectByType<EventSystem>();

        if (currentEventSystem == null)
        {
            // EventSystem이 없으면 생성
            Debug.Log("[EventSystemManager] EventSystem을 찾을 수 없습니다. 새로 생성합니다.");
            CreateEventSystem();
        }
        else
        {
            // 기존 EventSystem을 지속적으로 사용
            if (instance != null && instance.makePersistent)
            {
                SetupPersistentEventSystem(currentEventSystem);
            }
        }
    }

    static void CreateEventSystem()
    {
        GameObject eventSystemObj = new GameObject("EventSystem");
        EventSystem eventSystem = eventSystemObj.AddComponent<EventSystem>();
        eventSystemObj.AddComponent<StandaloneInputModule>();

        if (instance != null && instance.makePersistent)
        {
            DontDestroyOnLoad(eventSystemObj);
            persistentEventSystem = eventSystem;
        }

        Debug.Log("[EventSystemManager] EventSystem 이 성공적으로 생성되었습니다.");
    }

    static void SetupPersistentEventSystem(EventSystem eventSystem)
    {
        if (eventSystem.gameObject.scene.name != "DontDestroyOnLoad")
        {
            DontDestroyOnLoad(eventSystem.gameObject);
            persistentEventSystem = eventSystem;
            Debug.Log("[EventSystemManager] 존재하는 EventSystem이 DontDestroyOnLoad로 설정되었습니다.");
        }
        else
        {
            persistentEventSystem = eventSystem;
        }
    }

    public static void CleanupDuplicateEventSystems()
    {
        EventSystem[] eventSystems = FindObjectsByType<EventSystem>(FindObjectsSortMode.None);

        if (eventSystems.Length <= 1) return;

        Debug.Log($"[EventSystemManager] EventSystem을 찾았습니다: {eventSystems.Length} 중복이 있으면 제거합니다.");

        // DontDestroyOnLoad에 있는 EventSystem 찾기
        EventSystem keepEventSystem = persistentEventSystem;

        if (keepEventSystem == null)
        {
            // DontDestroyOnLoad에 있는 것 중 첫 번째 선택
            foreach (var es in eventSystems)
            {
                if (es.gameObject.scene.name == "DontDestroyOnLoad")
                {
                    keepEventSystem = es;
                    persistentEventSystem = es;
                    break;
                }
            }
        }

        // 첫 번째를 유지할 EventSystem으로 선택 (DontDestroyOnLoad가 없는 경우)
        if (keepEventSystem == null)
        {
            keepEventSystem = eventSystems[0];
            if (instance != null && instance.makePersistent)
            {
                DontDestroyOnLoad(keepEventSystem.gameObject);
                persistentEventSystem = keepEventSystem;
            }
        }

        // 나머지 EventSystem들 제거
        foreach (var es in eventSystems)
        {
            if (es != keepEventSystem)
            {
                Debug.Log($"[EventSystemManager] 중복된 EventSystem을 삭제합니다: {es.name}");
                Destroy(es.gameObject);
            }
        }
    }

    // 씬이 로드될 때마다 호출되도록 설정
    void OnEnable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        // 씬 로드 후 EventSystem 상태 확인
        Invoke(nameof(DelayedEventSystemCheck), 0.1f);
    }

    void DelayedEventSystemCheck()
    {
        CleanupDuplicateEventSystems();
        EnsureEventSystem();
    }
}