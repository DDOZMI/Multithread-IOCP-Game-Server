using UnityEngine;

// 채팅 입력 처리 인터페이스
public interface IChatInputHandler
{
    bool IsChatInputActive { get; }
    void ShowChatInput();
    void HideChatInput();
}

// 말풍선 관리 인터페이스
public interface ISpeechBubbleManager
{
    void ShowSpeechBubble(GameObject player, string message);
    void HideSpeechBubble(GameObject player);
    void ClearAllBubbles();
}

// 채팅 네트워크 인터페이스
public interface IChatNetworkHandler
{
    void SendChatMessage(string message);
    bool IsConnected { get; }
}

// 말풍선 Factory 인터페이스
public interface ISpeechBubbleFactory
{
    GameObject CreateSpeechBubble(string message, Transform parent);
    void ConfigureBubble(GameObject bubble, string message);
}