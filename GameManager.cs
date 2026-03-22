using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using System.Collections;

public class GameManager : MonoBehaviour
{
    // 싱글톤 패턴
    private static GameManager instance;
    public static GameManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<GameManager>();
                if (instance == null)
                {
                    GameObject go = new GameObject("GameManager");
                    instance = go.AddComponent<GameManager>();
                }
            }
            return instance;
        }
    }

    [Header("Character Settings")]
    public GameObject[] characterPrefabs;
    public GameObject selectedCharacterPrefab;
    public int selectedCharacterIndex = -1;

    [Header("Scene Settings")]
    public string mainMenuSceneName = "MainMenu";
    public string gameplaySceneName = "Gameplay";

    [Header("Player Settings")]
    public string playerNickname = "";

    void Awake()
    {
        // 싱글톤 설정
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            Debug.Log("[GameManager] DontDestroyOnLoad 생성 및 설정");

            // EventSystem도 보존하도록 설정
            EnsureEventSystemPersistence();
        }
        else if (instance != this)
        {
            Destroy(gameObject);
            return;
        }
    }

    void EnsureEventSystemPersistence()
    {
        // EventSystem 찾기
        EventSystem eventSystem = FindFirstObjectByType<EventSystem>();

        if (eventSystem != null)
        {
            // EventSystem이 이미 DontDestroyOnLoad 설정되어 있는지 확인
            if (eventSystem.gameObject.scene.name != "DontDestroyOnLoad")
            {
                DontDestroyOnLoad(eventSystem.gameObject);
                Debug.Log("[GameManager] EventSystem 설정");
            }
        }
        else
        {
            Debug.Log("[GameManager] 유지할 EventSystem을 찾지 못했습니다.");
        }
    }

    void Start()
    {
        // 씬 로드 이벤트 구독
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        // 씬 로드 이벤트 구독 해제
        if (instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
    }

    // 캐릭터 선택 설정
    public void SetSelectedCharacter(int characterIndex, GameObject characterPrefab)
    {
        selectedCharacterIndex = characterIndex;
        selectedCharacterPrefab = characterPrefab;
        Debug.Log($"[GameManager] 캐릭터 {characterIndex} 선택, prefab: {characterPrefab?.name}");
    }

    // 선택된 캐릭터 프리팹 가져오기
    public GameObject GetSelectedCharacterPrefab()
    {
        // 먼저 직접 설정된 프리팹 확인
        if (selectedCharacterPrefab != null)
        {
            Debug.Log($"[GameManager] 선택된 프리팹 확인: {selectedCharacterPrefab.name}");
            return selectedCharacterPrefab;
        }

        // CharacterSelector의 정적 변수 확인
        if (CharacterSelector.selectedCharacterPrefab != null)
        {
            selectedCharacterPrefab = CharacterSelector.selectedCharacterPrefab;
            selectedCharacterIndex = CharacterSelector.selectedCharacterIndex;
            Debug.Log($"[GameManager] CharacterSelector로부터 prefab 받기: {selectedCharacterPrefab.name}");
            return selectedCharacterPrefab;
        }

        // PlayerPrefs 백업 확인
        if (PlayerPrefs.HasKey("SelectedCharacter"))
        {
            selectedCharacterIndex = PlayerPrefs.GetInt("SelectedCharacter");
            if (selectedCharacterIndex >= 0 && selectedCharacterIndex < characterPrefabs.Length)
            {
                selectedCharacterPrefab = characterPrefabs[selectedCharacterIndex];
                Debug.Log($"[GameManager] PlayerPrefs로부터 prefab 받기, index: {selectedCharacterIndex}");
                return selectedCharacterPrefab;
            }
        }

        // 기본 캐릭터 사용
        Debug.LogWarning("[GameManager] 캐릭터가 선택되지 않았습니다. 기본값으로 설정합니다.");
        if (characterPrefabs.Length > 0)
        {
            selectedCharacterPrefab = characterPrefabs[0];
            return selectedCharacterPrefab;
        }

        Debug.LogError("[GameManager] 사용 가능한 캐릭터 prefab이 없습니다!");
        return null;
    }

    // 씬 로드 완료 시 호출
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"[GameManager] Scene 로드: {scene.name}");

        // EventSystem 확인 및 중복 제거
        HandleEventSystemOnSceneLoad();

        if (scene.name == gameplaySceneName)
        {
            Debug.Log("[GameManager] Gameplay scene 로드. 네트워크 커넥션 설정.");
            StartCoroutine(SetupNetworkClient());
        }
    }

    void HandleEventSystemOnSceneLoad()
    {
        EventSystem[] eventSystems = FindObjectsByType<EventSystem>(FindObjectsSortMode.None);

        if (eventSystems.Length > 1)
        {
            Debug.Log($"[GameManager] {eventSystems.Length} 생성된 EventSystem들을 확인합니다.");

            // DontDestroyOnLoad EventSystem 찾기
            EventSystem persistentEventSystem = null;
            for (int i = 0; i < eventSystems.Length; i++)
            {
                if (eventSystems[i].gameObject.scene.name == "DontDestroyOnLoad")
                {
                    persistentEventSystem = eventSystems[i];
                    break;
                }
            }

            // 중복 EventSystem 제거
            for (int i = 0; i < eventSystems.Length; i++)
            {
                if (eventSystems[i] != persistentEventSystem)
                {
                    Debug.Log($"[GameManager] 중복된 EventSystem을 삭제합니다: {eventSystems[i].name}");
                    Destroy(eventSystems[i].gameObject);
                }
            }
        }
        else if (eventSystems.Length == 0)
        {
            Debug.LogWarning("[GameManager] 발견된 EventSystem이 없습니다. 새로 생성합니다.");
            CreateEventSystem();
        }
        else
        {
            Debug.Log($"[GameManager] EventSystem 발견: {eventSystems[0].name}");
        }
    }

    void CreateEventSystem()
    {
        GameObject eventSystemObj = new GameObject("EventSystem");
        EventSystem eventSystem = eventSystemObj.AddComponent<EventSystem>();
        eventSystemObj.AddComponent<StandaloneInputModule>();

        Debug.Log("[GameManager] EventSystem이 성공적으로 생성되었습니다.");
    }

    // 네트워크 클라이언트 설정
    IEnumerator SetupNetworkClient()
    {
        // 씬이 완전히 로드될 때까지 대기
        yield return null;

        // NetworkClient 찾기
        NetworkClient networkClient = FindFirstObjectByType<NetworkClient>();
        if (networkClient != null)
        {
            Debug.Log("[GameManager] NetworkClient 발견. 캐릭터 prefab 설정.");

            // 선택한 캐릭터 프리팹 가져오기
            GameObject characterPrefab = GetSelectedCharacterPrefab();
            if (characterPrefab != null)
            {
                // NetworkClient에 프리팹 설정
                networkClient.SetSelectedCharacterPrefab(characterPrefab);
                Debug.Log($"[GameManager] NetworkClient에 캐릭터 prefab을 설정합니다: {characterPrefab.name}");

                // 서버 연결 시작
                networkClient.ConnectToServer();
                Debug.Log("[GameManager] 서버 연결이 시작되었습니다.");
            }
            else
            {
                Debug.LogError("[GameManager] 사용 가능한 캐릭터 prefab이 없습니다. 서버에 연결할 수 없습니다.");
            }
        }
        else
        {
            Debug.LogError("[GameManager] Gameplay scene에서 NetworkClient를 찾을 수 없습니다!");
        }
    }

    // 메인 메뉴로 돌아가기
    public void ReturnToMainMenu()
    {
        // 선택 초기화
        selectedCharacterIndex = -1;
        selectedCharacterPrefab = null;
        CharacterSelector.selectedCharacterIndex = -1;
        CharacterSelector.selectedCharacterPrefab = null;
        PlayerPrefs.DeleteKey("SelectedCharacter");

        // NetworkClient 찾아서 연결 해제
        NetworkClient networkClient = FindFirstObjectByType<NetworkClient>();
        if (networkClient != null)
        {
            networkClient.Disconnect();
        }

        // 메인 메뉴 씬 로드
        SceneManager.LoadScene(mainMenuSceneName);
    }

    public void SetPlayerNickname(string nickname)
    {
        playerNickname = nickname;
        Debug.Log($"[GameManager] Nickname 설정됨: {nickname}");
    }

    public string GetPlayerNickname()
    {
        if (!string.IsNullOrEmpty(playerNickname))
            return playerNickname;

        if (PlayerPrefs.HasKey("PlayerNickname"))
        {
            playerNickname = PlayerPrefs.GetString("PlayerNickname");
            return playerNickname;
        }

        return "Player";
    }

    // 게임 종료
    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}