#include "server.h"
#include <conio.h>

int main()
{
	Server server;

	std::cout << "Starting Server..." << std::endl;

	if (!server.Start(8888))
	{
		std::cout << "Failed to start server." << std::endl;
		return -1;
	}

	std::cout << "Server started successfully." << std::endl;
	std::cout << "Press ESC to stop server" << std::endl;
	std::cout << std::endl;

	while (server.IsRunning())
	{
		if (_kbhit())
		{
			int key = _getch();

			if (key == 27)
			{
				std::cout << "Stopping server..." << std::endl;
				break;
			}
		}
	}

	Sleep(100);


	server.Stop();
	return 0;
}