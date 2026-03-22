using System;
using UnityEngine;

namespace ChatSystem
{
    // 채팅 메시지 데이터를 담는 형식 정의
    public class ChatMessageData
    {
        public int PlayerId { get; }
        public string Message { get; }
        public GameObject PlayerObject { get; }
        public DateTime Timestamp { get; }

        public ChatMessageData(int playerId, string message, GameObject playerObject = null)
        {
            PlayerId = playerId;
            Message = message;
            PlayerObject = playerObject;
            Timestamp = DateTime.Now;
        }
    }

    // 채팅 시스템 이벤트 정의
    public static class ChatEvents
    {
        // 채팅 입력 완료 이벤트
        public static event Action<string> OnChatSubmitted;

        // 네트워크로부터 채팅 메시지 수신 이벤트
        public static event Action<ChatMessageData> OnChatMessageReceived;

        // 로컬 플레이어 채팅 전송 요청 이벤트
        public static event Action<string> OnSendChatRequested;

        // 이벤트 발행 함수들
        public static void RaiseChatSubmitted(string message)
        {
            OnChatSubmitted?.Invoke(message);
        }

        public static void RaiseChatMessageReceived(ChatMessageData data)
        {
            OnChatMessageReceived?.Invoke(data);
        }

        public static void RaiseSendChatRequested(string message)
        {
            OnSendChatRequested?.Invoke(message);
        }
    }
}