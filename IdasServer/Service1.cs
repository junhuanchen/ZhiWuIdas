using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.ServiceProcess;
using System.Text;
using Tdp;
using ZwLib;

namespace IdasServer
{
    public class ZwIdas : TdpServer
    {
        public ushort Port;
        ZwTransit TranModel = new ZwTransit(3, 7);

        log4net.ILog log = log4net.LogManager.GetLogger("ZhiwuIdas.Logging");

        public ZwIdas(ushort port)
            : base("ZwIdas", port)
        {
            Port = port;
            Start();
        }

        static DateTime StartTime = TimeZone.CurrentTimeZone.ToLocalTime(new System.DateTime(1970, 1, 1));

        public static System.DateTime ConvertUintDateTime(UInt32 time)
        {
            return StartTime.AddSeconds(time);
        }

        protected override void PacketReceived(TdpPack pack)
        {
            try
            {
                switch ((ZwTransit.RecvPackType)pack.Data[0])
                {
                    case ZwTransit.RecvPackType.Collect:
                        UInt16 ms = 0;
                        byte ent_id = 0, dev_ip = 0;
                        UInt32 tm = 0;
                        byte[] dev_id = null;
                        string src_id = null, data = null;
                        if (true == TranModel.RecvCollect(pack.Data, (byte)pack.Length, ref ent_id, ref tm, ref ms, ref dev_id, ref dev_ip, ref src_id, ref data))
                        {
                            //Form.BeginInvoke(new Action(() =>
                            //{
                            //    Form.IdasUpdateDataSet(dev_id, src_id, Convert.ToUInt64(data, 16), ConvertUintDateTime(tm).AddMilliseconds(ms));
                            //}));

                            log.Info(string.Format("{0}-{1}-{2}-{3}", BitConverter.ToString(dev_id), src_id, Convert.ToUInt64(data, 16), ConvertUintDateTime(tm).AddMilliseconds(ms)));
                            AsyncBeginSend(pack);
                        }
                        else
                        {
                            AsyncBeginSend(new TdpPack(TranModel.RespondTimeSysn(), pack.RemoteEndPoint));
                        }
                        break;
                }
            }
            catch (Exception e)
            {
                log.Info("PacketReceived", e);
            }
        }
        protected override void PacketSent(TdpPack buffer, int bytesSent)
        {
            //System.Diagnostics.Trace.WriteLine("{0}:{1}", BitConverter.ToString(buffer.Data), bytesSent);
        }
    }

    public partial class Service1 : ServiceBase
    {
        public Service1()
        {
            InitializeComponent();
            log4net.Config.XmlConfigurator.Configure();
        }

        log4net.ILog log = log4net.LogManager.GetLogger("ZhiwuIdas.Logging");

        ZwIdas Server = new ZwIdas(ushort.Parse(ConfigurationManager.AppSettings["LocalPort"].ToString()));
        
        protected override void OnStart(string[] args)
        {
            Server.Start();
        }

        protected override void OnStop()
        {
            Server.Stop();
        }
    }
}
