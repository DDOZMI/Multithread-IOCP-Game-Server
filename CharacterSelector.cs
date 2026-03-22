using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class CharacterSelector : MonoBehaviour
{
    [Header("Character Settings")]
    public GameObject[] characterPrefabs;

    [Header("UI References")]
    public Button[] characterButtons;
    public NameInputUIManager nameInputUIManager;

    public static GameObject selectedCharacterPrefab = null;
    public static int selectedCharacterIndex = -1;

    void Start()
    {
        selectedCharacterPrefab = null;
        selectedCharacterIndex = -1;

        if (nameInputUIManager == null)
        {
            nameInputUIManager = FindFirstObjectByType<NameInputUIManager>();
        }

        for (int i = 0; i < characterButtons.Length; i++)
        {
            int index = i;
            characterButtons[i].onClick.RemoveAllListeners();
            characterButtons[i].onClick.AddListener(() => SelectCharacter(index));
        }

        Debug.Log("[CharacterSelector] 초기화. 캐릭터 선택을 기다리고 있습니다.");
    }

    void SelectCharacter(int characterIndex)
    {
        Debug.Log($"[CharacterSelector] 캐릭터 {characterIndex}가 선택되었습니다!");

        selectedCharacterIndex = characterIndex;

        if (characterIndex >= 0 && characterIndex < characterPrefabs.Length)
        {
            selectedCharacterPrefab = characterPrefabs[characterIndex];
            Debug.Log($"[CharacterSelector] 캐릭터 프리팹 설정: {selectedCharacterPrefab.name}");

            // PlayerPrefs에 저장
            PlayerPrefs.SetInt("SelectedCharacter", characterIndex);
            PlayerPrefs.Save();

            // GameManager가 있다면 캐릭터 정보 설정
            GameManager gameManager = FindFirstObjectByType<GameManager>();
            if (gameManager != null)
            {
                gameManager.SetSelectedCharacter(characterIndex, characterPrefabs[characterIndex]);
            }

            // 닉네임 입력 UI 표시
            if (nameInputUIManager != null)
            {
                nameInputUIManager.ShowNameInputUI(characterIndex, characterPrefabs[characterIndex]);
            }
            else
            {
                Debug.LogError("[CharacterSelector] NameInputUIManager를 찾을 수 없습니다. Gameplay를 로드합니다.");
                LoadGameplayScene();
            }
        }
        else
        {
            Debug.LogError($"[CharacterSeletor] 부적절한 캐릭터 인덱스: {characterIndex}");
        }
    }

    void LoadGameplayScene()
    {
        Debug.Log("[CharacterSelector] Gameplay를 로드합니다..");
        SceneManager.LoadScene("Gameplay");
    }

    public void ResetSelection()
    {
        selectedCharacterIndex = -1;
        selectedCharacterPrefab = null;
        PlayerPrefs.DeleteKey("SelectedCharacter");
        PlayerPrefs.DeleteKey("PlayerNickname");
    }
}