#pragma once

#include <iostream>
#include <WinSock2.h>
#include <ws2tcpip.h>
#include <thread>
#include <map>
#include <mutex>

#pragma comment(lib, "ws2_32.lib")

enum PacketType
{
	PLAYER_ID_ASSIGN = 0,
	PLAYER_JOIN = 1,
	PLAYER_MOVE = 2,
	PLAYER_LEAVE = 3
};

struct PacketHeader
{
	int size;
	int type;
};

struct PlayerIdPacket
{
	PacketHeader header;
	int playerId;
};

struct PlayerMovePacket
{
	PacketHeader header;
	int playerId;
	float x;
	float y;
};

struct ClientInfo
{
	SOCKET socket;
	int playerId;
	float x, y;
	WSAOVERLAPPED overlapped;
	WSABUF wsaBuf;
	char buffer[1024];

	ClientInfo();
};

class Server
{
private:
	HANDLE hCompletionPort;
	SOCKET listenSocket;
	std::map<int, ClientInfo*> clients;
	std::mutex clientsMutex;
	int nextPlayerId;
	bool isRunning;

	// 쓰레드 관리
	void AcceptThread();
	void WorkerThread();

	// 패킷 처리
	void StartReceive(ClientInfo*);
	void ProcessPacket(ClientInfo*, DWORD);
	void HandlePlayerMove(ClientInfo*, PlayerMovePacket*);

	void BroadcastNewPlayer(ClientInfo*);
	void SendExistingPlayers(ClientInfo*);
	void DisconnectClient(ClientInfo*);

public:
	Server();
	~Server();

	// 서버 제어
	bool Start(int port);
	void Stop();

	// 상태 확인
	int GetConnectedPlayerCount();
	bool IsRunning() const { return isRunning; }
};