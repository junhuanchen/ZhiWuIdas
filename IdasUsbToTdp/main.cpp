#include "FrameWork\DataTransfer\WinUsbToTdp.hpp"

#include <time.h>

void GetTime(uint32_t *sec, uint16_t *ms)
{
	*sec = time(NULL), *ms = GetTickCount() % 1000;
}

int CollectServer()
{
	// ����WINDOWS���绷��
	WSADATA wsaData;
	if (0 == WSAStartup(MAKEWORD(0x02, 0x02), &wsaData))
    {
        CHAR * ConfigPath = ".\\IdasUsbToTdp.ini";
        // ��ȡ�����ļ�����������
        USHORT RemotePort, LocalPort, UsbPid, UsbVid, UsbPvn;
        CHAR RemoteIP[16] = { 0 };

        // ��ȡ������Ϣ
        GetPrivateProfileString("USB����", "PID", "0x9990", RemoteIP, sizeof(RemoteIP), ConfigPath);
        WritePrivateProfileString("USB����", "PID", RemoteIP, ConfigPath);
        sscanf_s(RemoteIP, "%hx", &UsbPid);

        GetPrivateProfileString("USB����", "VID", "0x0666", RemoteIP, sizeof(RemoteIP), ConfigPath);
        WritePrivateProfileString("USB����", "VID", RemoteIP, ConfigPath);
        sscanf_s(RemoteIP, "%hx", &UsbVid);

        GetPrivateProfileString("USB����", "PVN", "0x0200", RemoteIP, sizeof(RemoteIP), ConfigPath);
        WritePrivateProfileString("USB����", "PVN", RemoteIP, ConfigPath);
        sscanf_s(RemoteIP, "%hx", &UsbPvn);

        GetPrivateProfileString("��������", "���ض˿�", "6666", RemoteIP, sizeof(RemoteIP), ConfigPath);
        WritePrivateProfileString("��������", "���ض˿�", RemoteIP, ConfigPath);
        sscanf_s(RemoteIP, "%hd", &LocalPort);

        GetPrivateProfileString("��������", "Զ�˶˿�", "9954", RemoteIP, sizeof(RemoteIP), ConfigPath);
        WritePrivateProfileString("��������", "Զ�˶˿�", RemoteIP, ConfigPath);
        sscanf_s(RemoteIP, "%hd", &RemotePort);

        GetPrivateProfileString("��������", "Զ�˵�ַ", "127.000.000.001", RemoteIP, sizeof(RemoteIP), ConfigPath);
        WritePrivateProfileString("��������", "Զ�˵�ַ", RemoteIP, ConfigPath);

        PutInfo("��ȡ�������£�\n");
        printf("�������ã�Զ�˵�ַ:%s Զ�˶˿�:%hd ���ض˿�:%hd\n", RemoteIP, RemotePort, LocalPort);
        printf("USB���ã�Pid:0x%hX Vid:0x%hX Pvn:0x%hX\n", UsbPid, UsbVid, UsbPvn);

		WinTranUsbToTdp Sys;
        if (Sys.StartUp(RemoteIP, RemotePort, LocalPort, UsbPid, UsbVid, UsbPvn))
		{
			PutInfo("���ݲɼ����������ɹ����������ֹͣ����\n");
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
			PutError("���ݲɼ���������ʧ��");
		}
		WSACleanup();
	}
	else
	{
		PutError("���绷������ʧ�ܣ���������������");
		system("netsh winsock reset");
	}
	return 0;
}

#include <crtdbg.h> // ����ڴ�й©

int main()
{
	_CrtDumpMemoryLeaks();
	// �����ɼ�����
	CollectServer();
	// ����ڴ��Ƿ�й©������ڳ�������ִ��λ�ö����Ǻ���
	_CrtDumpMemoryLeaks();
	return 0;
}