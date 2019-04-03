
#include "FrameWork\DataTransfer\WinTdpToSql.hpp"

#pragma comment(lib,"user32.lib")

#define debug(_operator) printf("operator: %d\n", _operator)

#include  <io.h>

int main()
{
	WSADATA wsaData;
	// 启动网络环境
	if (0 == WSAStartup(MAKEWORD(0x02, 0x02), &wsaData))
    {
        CHAR * ConfigPath = ".\\IdasCppServer.ini";
        // 读取配置文件，无则生成
        USHORT LocalPort, ThreadSum;
        CHAR Temp[6] = { 0 };

        // 读取配置信息
        GetPrivateProfileString("线程配置", "线程总数", "4", Temp, sizeof(Temp), ConfigPath);
        WritePrivateProfileString("线程配置", "线程总数", Temp, ConfigPath);
        sscanf_s(Temp, "%hd", &ThreadSum);

        GetPrivateProfileString("网络配置", "本地端口", "9954", Temp, sizeof(Temp), ConfigPath);
        WritePrivateProfileString("网络配置", "本地端口", Temp, ConfigPath);
        sscanf_s(Temp, "%hd", &LocalPort);

        PutInfo("读取配置如下：\n");
        printf("网络配置：本地端口:%hd 线程总数:%hd\n", LocalPort, ThreadSum);

        // 加载数据库配置
        CHAR * SqlPath = ".\\IdasCppServer.udl";
        if ((_access(SqlPath, 0)) != -1)
        {
            WinTranTdpToSql Sys;
            if (Sys.StartUp("File Name=IdasCppServer.udl", LocalPort, ThreadSum))
            {
                PutInfo("数据采集主机服务启动成功，按 ESC 退出程序！\n");
                while (true)
                {
                    // 按下ESC可以退出程序
                    if (GetAsyncKeyState(VK_ESCAPE))
                    {
                        break;
                    }
                    Sleep(1000);
                }
                Sys.CleanUp();
            }
            WSACleanup();
        }
        else
        {
            printf_s("数据库连接配置文件（IdasCppServer.udl）不存在，请手动创建。\n");
        }
	}
	else
	{
		PutInfo("WSAStartup服务环境异常，正尝试重置网络......\n");
		system("netsh winsock reset");
    }
    system("pause");
	return 0;
}
