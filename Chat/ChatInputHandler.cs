using UnityEngine;
using TMPro;
using ChatSystem;

public class ChatInputHandler : MonoBehaviour, IChatInputHandler
{
    [Header("UI References")]
    [SerializeField] private GameObject chatInputPanel;
    [SerializeField] private TMP_InputField chatInputField;

    private bool isChatInputActive = false;

    public bool IsChatInputActive => isChatInputActive;

    private void Awake()
    {
        ValidateReferences();
        InitializeInputField();
        HideChatInput();
    }

    private void OnEnable()
    {
        // InputField event subscribe
        if (chatInputField != null)
        {
            chatInputField.onSubmit.AddListener(OnInputFieldSubmit);
        }
    }

    private void OnDisable()
    {
        if (chatInputField != null)
        {
            chatInputField.onSubmit.RemoveListener(OnInputFieldSubmit);
        }
    }

    private void Update()
    {
        HandleKeyboardInput();
    }

    private void ValidateReferences()
    {
        if (chatInputPanel == null)
        {
            Debug.LogError($"[ChatInputHandler] [{GetType().Name}] Chat input panel is not assigned!");
        }

        if (chatInputField == null)
        {
            Debug.LogError($"[ChatInputHandler] [{GetType().Name}] Chat input field is not assigned!");
        }
    }

    private void InitializeInputField()
    {
        if (chatInputField != null)
        {
            // UTF-8 서버 설정과 동일하게 설정(256)
            chatInputField.characterLimit = 256;
        }
    }

    private void HandleKeyboardInput()
    {
        // Enter 키 입력 처리
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            if (!isChatInputActive)
            {
                ShowChatInput();
            }
            else if (!string.IsNullOrEmpty(GetCurrentText()))
            {
                SubmitChat();
            }
            else
            {
                HideChatInput();
            }
        }

        // ESC 키로 채팅 입력 취소
        if (Input.GetKeyDown(KeyCode.Escape) && isChatInputActive)
        {
            HideChatInput();
        }
    }

    public void ShowChatInput()
    {
        if (chatInputPanel == null || chatInputField == null) return;

        chatInputPanel.SetActive(true);
        isChatInputActive = true;

        chatInputField.text = "";
        chatInputField.ActivateInputField();
        chatInputField.Select();

        Debug.Log($"[ChatInputHandler] [{GetType().Name}] 채팅 입력이 활성화되었습니다.");
    }

    public void HideChatInput()
    {
        if (chatInputPanel == null) return;

        chatInputPanel.SetActive(false);
        isChatInputActive = false;

        if (chatInputField != null)
        {
            chatInputField.text = "";
            chatInputField.DeactivateInputField();
        }

        Debug.Log($"[ChatInputHandler] [{GetType().Name}] 채팅 입력이 비활성화되었습니다.");
    }

    private void SubmitChat()
    {
        string message = GetCurrentText();

        if (string.IsNullOrEmpty(message))
        {
            HideChatInput();
            return;
        }

        // 메시지 전달 이벤트 발행
        ChatEvents.RaiseChatSubmitted(message);

        HideChatInput();

        Debug.Log($"[ChatInputHandler] [{GetType().Name}] 메세지 전송됨: {message}");
    }

    private void OnInputFieldSubmit(string text)
    {
        // InputField의 onSubmit event 처리
        if (isChatInputActive && !string.IsNullOrEmpty(text))
        {
            SubmitChat();
        }
    }

    private string GetCurrentText()
    {
        return chatInputField != null ? chatInputField.text.Trim() : string.Empty;
    }

    public void SetInputActive(bool active)
    {
        if (active)
            ShowChatInput();
        else
            HideChatInput();
    }
}