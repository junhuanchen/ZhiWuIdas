using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Configuration;

namespace IdasLocal
{
    using DevExpress.UserSkins;
    using DevExpress.Skins;
    using DevExpress.LookAndFeel;
    using DevExpress.XtraEditors.Controls;
    using ZwLib;
    using Tdp;
     
    public class nlog
    {
        public static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
    }
    class LocalizationCHS : DevExpress.XtraEditors.Controls.Localizer
    {
        public override string GetLocalizedString(DevExpress.XtraEditors.Controls.StringId id)
        {
            switch (id)
            {
                case StringId.XtraMessageBoxCancelButtonText:
                    return "取消";
                case StringId.XtraMessageBoxOkButtonText:
                    return "确定";
                case StringId.XtraMessageBoxYesButtonText:
                    return "是";
                case StringId.XtraMessageBoxNoButtonText:
                    return "否";
                case StringId.XtraMessageBoxIgnoreButtonText:
                    return "忽略";
                case StringId.XtraMessageBoxAbortButtonText:
                    return "中止";
                case StringId.XtraMessageBoxRetryButtonText:
                    return "重试";
                default:
                    return base.GetLocalizedString(id);
            }
        }
    }

    //class Idas : KvTransit
    //{
    //    public static int ThreadSum = 8;
    //    public static ushort Port = 9954;
    //    public XtraMain Form = new XtraMain();
    
    //    public Idas()
    //    {
    //        if (true == KvTransit.StopConsoleOut())
    //        {
    //            if (1 == StartUp(ThreadSum, Port))
    //            {
    //                return;
    //            }
    //        }
    //        throw new Exception("KvTransit Dll 环境启动失败，请检查同目录下的ClrTransit.log文件查看错误报告。");
    //    }

    //    ~Idas()
    //    {
    //        CleanUp();
    //    }

    //    protected override bool RecvCnctData(byte[] DevId, string DevIp, string DevName)
    //    {
    //        try
    //        {
    //            return true;
    //        }
    //        catch (Exception e)
    //        {
    //            nlog.logger.Error(e);
    //        }
    //        return false;
    //    }

    //    protected override bool RecvCollectData(byte[] DevId, ushort DevIp, string RegId, string Data, DateTime Collect)
    //    {
    //        try
    //        {
    //            if (true == Form.IsHandleCreated)
    //            {
    //                Form.BeginInvoke(new EventHandler(delegate
    //                {
    //                    Form.IdasUpdateDataSet(DevId, RegId, UInt64.Parse(Data), Collect);
    //                    // GC.Collect();
    //                }));
    //                return true;
    //            }
    //        }
    //        catch (Exception e)
    //        {
    //            nlog.logger.Error(e);
    //        }
    //        return false;
    //    }
         
    //    protected override bool RecvCtrlData(byte[] DevId, ushort DevIp, string Ctrl, DateTime GetTime)
    //    {
    //        return true;
    //    }
    //}
    
    public class ZwIdas : TdpServer
    {
        public static ushort Port;
        public XtraMain Form = new XtraMain();
        ZwTransit TranModel = new ZwTransit(3, 7);

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
                if (true == Form.IsHandleCreated)
                {
                    uint tm = 0;
                    ushort ms = 0;
                    byte ent_id = 0, dev_ip = 0;
                    string dev_id = null;
                    byte[] result = null;

                    if (TranModel.UnPackCore(pack.Data, (byte)pack.Length, ref result, ref ent_id, ref tm, ref ms, ref dev_id, ref dev_ip))
                    {
                        var type = (ZwTransit.RecvPackType)pack.Data[0];
                        if (type == ZwTransit.RecvPackType.Collect)
                        {
                            string src_id = null, data = null;
                            if (TranModel.RecvCollect(result, ref src_id, ref data))
                            {
                                // AsyncBeginSend(new TdpPack(TranModel.RequestCommand(dev_ip, "abc 1231"), pack.RemoteEndPoint));

                                Form.BeginInvoke(new Action(() =>
                                {
                                    Form.IdasUpdateDataSet(dev_id, src_id, Convert.ToUInt64(data, 16), ConvertUintDateTime(tm).AddMilliseconds(ms));
                                }));

                                AsyncBeginSend(new TdpPack(TranModel.RespondCollect(dev_ip), pack.RemoteEndPoint));
                                    
                            }
                        }
                        else if (type == ZwTransit.RecvPackType.Command)
                        {
                            string command = null;
                            if (true == TranModel.RecvCommand(result, ref command))
                            {
                                ;
                            }
                        }
                    }
                    else
                    {
                        AsyncBeginSend(new TdpPack(TranModel.RespondTimeSysn(), pack.RemoteEndPoint));

                        // Config 链接

                    }
                }
            }
            catch (Exception e)
            {
                nlog.logger.Error(e);
            }
        }
        protected override void PacketSent(TdpPack buffer, int bytesSent)
        {
            //System.Diagnostics.Trace.WriteLine("{0}:{1}", BitConverter.ToString(buffer.Data), bytesSent);
        }
    }

    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            try
            {
                Application.ThreadException += Application_ThreadException;
                Application.SetUnhandledExceptionMode(UnhandledExceptionMode.Automatic);
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                BonusSkins.Register();
                SkinManager.EnableFormSkins();
                UserLookAndFeel.Default.SetSkinStyle("Office 2013");// DevExpress Style

                DevExpress.XtraEditors.Controls.Localizer.Active = new LocalizationCHS();

                DevExpress.Data.CurrencyDataController.DisableThreadingProblemsDetection = true;

                Application.Run(new ZwIdas(ushort.Parse(ConfigurationManager.AppSettings["LocalPort"].ToString())).Form);
            }
            catch(Exception e)
            {
                DevExpress.XtraEditors.XtraMessageBox.Show(e.ToString());
                nlog.logger.Error(e);
            }
        }

        static void Application_ThreadException(object sender, System.Threading.ThreadExceptionEventArgs e)
        {
            nlog.logger.Warn(e);
        }
    }
}
