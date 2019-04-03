// ZwLib.h

#pragma once

#include <Windows.h>

#include <stdio.h>

//
//#include <comdef.h>
//
//#pragma region 非托管类相关操作
//namespace ZwLib
//{
//	// _variant_t到Object对象
//	inline System::Object^ VarToObject(_variant_t var)
//	{
//		using namespace System::Runtime::InteropServices;
//		System::IntPtr^ pvar = gcnew System::IntPtr(&var);
//		System::Object^ obj = Marshal::GetObjectForNativeVariant(*pvar);
//		return obj;
//	}
//	// Object对象到_variant_t
//	inline _variant_t* ObjectToVar(System::Object^ obj)
//	{
//		using namespace System::Runtime::InteropServices;
//		_variant_t* vt = new _variant_t();
//		System::IntPtr^ pvar = (gcnew System::IntPtr((void*) vt));
//		Marshal::GetNativeVariantForObject(obj, *pvar);
//		return vt;
//	}
//}
//#pragma endregion

//#include "FrameWork/DataIntfc/Tdp/Tdp.hpp"
//namespace ZwLib
//{
//	interface class ZwIntfc
//	{
//		virtual void recv_zw_pack(System::String ^ pack);
//	};
//
//	struct Pack
//	{
//		CHAR Buf[64];
//		Pack()
//		{
//			memset(Buf, 0, sizeof(Buf));
//		}
//		Pack(const char * str)
//		{
//			strcpy(Buf, str);
//		}
//	};
//
//	struct Tran : public Tdp<Pack>
//	{
//		_variant_t ZwTdp;
//		Tran(ZwIntfc ^ This)
//		{
//			ZwTdp = ObjectToVar(This);
//		}
//		void TdpRecvProcess(TdpFrame & packet)
//		{
//			((ZwIntfc ^) VarToObject(ZwTdp))->recv_zw_pack(gcnew System::String(packet.var.Buf, 0, packet.len));
//		}
//	};
//
//	ref class ZwTdp : public ZwIntfc
//	{
//		Tran * tdp;
//	public:
//		virtual void recv_zw_pack(System::String ^ pack)
//		{
//			;
//		}
//		ZwTdp()
//		{
//			// 启动WINDOWS网络环境
//			WSADATA wsaData;
//			if (0 == WSAStartup(MAKEWORD(0x02, 0x02), &wsaData))
//			{
//				tdp = new Tran(this);
//				if (NULL == tdp)
//				{
//					throw gcnew System::Exception(System::String::Format("ZwTdp 启动失败，错误码：%d\n", GetLastError()));
//				}
//			}
//		}
//		~ZwTdp()
//		{
//			Stop();
//			WSACleanup();
//		}
//		DWORD Start(unsigned short port, int tread_sum)
//		{
//			if (NULL != tdp)
//			{
//				if (tdp->StartServer(port, tread_sum))
//				{
//					return NO_ERROR;
//				}
//			}
//			return GetLastError();
//		}
//		DWORD Stop()
//		{
//			if (NULL != tdp)
//			{
//				tdp->StopServer();
//				return NO_ERROR;
//			}
//			return GetLastError();
//		}
//	};
//}

#include "FrameWork/FrameWork.hpp"

extern "C"
{
#include "FrameWork/Core/ZwTransit.h"
}

#include <time.h>

static inline void GetTime(uint32_t *sec, uint16_t *ms)
{
    *sec = time(NULL), *ms = GetTickCount() % 1000;
}

namespace ZwLib {

	using namespace System::Text;

	static FILE * ConsoleOut = stdout;
#undef LogError
#define LogError(format, ...) fprintf(ConsoleOut, format, ##__VA_ARGS__), fflush(ConsoleOut)

	// 申请非托管资源，并检查，失败产生失败断言
	template < typename T > void CheckNew(T * ptr)
	{
		if (NULL == ptr) abort();
	}

	// 检查非托管资源是否可释放
	template < typename T > void CheckDel(T * ptr)
	{
		if (NULL != ptr) delete (ptr);
	}

	array<System::Byte> ^ MemCpy(array<System::Byte> ^ Dst, uint8_t * Src)
	{
		for (size_t i = 0; i != Dst->Length; i++)
		{
			Dst[i] = Src[i];
		}
		return Dst;
	}

	uint8_t * MemCpy(uint8_t * Dst, array<System::Byte> ^ Src)
	{
		for (size_t i = 0; i != Src->Length; i++)
		{
			Dst[i] = Src[i];
		}
		return Dst;
	}

	public ref class ZwTransit
	{
        const uint8_t RsaLocalKey, RsaRemoteKey;

        ZwEncode * En;
		ZwDecode * De;

	public:
		
		enum class RecvPackType
		{
			Collect = ZwTranTypeCollect,
			Command = ZwTranTypeCommand
		};

        ZwTransit(uint8_t LocalKey, uint8_t RemoteKey) : RsaLocalKey(LocalKey), RsaRemoteKey(RemoteKey), En(NULL)
		{
            ZwTranInit();
            En = new ZwEncode();
			De = new ZwDecode();
            if (NULL == En)
            {
                throw gcnew System::Exception("not enough memory");
            }
            else
            {
                ZwEncodeInit(En, RsaRemoteKey, 0, (uint8_t *) "\xFF\x00\xFF\x00\xFF\x00\xFF\x00\xFF\x00\xFF", 0, GetTime);
				ZwDecodeInit(De, RsaLocalKey);
            }
        }

        ~ZwTransit()
        {
            if (En)
            {
                delete En;
            }
			if (De)
			{
				delete De;
			}
        }

		bool UnPackCore(array<System::Byte> ^ RecvBuf, uint8_t RecvLen, array<System::Byte> ^% Pack, uint8_t % EntID, uint32_t % Tm, uint16_t % Ms, System::String ^% DevID, uint8_t % DevIP)
		{
			uint8_t buf[ZwTranMax] = { 0 };

			ZwDecode tmpDe = *this->De, *De = &tmpDe;
			uint8_t* pack = ZwDecodeCore(De, MemCpy(buf, RecvBuf), RecvLen);
			if (pack)
			{
				DevIP = *((uint8_t *)De->Zip.DevIP);
				EntID = *((uint8_t *)De->Zip.EntID);
				DevID = gcnew System::String((char *)De->Zip.DevID, 0, sizeof(De->Zip.DevID));
				Tm = *((uint32_t *)De->Zip.DevTm);
				time_t tt = time(NULL); // time_sec;
				LogPut("Time:%I64u Tm:%u Ms:%hu\n", tt, Tm, *((uint16_t *)De->Zip.DevMs));
				if ( /* true || */ tt > Tm && tt - Tm <= 16)
				{
					Ms = *((uint16_t *)De->Zip.DevMs);
					Pack = MemCpy(gcnew array<System::Byte>(strlen((const char *)pack)), pack);
					return true;
				}
			}
			return false;
		}

		bool RecvCollect(array<System::Byte> ^ Pack, System::String ^% SrcID, System::String ^% Data)
		{
			uint8_t buf[ZwContentMax] = { 0 }, src_len, src_id[ZwSourceMax], data_len, data[ZwDataMax];
			if (ZwDecodeCollect(MemCpy(buf, Pack), &src_len, src_id, &data_len, data))
			{
				SrcID = gcnew System::String((char *)src_id, 0, src_len);
				Data = gcnew System::String((char *)data, 0, data_len);
				return true;
			}
			return false;
		}

		bool RecvCommand(array<System::Byte> ^ Pack, System::String ^% Command)
		{
			uint8_t buf[ZwContentMax] = { 0 }, cmd_len, command[ZwCmdMax];
			if (ZwDecodeCommand(MemCpy(buf, Pack), &cmd_len, command))
			{
				Command = gcnew System::String((char *)command, 0, cmd_len);
				return true;
			}
			return false;
		}

		array<System::Byte> ^ RespondTimeSysn()
		{
			uint8_t buf[ZwTranMax] = { 0 };
			// 反馈时间同步指令
			uint8_t len = ZwEncodeCommand(En, (uint8_t *)buf, sizeof("TimeSysn") - 1, (uint8_t *)"TimeSysn");
			if (0 != len)
			{
				uint32_t time = *(uint32_t *)En->Zip.DevTm;
				LogPut("RespondTimeSysn TimeSysn:%u\n", time);
				return MemCpy(gcnew array<System::Byte>(len + 1), buf);
			}
			return nullptr;
		}

		array<System::Byte> ^ RespondCollect(uint8_t DevIP)
		{
			uint8_t buf[ZwTranMax] = { 0 };
			uint8_t len = ZwEncodeCollect(En, (uint8_t *)buf, 1, (uint8_t *)"0", 1, (uint8_t *)"F");
			if (0 != len && ZwEncodeSetDevIP(buf, len, DevIP))
			{
				return MemCpy(gcnew array<System::Byte>(len + 1), buf);
			}
			return nullptr;
		}

		array<System::Byte> ^ RequestCommand(uint8_t DevIP, System::String ^ Command)
		{
			if (ZwCmdMax >= Command->Length)
			{
				uint8_t buf[ZwTranMax] = { 0 }, tmp[ZwCmdMax] = { 0 };
				uint8_t len = ZwEncodeCommand(En, (uint8_t *)buf, Command->Length, MemCpy(tmp, Encoding::GetEncoding("ASCII")->GetBytes(Command)));
				if (0 != len && ZwEncodeSetDevIP(buf, len, DevIP))
				{
					return MemCpy(gcnew array<System::Byte>(len + 1), buf);
				}
			}
			return nullptr;
		}

	};
}
