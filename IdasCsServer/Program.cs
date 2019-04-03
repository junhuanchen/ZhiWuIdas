using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace IdasCsServer
{
    using Newtonsoft.Json;
    using System.Configuration;
    using System.Data;
    using System.Net;
    using Tdp;
    using ThinkDb;
    using ZwLib;
    using System.Threading;

    public class nlog
    {
        public static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
    }

    public class ZwDataModel : Tdp.TdpServer
    {
        ZwTransit TranModel = new ZwTransit(3, 7);
        ConfigCtrl Ctrl = new ConfigCtrl(ConfigurationManager.ConnectionStrings["mysql"].ConnectionString);

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

            string ConnectionString;

            public ConfigCtrl(string Connection)
            {
                ConnectionString = Connection;
            }

            public void SetData(byte ent_id, string dev_id, byte dev_ip, string src_id, string data, ushort ms, uint tm)
            {
                using (var MysqlDB = new MySqlDatabase(ConnectionString))
                {
                    var Result = MysqlDB.CreateOutParameter("Result", DbType.Int32);

                    var EntID = MysqlDB.CreateInParameter("EntID", DbType.Int16, ent_id);

                    var DevName = MysqlDB.CreateInParameter("DevID", DbType.AnsiString, dev_id);

                    var DevPort = MysqlDB.CreateInParameter("DevPort", DbType.Int16, dev_ip);

                    var DtSrcName = MysqlDB.CreateInParameter("DtSrcID", DbType.AnsiString, src_id);

                    var Dt = MysqlDB.CreateInParameter("Dt", DbType.AnsiString, data);

                    var Tm = MysqlDB.CreateInParameter("Tm", DbType.AnsiString, StartTime.AddSeconds(tm).AddMilliseconds(ms).ToString("yyyy-MM-dd HH:mm:ss.fff"));

                    var tmp = MysqlDB.ExecuteScalar(CommandType.StoredProcedure, "ZwSetData", new IDataParameter[] { Result, EntID, DevName, DevPort, DtSrcName, Dt, Tm });

                    Console.WriteLine("{1}-{2}-{3} MysqlDB SetData {4}-{5} Result: {0}", Result.Value, ent_id, dev_id, dev_ip, src_id, data);
                    if ((int)Result.Value == 0)
                    {

                    }
                }
            }
            public bool CheckDevive(byte ent_id, string dev_id, byte dev_ip)
            {
                using (var MysqlDB = new MySqlDatabase(ConnectionString))
                {
                    var Result = MysqlDB.CreateOutParameter("Result", DbType.Int32);

                    var EntID = MysqlDB.CreateInParameter("EntID", DbType.Int16, ent_id);

                    var DevName = MysqlDB.CreateInParameter("DevID", DbType.AnsiString, dev_id);

                    var DevPort = MysqlDB.CreateInParameter("DevPort", DbType.Int16, dev_ip);

                    var tmp = MysqlDB.ExecuteScalar(CommandType.StoredProcedure, "ZwCheckDevice", new IDataParameter[] { Result, EntID, DevName, DevPort });

                    Console.WriteLine("{1}-{2}-{3} MysqlDB CheckDevive Result: {0}", Result.Value, ent_id, dev_id, dev_ip);
                    if ((int)Result.Value == 0)
                    {
                        return true;
                    }
                    return false;
                }
            }
            public void UpdateDevive(byte ent_id, string dev_id, byte dev_ip, ref EndPoint end_point)
            {
                try
                {
                    using (var MysqlDB = new MySqlDatabase(ConnectionString))
                    {
                        var EntID = MysqlDB.CreateInParameter("EntID", DbType.Int32, ent_id);

                        var DevID = MysqlDB.CreateInParameter("DevID", DbType.AnsiString, dev_id);

                        var DevPort = MysqlDB.CreateInParameter("DevPort", DbType.Int32, dev_ip);

                        var IpAddr = MysqlDB.CreateInParameter("IpAddr", DbType.AnsiString, ((IPEndPoint)end_point).Address.ToString());

                        var IpPort = MysqlDB.CreateInParameter("IpPort", DbType.Int32, ((IPEndPoint)end_point).Port);

                        var LastTime = MysqlDB.CreateInParameter("LastTime", DbType.AnsiString, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"));

                        var tmp = MysqlDB.ExecuteScalar(CommandType.StoredProcedure, "ZwUpdateDevice", new IDataParameter[] { EntID, DevID, DevPort, IpAddr, IpPort, LastTime });

                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    nlog.logger.Error(e);
                }
            }

            Dictionary<string, string> GetJsonConfig(string Json)
            {
                return JsonConvert.DeserializeObject<Dictionary<string, string>>(Json);
            }

            string SetJsonConfig(Dictionary<string, string> Set)
            {
                return JsonConvert.SerializeObject(Set);
            }

            public bool SetState(byte ent_id, string dev_id, ushort state)
            {
                try
                {
                    using (var MysqlDB = new MySqlDatabase(ConnectionString))
                    {
                        var query = MysqlDB.CreateSqlCommand();
                        query.Table(string.Format("device_{0}", ent_id));
                        query.Where("id", dev_id);
                        query.SetField("state", state);
                        query.Update();
                        return true;
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    nlog.logger.Error(e);
                }
                return false;
            }

            public ushort GetState(byte ent_id, string dev_id)
            {
                using (var MysqlDB = new MySqlDatabase(ConnectionString))
                {
                    var query = MysqlDB.CreateSqlQuery();
                    query.Table(string.Format("device_{0}", ent_id));
                    query.Where("id", dev_id);
                    return query.GetField<ushort>("state");
                }
            }

            public bool SetMysqlConfig(byte ent_id, string dev_id, Dictionary<string, string> config)
            {
                try
                {
                    using (var MysqlDB = new MySqlDatabase(ConnectionString))
                    {
                        var query = MysqlDB.CreateSqlCommand();
                        query.Table(string.Format("device_{0}", ent_id));
                        query.Where("id", dev_id);
                        query.SetField("config", SetJsonConfig(config));
                        query.Update();
                        return true;
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
                return false;
            }

            public Dictionary<string, string> GetMysqlConfig(byte ent_id, string dev_id)
            {
                try
                {
                    using (var MysqlDB = new MySqlDatabase(ConnectionString))
                    {
                        var query = MysqlDB.CreateSqlQuery();
                        query.Table(string.Format("device_{0}", ent_id));
                        query.Where("id", dev_id);
                        return GetJsonConfig(query.GetField<string>("config"));
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    nlog.logger.Error(e);
                }
                return null;
            }

            public bool CloseConfig(byte ent_id, string dev_id)
            {
                var tmp = GetState(ent_id, dev_id);
                if ((tmp & 2u) == 2u)
                {
                    return SetState(ent_id, dev_id, (ushort)(tmp & ~(2U)));
                }
                return false;
            }
            public bool StartConfig(byte ent_id, string dev_id)
            {
                var tmp = GetState(ent_id, dev_id);
                if ((tmp & 1u) == 1u)
                {
                    var key = string.Format("{0}-{1}", ent_id, dev_id);
                    if (Cache.ContainsKey(key)) Cache.Remove(key);
                    var dict = GetMysqlConfig(ent_id, dev_id);
                    if (dict != null)
                    {
                        lock (dict)
                        {
                            if (dict.Count != 0)
                            {
                                Cache.Add(key, dict);
                            }
                        }
                        return SetState(ent_id, dev_id, (ushort)(tmp | 2U));
                    }
                    return true; // 给没有配置的设备通过
                }
                return false;
            }
            public bool ExistConfig(byte ent_id, string dev_id)
            {
                var res = GetState(ent_id, dev_id);
                if ((res & 2u) == 2u)
                {
                    var key = string.Format("{0}-{1}", ent_id, dev_id);
                    if (Cache.ContainsKey(key))
                    {
                        if (Cache[key].Count != 0)
                        {
                            return true;
                        }
                        Cache.Remove(key);
                        CloseConfig(ent_id, dev_id);
                    }
                    else
                    {
                        StartConfig(ent_id, dev_id);
                    }
                }
                return false;
            }
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

                            if (src_id != "sync")
                            {
                                Ctrl.SetData(ent_id, dev_id, dev_ip, src_id, data, ms, tm);
                            }
                        }
                    }
                    else if (type == ZwTransit.RecvPackType.Command)
                    {
                        string command = null;
                        if (true == TranModel.RecvCommand(result, ref command))
                        {
                            // Console.WriteLine("Command {0}", command);

                            var key = string.Format("{0}-{1}", ent_id, dev_id);

                            if (Ctrl.Cache.ContainsKey(key))
                            {
                                var pair = command.Split(':');
                                var dict = Ctrl.Cache[key];
                                lock (dict)
                                {
                                    if (dict.ContainsKey(pair[0]) && dict[pair[0]] == pair[1])
                                    {
                                        Console.WriteLine("{1}-{2}-{3} Recv Config: {0}", command, ent_id, dev_id, dev_ip);
                                        dict.Remove(pair[0]);
                                    }
                                }
                            }

                            //if (Ctrl.Cache.ContainsKey(key))
                            //{
                            //    var config = Ctrl.Cache[key].First();
                            //    var parts = command.Split(':');
                            //    if (config.Key == parts[0] && config.Value == parts[1])
                            //    {
                            //        Console.WriteLine("{1}-{2}-{3} Recv Config: {0}", command, ent_id, dev_id, dev_ip);
                            //        var dict = Ctrl.Cache[key];
                            //        lock(dict)
                            //        {
                            //            dict.Remove(config.Key);
                            //        }
                            //    }
                            //}
                        }
                    }

                    // 返回现存在配置的变更
                    if (Ctrl.ExistConfig(ent_id, dev_id))
                    {
                        var key = string.Format("{0}-{1}", ent_id, dev_id);
                        
                        var timeout = DateTime.Now.AddMinutes(3);
                        var dict = Ctrl.Cache[key];
                        lock (dict)
                        {
                            var config = dict.First();
                            var command = string.Format("{0}:{1}", config.Key, config.Value);
                            AsyncBeginSend(new TdpPack(TranModel.RequestCommand(dev_ip, command), pack.RemoteEndPoint));
                            Console.WriteLine("{1}-{2}-{3} Send Config: {0}", command, ent_id, dev_id, dev_ip);

                            //foreach (var config in dict)
                            //{
                            //    var command = string.Format("{0}:{1}", config.Key, config.Value);
                            //    AsyncBeginSend(new TdpPack(TranModel.RequestCommand(dev_ip, command), pack.RemoteEndPoint));
                            //    Console.WriteLine("{1}-{2}-{3} Send Config: {0}", command, ent_id, dev_id, dev_ip);
                            //}
                        }
                    }

                    Ctrl.UpdateDevive(ent_id, dev_id, dev_ip, ref pack.RemoteEndPoint);
                }
                else
                {
                    Console.WriteLine("CheckDevive {0}-{1}-{2}", ent_id, dev_id, dev_ip);

                    if (Ctrl.CheckDevive(ent_id, dev_id, dev_ip) && Ctrl.StartConfig(ent_id, dev_id))
                    {
                        AsyncBeginSend(new TdpPack(TranModel.RespondTimeSysn(), pack.RemoteEndPoint));
                    }
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
