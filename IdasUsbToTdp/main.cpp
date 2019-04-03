#include "FrameWork\DataTransfer\WinUsbToTdp.hpp"

#include <time.h>

void GetTime(uint32_t *sec, uint16_t *ms)
{
	*sec = time(NULL), *ms = GetTickCount() % 1000;
}

int CollectServer()
{
	// 启动WINDOWS网络环境
	WSADATA wsaData;
	if (0 == WSAStartup(MAKEWORD(0x02, 0x02), &wsaData))
    {
        CHAR * ConfigPath = ".\\IdasUsbToTdp.ini";
        // 读取配置文件，无则生成
        USHORT RemotePort, LocalPort, UsbPid, UsbVid, UsbPvn;
        CHAR RemoteIP[16] = { 0 };

        // 读取配置信息
        GetPrivateProfileString("USB配置", "PID", "0x9990", RemoteIP, sizeof(RemoteIP), ConfigPath);
        WritePrivateProfileString("USB配置", "PID", RemoteIP, ConfigPath);
        sscanf_s(RemoteIP, "%hx", &UsbPid);

        GetPrivateProfileString("USB配置", "VID", "0x0666", RemoteIP, sizeof(RemoteIP), ConfigPath);
        WritePrivateProfileString("USB配置", "VID", RemoteIP, ConfigPath);
        sscanf_s(RemoteIP, "%hx", &UsbVid);

        GetPrivateProfileString("USB配置", "PVN", "0x0200", RemoteIP, sizeof(RemoteIP), ConfigPath);
        WritePrivateProfileString("USB配置", "PVN", RemoteIP, ConfigPath);
        sscanf_s(RemoteIP, "%hx", &UsbPvn);

        GetPrivateProfileString("网络配置", "本地端口", "6666", RemoteIP, sizeof(RemoteIP), ConfigPath);
        WritePrivateProfileString("网络配置", "本地端口", RemoteIP, ConfigPath);
        sscanf_s(RemoteIP, "%hd", &LocalPort);

        GetPrivateProfileString("网络配置", "远端端口", "9954", RemoteIP, sizeof(RemoteIP), ConfigPath);
        WritePrivateProfileString("网络配置", "远端端口", RemoteIP, ConfigPath);
        sscanf_s(RemoteIP, "%hd", &RemotePort);

        GetPrivateProfileString("网络配置", "远端地址", "127.000.000.001", RemoteIP, sizeof(RemoteIP), ConfigPath);
        WritePrivateProfileString("网络配置", "远端地址", RemoteIP, ConfigPath);

        PutInfo("读取配置如下：\n");
        printf("网络配置：远端地址:%s 远端端口:%hd 本地端口:%hd\n", RemoteIP, RemotePort, LocalPort);
        printf("USB配置：Pid:0x%hX Vid:0x%hX Pvn:0x%hX\n", UsbPid, UsbVid, UsbPvn);

		WinTranUsbToTdp Sys;
        if (Sys.StartUp(RemoteIP, RemotePort, LocalPort, UsbPid, UsbVid, UsbPvn))
		{
			PutInfo("数据采集服务启动成功，按任意键停止服务\n");
			/*
			const uint8_t RsaDe = 43, RsaEn = 55;
			ZwTranInit();
			ZwDecode zp;
			ZwEncodeInit(&zp, RsaDe, 0xFF, (uint8_t *)"0123456789ABCDEF", 0, GetTime);
			ZwDecode zup;
			ZwDecodeInit(&zup, RsaDe);
			while (0)
			{
				WinTranPack wtp;
				uint8_t data[6], packlen;
				sprintf((char *) data, "%05d", rand());
				packlen = ZwEncodeCollect(&zp, wtp.buffer, 8, (uint8_t *)"DM123456", 5, data);
				// Sys.WriteData(wtp.buffer);
				Sys.SendTo(Sys.WTAddr, wtp, packlen);
				Sleep(100);
			}
			*/
			system("pause > nul");
			Sys.CleanUp();
		}
		else
		{
			PutError("数据采集服务启动失败");
		}
		WSACleanup();
	}
	else
	{
		PutError("网络环境启动失败，正尝试重置网络");
		system("netsh winsock reset");
	}
	return 0;
}

#include <crtdbg.h> // 检测内存泄漏

int main()
{
	_CrtDumpMemoryLeaks();
	// 启动采集服务
	CollectServer();
	// 检测内存是否泄漏，请放在程序最终执行位置而不是函数
	_CrtDumpMemoryLeaks();
	return 0;
}