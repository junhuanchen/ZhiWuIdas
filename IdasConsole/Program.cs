using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IdasConsole
{
    using System.Configuration;
    using System.Data;
    using System.Net;
    using Tdp;
    using ZwLib;
    using System.Threading;

    public class nlog
    {
        public static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
    }

    public class ZwDataModel : Tdp.TdpServer
    {
        ZwTransit TranModel = new ZwTransit(3, 7);

        public ZwDataModel() : base("ZwDataModel", ushort.Parse(ConfigurationManager.AppSettings["ServerPort"].ToString()))
        {
            Start();
            Console.WriteLine("IdasCsServer Start");
        }

        static DateTime StartTime = TimeZone.CurrentTimeZone.ToLocalTime(new System.DateTime(1970, 1, 1));

        public static System.DateTime ConvertUintDateTime(UInt32 time)
        {
            return StartTime.AddSeconds(time);
        }

        public class ConfigCtrl
        {
            public Dictionary<string, Dictionary<string, string>> Cache = new Dictionary<string, Dictionary<string, string>>();
            
        }

        protected override void PacketReceived(TdpPack pack)
        {
            try
            {
                Console.WriteLine("RemoteEndPoint: {0}", ((IPEndPoint)pack.RemoteEndPoint).ToString());

                uint tm = 0;
                ushort ms = 0;
                byte ent_id = 0, dev_ip = 0;
                string dev_id = null;
                byte[] result = null;

                if (TranModel.UnPackCore(pack.Data, (byte)pack.Length, ref result, ref ent_id, ref tm, ref ms, ref dev_id, ref dev_ip))
                {
                    Console.WriteLine("{0}-{1}-{2}-{3}-{4} Recv Data", ent_id, tm, ms, dev_id, dev_ip);

                    var type = (ZwTransit.RecvPackType)pack.Data[0];
                    if (type == ZwTransit.RecvPackType.Collect)
                    {
                        string src_id = null, data = null;
                        if (TranModel.RecvCollect(result, ref src_id, ref data))
                        {
                            AsyncBeginSend(new TdpPack(TranModel.RespondCollect(dev_ip), pack.RemoteEndPoint));

                            Console.WriteLine("Collect {0}-{1}", src_id, data);
                        }
                    }
                    else if (type == ZwTransit.RecvPackType.Command)
                    {
                        string command = null;
                        if (true == TranModel.RecvCommand(result, ref command))
                        {
                            // Console.WriteLine("Command {0}", command);

                            var key = string.Format("{0}-{1}", ent_id, dev_id);
                            
                        }
                    }
                    
                }
                else
                {
                    Console.WriteLine("CheckDevive {0}-{1}-{2}", ent_id, dev_id, dev_ip);
                    AsyncBeginSend(new TdpPack(TranModel.RespondTimeSysn(), pack.RemoteEndPoint));
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                nlog.logger.Error(e);
                GC.Collect();
            }
        }
        protected override void PacketSent(TdpPack buffer, int bytesSent)
        {
            //System.Diagnostics.Trace.WriteLine("{0}:{1}", BitConverter.ToString(buffer.Data), bytesSent);
        }
        public void CtrlUnitTest()
        {
            // 因为只有在链接过程中才有收发

            // 所以开始传输配置的发送线程会阻塞，直到配置发送期间完成或异常退出。

            // 被动触发条件在同步时间的时候，因为往往是在上电前发生。

            // 主动触发的前提是数据库有变动，因此同步时间时改变数据库。

            // 则 可以 在传输过程中数据解析成功后，核对是否存在配置要下载

            // 则 在解析成功后，检查 是否存在标记，如果存在则开始增加一个配置配置。

            // 接口 分为 检查 当前存在的 存储缓存，如果不存在 从数据库里读，

            // 如果下载配置存在，则继续发送，直到返回的值将其移出

            // 移除到最后一个的时候，将数据库的标记移除。

            // 传输完成。

            // return;

            //var tmp = new dict<ConfigCtrl.Config>();
            //tmp.Add(new ConfigCtrl.Config() { name = "test", data = "123F" });
            //var res = Ctrl.SetMysqlConfig(1, "0P-01-L1-DGLB", tmp);

        }

    }

    class Program
    {
        static void Main(string[] args)
        {
            var idas = new ZwDataModel();
            idas.CtrlUnitTest();
            while (ConsoleKey.Escape != Console.ReadKey().Key)
            {
                ;
            }
        }
    }
}
