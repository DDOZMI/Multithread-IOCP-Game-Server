#include "server.h"

ClientInfo::ClientInfo()
{
	ZeroMemory(&overlapped, sizeof(overlapped));
	wsaBuf.buf = buffer;
	wsaBuf.len = sizeof(buffer);
	x = 0.0f;
	y = 0.0f;
}

Server::Server() : nextPlayerId(1), isRunning(false)
{
	WSADATA wsaData;
	WSAStartup(MAKEWORD(2, 2), &wsaData);

	hCompletionPort = CreateIoCompletionPort(INVALID_HANDLE_VALUE, NULL, 0, 0);

	// 소켓 생성
	listenSocket = WSASocket(AF_INET, SOCK_STREAM, 0, NULL, 0, WSA_FLAG_OVERLAPPED);
}

Server::~Server()
{
	Stop();
	closesocket(listenSocket);
	CloseHandle(hCompletionPort);
	WSACleanup();
};

bool Server::Start(int port)
{
	// 바인드
	sockaddr_in serverAddr;
	serverAddr.sin_family = AF_INET;
	serverAddr.sin_port = htons(port);
	serverAddr.sin_addr.s_addr = INADDR_ANY;

	if (bind(listenSocket, (sockaddr*)&serverAddr, sizeof(serverAddr)) == SOCKET_ERROR)
	{
		std::cout << "Bind failed" << std::endl;
		return false;
	}

	// 리슨
	if (listen(listenSocket, SOMAXCONN) == SOCKET_ERROR)
	{
		std::cout << "Listen failed" << std::endl;
		return false;
	}

	isRunning = true;
	std::cout << "Server started on port " << port << std::endl;

	// worker 쓰레드 시작
	int numThreads = std::thread::hardware_concurrency();
	for (int i = 0; i < numThreads; ++i)
	{
		std::thread workerThread(&Server::WorkerThread, this);
		workerThread.detach();
	}

	// accept 쓰레드 시작
	std::thread acceptThread(&Server::AcceptThread, this);
	acceptThread.detach();

	return true;
}

// 서버 중지
void Server::Stop()
{
	if (!isRunning) return;

	isRunning = false;

	std::lock_guard<std::mutex> lock(clientsMutex);
	for (auto& pair : clients)
	{
		closesocket(pair.second->socket);
		delete pair.second;
	}
	clients.clear();

	std::cout << "server stopped" << std::endl;
}

// 연결된 플레이어 수 반환
int Server::GetConnectedPlayerCount()
{
	std::lock_guard<std::mutex> lock(clientsMutex);
	return static_cast<int>(clients.size());
}

// Accept 쓰레드
void Server::AcceptThread()
{
	while (isRunning)
	{
		SOCKET clientSocket = accept(listenSocket, NULL, NULL);
		if (clientSocket == INVALID_SOCKET || !isRunning) continue;

		// 클라이언트 정보 생성
		ClientInfo* client = new ClientInfo();
		client->socket = clientSocket;
		client->playerId = nextPlayerId++;

		// 포트에 등록
		CreateIoCompletionPort((HANDLE)clientSocket, hCompletionPort, (ULONG_PTR)client, 0);

		// 클라이언트 목록에 추가 (먼저 추가해야 기존 플레이어 목록에 포함되지 않음)
		{
			std::lock_guard<std::mutex> lock(clientsMutex);
			clients[client->playerId] = client;
		}

		std::cout << "Player" << client->playerId << "connected (Total: " << GetConnectedPlayerCount() << ")" << std::endl;

		// 기존 플레이어들 정보를 새 클라이언트에게 전송
		SendExistingPlayers(client);

		// 그 다음 플레이어 ID 할당
		PlayerIdPacket idPacket;
		idPacket.header.size = sizeof(PlayerIdPacket);
		idPacket.header.type = PLAYER_ID_ASSIGN;
		idPacket.playerId = client->playerId;
		send(clientSocket, (char*)&idPacket, sizeof(idPacket), 0);

		// 마지막으로 다른 플레이어들에게 새 플레이어 알림
		BroadcastNewPlayer(client);

		// 수신 시작
		StartReceive(client);
	}
}

// Worker 쓰레드
void Server::WorkerThread()
{
	DWORD bytesTransferred;
	ULONG_PTR completionKey;
	LPOVERLAPPED overlapped;

	while (isRunning)
	{
		bool result = GetQueuedCompletionStatus(hCompletionPort, &bytesTransferred, &completionKey, &overlapped, 1000); // timeout 1초

		if (!result)
		{
			if (GetLastError() == WAIT_TIMEOUT) continue;
			break;
		}

		ClientInfo* client = (ClientInfo*)completionKey;

		if (bytesTransferred == 0)
		{
			DisconnectClient(client);
			continue;
		}

		ProcessPacket(client, bytesTransferred);
		StartReceive(client);
	}
}

// 수신 시작
void Server::StartReceive(ClientInfo* client)
{
	ZeroMemory(&client->overlapped, sizeof(client->overlapped));
	DWORD flags = 0;

	int result = WSARecv(client->socket, &client->wsaBuf, 1, NULL, &flags, &client->overlapped, NULL);

	if (result == SOCKET_ERROR && WSAGetLastError() != WSA_IO_PENDING)
	{
		// 수신 실패
		DisconnectClient(client);
	}
}

// 패킷 처리
void Server::ProcessPacket(ClientInfo* client, DWORD bytesReceived)
{
	if (bytesReceived < sizeof(PacketHeader)) return;

	PacketHeader* header = (PacketHeader*)client->buffer;

	switch (header->type)
	{
	case PLAYER_MOVE:
		if (bytesReceived >= sizeof(PlayerMovePacket))
		{
			HandlePlayerMove(client, (PlayerMovePacket*)client->buffer);
		}
		break;
	}
}

// 플레이어 이동 처리
void Server::HandlePlayerMove(ClientInfo* client, PlayerMovePacket* packet)
{
	// 플레이어 위치 업데이트
	client->x = packet->x;
	client->y = packet->y;

	// 패킷의 id를 실제 클라이언트 id로 설정
	packet->playerId = client->playerId;

	// 다른 클라이언트에게도 이동 정보 전송
	std::lock_guard<std::mutex> lock(clientsMutex);
	for (auto& pair : clients)
	{
		if (pair.second != client)
		{
			send(pair.second->socket, (char*)packet, sizeof(PlayerMovePacket), 0);
		}
	}
}

// 새 플레이어 정보 전송
void Server::BroadcastNewPlayer(ClientInfo* newClient)
{
	PlayerMovePacket packet;
	packet.header.size = sizeof(PlayerMovePacket);
	packet.header.type = PLAYER_JOIN;
	packet.playerId = newClient->playerId;
	packet.x = newClient->x;
	packet.y = newClient->y;

	std::lock_guard<std::mutex> lock(clientsMutex);
	for (auto& pair : clients)
	{
		if (pair.second != newClient)
		{
			send(pair.second->socket, (char*)&packet, sizeof(packet), 0);
		}
	}
}

// 기존 플레이어 정보 전송
void Server::SendExistingPlayers(ClientInfo* client)
{
	std::lock_guard<std::mutex> lock(clientsMutex);

	std::cout << "Sending existing players to Player " << client->playerId << std::endl;

	for (auto& pair : clients)
	{
		// 자신은 제외
		if (pair.second != client)
		{
			PlayerMovePacket packet;
			packet.header.size = sizeof(PlayerMovePacket);
			packet.header.type = PLAYER_JOIN;
			packet.playerId = pair.second->playerId;
			packet.x = pair.second->x;
			packet.y = pair.second->y;

			std::cout << "  Sending Player " << pair.second->playerId
				<< " info to Player " << client->playerId << std::endl;

			send(client->socket, (char*)&packet, sizeof(packet), 0);

			// 패킷 사이에 약간의 지연 추가 (선택사항)
			Sleep(10);
		}
	}

	std::cout << "Finished sending existing players to Player " << client->playerId << std::endl;

}

// 클라이언트 연결 종료
void Server::DisconnectClient(ClientInfo* client)
{
	PlayerMovePacket packet;
	packet.header.size = sizeof(PlayerMovePacket);
	packet.header.type = PLAYER_LEAVE;
	packet.playerId = client->playerId;

	{
		std::lock_guard<std::mutex> lock(clientsMutex);
		clients.erase(client->playerId);

		for (auto& pair : clients)
		{
			send(pair.second->socket, (char*)&packet, sizeof(packet), 0);
		}
	}

	closesocket(client->socket);
	delete client;
}