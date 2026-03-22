using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class NameInputUIManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject nameInputUIPanel;
    [SerializeField] private TMP_InputField nameInputField;
    [SerializeField] private Button okButton;
    [SerializeField] private Button backButton;

    [Header("Settings")]
    [SerializeField] private int maxNameLength = 8;

    private int selectedCharacterIndex = -1;
    private GameObject selectedCharacterPrefab = null;

    private void Start()
    {
        // 초기에는 UI 숨김
        HideNameInputUI();

        // 버튼 이벤트 연결
        if (okButton != null)
            okButton.onClick.AddListener(OnOkButtonClicked);

        if (backButton != null)
            backButton.onClick.AddListener(OnBackButtonClicked);

        // InputField 설정
        if (nameInputField != null)
            nameInputField.characterLimit = maxNameLength;
    }

    public void ShowNameInputUI(int characterIndex, GameObject characterPrefab)
    {
        selectedCharacterIndex = characterIndex;
        selectedCharacterPrefab = characterPrefab;

        if (nameInputUIPanel != null)
        {
            nameInputUIPanel.SetActive(true);

            // InputField 초기화 및 포커스
            if (nameInputField != null)
            {
                nameInputField.text = "";
                nameInputField.ActivateInputField();
            }
        }
    }

    public void HideNameInputUI()
    {
        if (nameInputUIPanel != null)
            nameInputUIPanel.SetActive(false);

        selectedCharacterIndex = -1;
        selectedCharacterPrefab = null;
    }

    private void OnOkButtonClicked()
    {
        if (nameInputField == null || string.IsNullOrWhiteSpace(nameInputField.text))
        {
            Debug.LogWarning("[NameInputUIManager] 닉네임을 입력해주세요!");
            return;
        }

        string nickname = nameInputField.text.Trim();

        // 닉네임 저장
        PlayerPrefs.SetString("PlayerNickname", nickname);
        PlayerPrefs.SetInt("SelectedCharacter", selectedCharacterIndex);
        PlayerPrefs.Save();

        // GameManager에 정보 전달
        GameManager gameManager = GameManager.Instance;
        if (gameManager != null)
        {
            gameManager.SetPlayerNickname(nickname);
            gameManager.SetSelectedCharacter(selectedCharacterIndex, selectedCharacterPrefab);
        }

        Debug.Log($"[NameInputUIManager] 닉네임 '{nickname}'과 캐릭터 {selectedCharacterIndex} 선택 완료");

        // Gameplay 씬으로 이동
        UnityEngine.SceneManagement.SceneManager.LoadScene("Gameplay");
    }

    private void OnBackButtonClicked()
    {
        Debug.Log("[NameInputUIManager] 캐릭터 선택 취소");
        HideNameInputUI();

        // CharacterSelector 초기화
        CharacterSelector selector = FindFirstObjectByType<CharacterSelector>();
        if (selector != null)
        {
            selector.ResetSelection();
        }
    }

    private void OnDestroy()
    {
        if (okButton != null)
            okButton.onClick.RemoveListener(OnOkButtonClicked);

        if (backButton != null)
            backButton.onClick.RemoveListener(OnBackButtonClicked);
    }
}