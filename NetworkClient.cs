using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using UnityEngine;
using ChatSystem;

[System.Serializable]
public struct PacketHeader
{
    public int size;
    public int type;
}

[Serializable]
public struct PlayerIdPacket
{
    public PacketHeader header;
    public int playerId;
}

[System.Serializable]
public struct PlayerMovePacket
{
    public PacketHeader header;
    public int playerId;
    public float x;
    public float y;
}

[System.Serializable]
public struct PlayerJoinPacket
{
    public PacketHeader header;
    public int playerId;
    public float x;
    public float y;
    public int characterType;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
    public byte[] nickname;  // 추가
}


[System.Serializable]
public struct PlayerCharacterPacket
{
    public PacketHeader header;
    public int playerId;
    public int characterType;
}

[System.Serializable]
public struct ChatMessagePacket
{
    public PacketHeader header;
    public int playerId;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
    public byte[] message;
}

[System.Serializable]
public struct PlayerNicknamePacket
{
    public PacketHeader header;
    public int playerId;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
    public byte[] nickname;
}

public enum PacketType
{
    PLAYER_ID_ASSIGN = 0,
    PLAYER_JOIN = 1,
    PLAYER_MOVE = 2,
    PLAYER_LEAVE = 3,
    HEARTBEAT = 4,
    PLAYER_CHARACTER_INFO = 5,
    CHAT_MESSAGE = 6,
    PLAYER_NICKNAME = 7
}

public class NetworkClient : MonoBehaviour
{
    [Header("Network Settings")]
    public string serverIP = "127.0.0.1";
    public int serverPort = 8888;

    [Header("Character Prefabs")]
    public GameObject[] characterPrefabs; // 캐릭터 프리팹 배열 (Inspector에서 할당)
    private GameObject selectedCharacterPrefab; // 선택된 캐릭터 프리팹
    private int myCharacterType = 0; // 내 캐릭터 타입

    [Header("UI Prefabs")]
    public GameObject namePanelPrefab;

    private TcpClient tcpClient;
    private NetworkStream stream;
    private byte[] receiveBuffer = new byte[1024];
    private bool isConnected = false;
    private GameObject myPlayer;

    public bool IsConnected => isConnected;

    // 플레이어 관리
    public int myPlayerId = -1;
    private Dictionary<int, GameObject> otherPlayers = new Dictionary<int, GameObject>();
    private Dictionary<int, string> playerNicknames = new Dictionary<int, string>();

    private Queue<byte[]> packetQueue = new Queue<byte[]>();
    private bool isProcessingPackets = false;

    private void Start()
    {
        Debug.Log("[NetworkClient] NetworkClient 시작. 캐릭터 선택을 기다리고 있습니다.");
    }

    public GameObject GetPlayerObject(int playerId)
    {
        if (playerId == myPlayerId)
        {
            return myPlayer;
        }

        otherPlayers.TryGetValue(playerId, out GameObject player);
        return player;
    }

    // 선택된 캐릭터 프리팹 설정
    public void SetSelectedCharacterPrefab(GameObject prefab)
    {
        selectedCharacterPrefab = prefab;

        // 프리팹 배열에서 인덱스 찾기
        for (int i = 0; i < characterPrefabs.Length; i++)
        {
            if (characterPrefabs[i] == prefab)
            {
                myCharacterType = i;
                break;
            }
        }

        Debug.Log($"[NetworkClient] 캐릭터 prefab을 {prefab?.name}로 설정했습니다. 타입: {myCharacterType}");
    }

    // 캐릭터 타입에 따른 프리팹 가져오기
    private GameObject GetCharacterPrefab(int characterType)
    {
        if (characterType >= 0 && characterType < characterPrefabs.Length)
        {
            return characterPrefabs[characterType];
        }

        // 기본 프리팹 반환
        return characterPrefabs.Length > 0 ? characterPrefabs[0] : null;
    }

    // 서버 연결
    public void ConnectToServer()
    {
        if (isConnected)
        {
            Debug.LogWarning("[NetworkClient] 이미 서버에 연결되어 있습니다!");
            return;
        }

        if (selectedCharacterPrefab == null)
        {
            Debug.LogError("[NetworkClient] 서버에 연결할 수 없습니다. 캐릭터 prefab이 선택되지 않았습니다!");
            return;
        }

        Debug.Log($"[NetworkClient] 캐릭터 {selectedCharacterPrefab.name}로 서버에 연결합니다.");

        try
        {
            tcpClient = new TcpClient();
            tcpClient.Connect(serverIP, serverPort);
            stream = tcpClient.GetStream();
            isConnected = true;

            Debug.Log("[NetworkClient] 서버에 성공적으로 연결되었습니다!");

            // 수신 시작
            BeginReceive();
        }
        catch (Exception e)
        {
            Debug.LogError("[NetworkClient] 서버 연결에 실패했습니다: " + e.Message);
            isConnected = false;
        }
    }

    private void BeginReceive()
    {
        if (stream != null && stream.CanRead)
        {
            stream.BeginRead(receiveBuffer, 0, receiveBuffer.Length, OnReceiveData, null);
        }
    }

    private void OnReceiveData(IAsyncResult result)
    {
        try
        {
            int bytesRead = stream.EndRead(result);

            if (bytesRead > 0)
            {
                // 패킷을 큐에 추가
                byte[] data = new byte[bytesRead];
                Array.Copy(receiveBuffer, data, bytesRead);

                // Unity는 메인 스레드에서 GameObject 조작해야 함
                UnityMainThreadDispatcher.Instance().Enqueue(() => {
                    lock (packetQueue)
                    {
                        packetQueue.Enqueue(data);
                    }
                });

                // 다음 수신 준비
                BeginReceive();
            }
        }
        catch (Exception e)
        {
            Debug.LogError("[NetworkClient] 받은 에러: " + e.Message);
            Disconnect();
        }
    }

    private void Update()
    {
        // 연결되어 있을 때만 패킷 처리
        if (isConnected)
        {
            ProcessPacketQueue();
        }
    }

    private void ProcessPacketQueue()
    {
        if (isProcessingPackets) return;

        lock (packetQueue)
        {
            if (packetQueue.Count == 0) return;

            isProcessingPackets = true;

            while (packetQueue.Count > 0)
            {
                byte[] data = packetQueue.Dequeue();
                ProcessPacket(data);
            }

            isProcessingPackets = false;
        }
    }

    private void ProcessPacket(byte[] data)
    {
        if (data.Length < 8) return;

        PacketHeader header = ByteArrayToStructure<PacketHeader>(data);

        switch ((PacketType)header.type)
        {
            case PacketType.PLAYER_ID_ASSIGN:
                HandlePlayerIdAssign(data);
                break;
            case PacketType.PLAYER_JOIN:
                HandlePlayerJoin(data);
                break;
            case PacketType.PLAYER_MOVE:
                HandlePlayerMove(data);
                break;
            case PacketType.PLAYER_LEAVE:
                HandlePlayerLeave(data);
                break;
            case PacketType.PLAYER_CHARACTER_INFO:
                break;
            case PacketType.CHAT_MESSAGE:
                HandleChatMessage(data);
                break;
            case PacketType.PLAYER_NICKNAME:
                HandlePlayerNickname(data);
                break;
        }
    }

    private void HandlePlayerIdAssign(byte[] data)
    {
        PlayerIdPacket packet = ByteArrayToStructure<PlayerIdPacket>(data);
        myPlayerId = packet.playerId;
        Debug.Log($"[NetworkClient] 등록 플레이어 ID: {myPlayerId}");

        if (myPlayer == null)
        {
            GameObject prefabToUse = selectedCharacterPrefab;

            if (prefabToUse == null)
            {
                Debug.LogWarning("[NetworkClient] 선택한 플레이어 Prefab이 없습니다. 기본으로 선택합니다.");
                prefabToUse = GetCharacterPrefab(0);
            }

            if (prefabToUse != null)
            {
                myPlayer = Instantiate(prefabToUse, new Vector3(0, 0, 0), Quaternion.identity);
                myPlayer.name = "Player_" + myPlayerId + " (Me)";

                PlayerController controller = myPlayer.GetComponent<PlayerController>();
                if (controller != null)
                {
                    controller.isLocalPlayer = true;
                }

                // 닉네임 표시
                string nickname = GameManager.Instance?.GetPlayerNickname() ?? "Player";
                playerNicknames[myPlayerId] = nickname;

                PlayerNameDisplay nameDisplay = myPlayer.AddComponent<PlayerNameDisplay>();
                nameDisplay.Initialize(myPlayer.transform, nickname, namePanelPrefab);

                Debug.Log($"[NetworkClient] 로컬 플레이어 생성 ID: {myPlayerId}, Nickname: {nickname}");

                // 캐릭터 타입과 닉네임 전송
                SendCharacterType();
                SendNickname(nickname);
            }
        }
    }

    private void HandlePlayerJoin(byte[] data)
    {
        if (data.Length < Marshal.SizeOf(typeof(PlayerJoinPacket)))
        {
            Debug.LogWarning("[NetworkClient] 불완전한 PlayerJoin 패킷을 받았습니다.");
            return;
        }

        PlayerJoinPacket packet = ByteArrayToStructure<PlayerJoinPacket>(data);

        if (packet.playerId == myPlayerId)
        {
            Debug.Log("[NetworkClient] 로컬 플레이어의 join 패킷은 무시합니다: " + packet.playerId);
            return;
        }

        if (!otherPlayers.ContainsKey(packet.playerId))
        {
            GameObject prefabToUse = GetCharacterPrefab(packet.characterType);

            if (prefabToUse == null)
            {
                Debug.LogError($"[NetworkClient] {packet.characterType}에 사용 가능한 prefab이 없습니다!");
                return;
            }

            GameObject newPlayer = Instantiate(prefabToUse);
            newPlayer.transform.position = new Vector3(packet.x, packet.y, 0);
            newPlayer.name = "Player_" + packet.playerId + " (Other, Type: " + packet.characterType + ")";

            PlayerController controller = newPlayer.GetComponent<PlayerController>();
            if (controller != null)
            {
                controller.isLocalPlayer = false;
                controller.SetPosition(packet.x, packet.y);
            }

            // 닉네임 추출
            string nickname = System.Text.Encoding.UTF8.GetString(packet.nickname).TrimEnd('\0');
            if (string.IsNullOrEmpty(nickname))
            {
                nickname = $"Player{packet.playerId}";
            }

            playerNicknames[packet.playerId] = nickname;

            PlayerNameDisplay nameDisplay = newPlayer.AddComponent<PlayerNameDisplay>();
            nameDisplay.Initialize(newPlayer.transform, nickname, namePanelPrefab);

            otherPlayers[packet.playerId] = newPlayer;

            if (ChatController.Instance != null)
            {
                ChatController.Instance.RegisterPlayer(packet.playerId, newPlayer);
            }

            Debug.Log($"[NetworkClient] 플레이어 {packet.playerId} 이(가) {nickname} 으로 접속했습니다.");
        }
    }

    private void HandlePlayerMove(byte[] data)
    {
        PlayerMovePacket packet = ByteArrayToStructure<PlayerMovePacket>(data);

        // 내 움직임은 무시 (이미 로컬에서 처리됨)
        if (packet.playerId == myPlayerId) return;

        // 다른 플레이어 위치 업데이트
        if (otherPlayers.ContainsKey(packet.playerId))
        {
            PlayerController controller = otherPlayers[packet.playerId].GetComponent<PlayerController>();
            if (controller != null)
            {
                controller.SetNetworkPosition(packet.x, packet.y);
            }
        }
        else
        {
            // 플레이어가 존재하지 않으면 생성 (JOIN 패킷을 놓쳤을 경우)
            Debug.LogWarning($"[NetworkClient] 플레이어 {packet.playerId}이(가) 존재하지 않습니다. 기본 캐릭터를 선택합니다.");

            GameObject prefabToUse = GetCharacterPrefab(0);
            if (prefabToUse != null)
            {
                GameObject newPlayer = Instantiate(prefabToUse);
                newPlayer.transform.position = new Vector3(packet.x, packet.y, 0);
                newPlayer.name = "Player_" + packet.playerId + " (Other)";

                // Layer 명시적 설정
                newPlayer.layer = LayerMask.NameToLayer("Player");

                PlayerController controller = newPlayer.GetComponent<PlayerController>();
                if (controller != null)
                {
                    controller.isLocalPlayer = false;
                    controller.SetPosition(packet.x, packet.y);
                }

                otherPlayers[packet.playerId] = newPlayer;
            }
        }
    }

    private void HandlePlayerLeave(byte[] data)
    {
        PlayerMovePacket packet = ByteArrayToStructure<PlayerMovePacket>(data);

        if (otherPlayers.ContainsKey(packet.playerId))
        {
            // ===== 추가: ChatController에서 정리 =====
            if (ChatController.Instance != null)
            {
                ChatController.Instance.UnregisterPlayer(packet.playerId);
                ChatController.Instance.HideSpeechBubbleForPlayer(otherPlayers[packet.playerId]);
            }

            Destroy(otherPlayers[packet.playerId]);
            otherPlayers.Remove(packet.playerId);
            Debug.Log($"[NetworkClient] 플레이어 {packet.playerId}이(가) 떠났습니다.");
        }
    }

    // 채팅 메시지 처리 (새로 추가)
    private void HandleChatMessage(byte[] data)
    {
        if (data.Length < Marshal.SizeOf(typeof(ChatMessagePacket)))
        {
            Debug.LogWarning("[NetworkClient] 완성되지 않은 PlayerJoin 패킷을 받았습니다.");
            return;
        }

        ChatMessagePacket packet = ByteArrayToStructure<ChatMessagePacket>(data);
        string message = System.Text.Encoding.UTF8.GetString(packet.message).TrimEnd('\0');
        Debug.Log($"[NetworkClient] 플레이어 {packet.playerId}로부터 메세지를 받았습니다: {message}");

        GameObject targetPlayer = GetPlayerObject(packet.playerId);

        if (targetPlayer != null)
        {
            // ChatMessageData 생성 및 이벤트 발행
            ChatMessageData chatData = new ChatMessageData(
                packet.playerId,
                message,
                targetPlayer
            );

            // 이벤트 발행, SpeechBubbleManager가 자동으로 처리
            ChatEvents.RaiseChatMessageReceived(chatData);

            // ChatController에 플레이어 등록
            if (ChatController.Instance != null)
            {
                ChatController.Instance.RegisterPlayer(packet.playerId, targetPlayer);
            }
        }
        else
        {
            Debug.LogWarning($"[NetworkClient] {packet.playerId} 플레이어의 말풍선을 띄울 수 없습니다. 플레이어를 찾지 못했습니다.");
        }
    }

    private void HandlePlayerNickname(byte[] data)
    {
        if (data.Length < Marshal.SizeOf(typeof(PlayerNicknamePacket)))
        {
            Debug.LogWarning("[NetworkClient] 불완전한 PlayerNickname 패킷을 받았습니다.");
            return;
        }

        PlayerNicknamePacket packet = ByteArrayToStructure<PlayerNicknamePacket>(data);
        string nickname = System.Text.Encoding.UTF8.GetString(packet.nickname).TrimEnd('\0');

        playerNicknames[packet.playerId] = nickname;
        Debug.Log($"[NetworkClient] 플레이어 {packet.playerId}이(가) 닉네임 {nickname}으로 설정되었습니다.");

        // 이미 생성된 플레이어의 닉네임 업데이트
        if (otherPlayers.TryGetValue(packet.playerId, out GameObject playerObj))
        {
            PlayerNameDisplay nameDisplay = playerObj.GetComponent<PlayerNameDisplay>();
            if (nameDisplay != null)
            {
                nameDisplay.UpdateNickname(nickname);
            }
        }
    }

    // 캐릭터 타입 정보 전송 (서버에 내 캐릭터 타입 알림)
    private void SendCharacterType()
    {
        if (!isConnected || myPlayerId == -1) return;

        PlayerCharacterPacket packet = new PlayerCharacterPacket();
        packet.header.size = Marshal.SizeOf(typeof(PlayerCharacterPacket));
        packet.header.type = (int)PacketType.PLAYER_CHARACTER_INFO;
        packet.playerId = myPlayerId;
        packet.characterType = myCharacterType;

        byte[] data = StructureToByteArray(packet);

        try
        {
            stream.Write(data, 0, data.Length);
            Debug.Log($"[NetworkClient] 캐릭터 타입 {myCharacterType}를 서버로 보냅니다.");
        }
        catch (Exception e)
        {
            Debug.LogError("[NetworkClient] 캐릭터 타입 전송 실패: " + e.Message);
        }
    }

    // 서버로 움직임 전송
    public void SendPlayerMove(float x, float y)
    {
        if (!isConnected || myPlayerId == -1) return;

        PlayerMovePacket packet = new PlayerMovePacket();
        packet.header.size = Marshal.SizeOf(typeof(PlayerMovePacket));
        packet.header.type = (int)PacketType.PLAYER_MOVE;
        packet.playerId = myPlayerId;
        packet.x = x;
        packet.y = y;

        byte[] data = StructureToByteArray(packet);

        try
        {
            stream.Write(data, 0, data.Length);
        }
        catch (Exception e)
        {
            Debug.LogError("Send error: " + e.Message);
            Disconnect();
        }
    }

    // 채팅 메시지 전송 (새로 추가)
    public void SendChatMessage(string message)
    {
        if (!isConnected || myPlayerId == -1)
        {
            Debug.LogWarning("[NetworkClient] 채팅 메세지를 보낼 수 없습니다. 연결되지 않았거나 플레이어ID가 없습니다.");
            return;
        }

        if (string.IsNullOrEmpty(message) || message.Length > 250)
        {
            Debug.LogWarning("[NetworkClient] 채팅 메세지가 없거나 너무 깁니다.");
            return;
        }

        ChatMessagePacket packet = new ChatMessagePacket();
        packet.header.size = Marshal.SizeOf(typeof(ChatMessagePacket));
        packet.header.type = (int)PacketType.CHAT_MESSAGE;
        packet.playerId = myPlayerId;

        // 메시지를 UTF-8로 인코딩
        byte[] messageBytes = System.Text.Encoding.UTF8.GetBytes(message);
        packet.message = new byte[256];
        Array.Copy(messageBytes, packet.message, Math.Min(messageBytes.Length, 255));

        byte[] data = StructureToByteArray(packet);

        try
        {
            stream.Write(data, 0, data.Length);
            Debug.Log($"[NetworkClient] 메세지를 보냅니다: {message}");
        }
        catch (Exception e)
        {
            Debug.LogError("[NetworkClient] 메세지 전송 오류: " + e.Message);
        }
    }

    private void SendNickname(string nickname)
    {
        if (!isConnected || myPlayerId == -1) return;

        PlayerNicknamePacket packet = new PlayerNicknamePacket();
        packet.header.size = Marshal.SizeOf(typeof(PlayerNicknamePacket));
        packet.header.type = (int)PacketType.PLAYER_NICKNAME;
        packet.playerId = myPlayerId;

        byte[] nicknameBytes = System.Text.Encoding.UTF8.GetBytes(nickname);
        packet.nickname = new byte[64];
        Array.Copy(nicknameBytes, packet.nickname, Math.Min(nicknameBytes.Length, 63));

        byte[] data = StructureToByteArray(packet);

        try
        {
            stream.Write(data, 0, data.Length);
            Debug.Log($"[NetworkClient] 서버에 보낸 닉네임: {nickname}");
        }
        catch (Exception e)
        {
            Debug.LogError("[NetworkClient] 닉네임 전송 오류: " + e.Message);
        }
    }

    public void Disconnect()
    {
        if (!isConnected) return;

        isConnected = false;

        foreach (var player in otherPlayers.Values)
        {
            if (player != null)
                Destroy(player);
        }
        otherPlayers.Clear();
        playerNicknames.Clear();

        if (myPlayer != null)
        {
            Destroy(myPlayer);
            myPlayer = null;
        }

        if (stream != null)
        {
            stream.Close();
            stream = null;
        }

        if (tcpClient != null)
        {
            tcpClient.Close();
            tcpClient = null;
        }

        myPlayerId = -1;

        Debug.Log("[NetworkClient] 서버로부터 접속이 끊겼습니다.");
    }

    private void OnApplicationQuit()
    {
        Disconnect();
    }

    private void OnDestroy()
    {
        Disconnect();
    }

    private static byte[] StructureToByteArray(object obj)
    {
        int len = Marshal.SizeOf(obj);
        byte[] arr = new byte[len];
        IntPtr ptr = Marshal.AllocHGlobal(len);
        Marshal.StructureToPtr(obj, ptr, true);
        Marshal.Copy(ptr, arr, 0, len);
        Marshal.FreeHGlobal(ptr);
        return arr;
    }

    private static T ByteArrayToStructure<T>(byte[] bytes) where T : struct
    {
        T stuff;
        GCHandle handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
        try
        {
            stuff = (T)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(T));
        }
        finally
        {
            handle.Free();
        }
        return stuff;
    }
}