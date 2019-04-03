
#include "FrameWork\DataTransfer\WinTdpToSql.hpp"

#pragma comment(lib,"user32.lib")

#define debug(_operator) printf("operator: %d\n", _operator)

#include  <io.h>

int main()
{
	WSADATA wsaData;
	// �������绷��
	if (0 == WSAStartup(MAKEWORD(0x02, 0x02), &wsaData))
    {
        CHAR * ConfigPath = ".\\IdasCppServer.ini";
        // ��ȡ�����ļ�����������
        USHORT LocalPort, ThreadSum;
        CHAR Temp[6] = { 0 };

        // ��ȡ������Ϣ
        GetPrivateProfileString("�߳�����", "�߳�����", "4", Temp, sizeof(Temp), ConfigPath);
        WritePrivateProfileString("�߳�����", "�߳�����", Temp, ConfigPath);
        sscanf_s(Temp, "%hd", &ThreadSum);

        GetPrivateProfileString("��������", "���ض˿�", "9954", Temp, sizeof(Temp), ConfigPath);
        WritePrivateProfileString("��������", "���ض˿�", Temp, ConfigPath);
        sscanf_s(Temp, "%hd", &LocalPort);

        PutInfo("��ȡ�������£�\n");
        printf("�������ã����ض˿�:%hd �߳�����:%hd\n", LocalPort, ThreadSum);

        // �������ݿ�����
        CHAR * SqlPath = ".\\IdasCppServer.udl";
        if ((_access(SqlPath, 0)) != -1)
        {
            WinTranTdpToSql Sys;
            if (Sys.StartUp("File Name=IdasCppServer.udl", LocalPort, ThreadSum))
            {
                PutInfo("���ݲɼ��������������ɹ����� ESC �˳�����\n");
                while (true)
                {
                    // ����ESC�����˳�����
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
            printf_s("���ݿ����������ļ���IdasCppServer.udl�������ڣ����ֶ�������\n");
        }
	}
	else
	{
		PutInfo("WSAStartup���񻷾��쳣����������������......\n");
		system("netsh winsock reset");
    }
    system("pause");
	return 0;
}
