using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Windows.Forms;
using DevExpress.XtraBars.Docking2010;
using DevExpress.XtraCharts;
using DevExpress.XtraEditors;
using System.Threading;

namespace IdasLocal
{
    public partial class 
        XtraMain : DevExpress.XtraEditors.XtraForm
    {
        public XtraMain()
        {
            InitializeComponent();
            try
            {
                // 建立静态资源
                if (!System.IO.Directory.Exists("IdasDevice"))
                {
                    System.IO.Directory.CreateDirectory("IdasDevice");
                }
                if (!System.IO.Directory.Exists("IdasError"))
                {
                    System.IO.Directory.CreateDirectory("IdasError");
                }
                if (!System.IO.Directory.Exists("IdasData"))
                {
                    System.IO.Directory.CreateDirectory("IdasData");
                }
                XtraMainInit();
            }
            catch (Exception ex)
            {
                XtraMessageBox.Show(ex.ToString());
                Close();
            }
        }

        private void XtraMainInit()
        {
            try
            {
                IdasDeviceDbLoad(idasDeviceSet);

                if (0 != idasDeviceSet.IdasDevice.Count)
                {
                    var DeviceName = idasDeviceSet.IdasDevice.First().Name;
                    设备信息.Text = "设备名称：" + DeviceName;
                    LoadIdasDataSet(IdasDeviceDb[DeviceName]);
                }
                else
                {
                    // IdasDeviceDbLoad和First()都失败，即意味着没有元素，将隐藏设备信息面板。
                    数据视图页.HideToCustomization();
                }
            }
            catch (Exception ex)
            {
                nlog.logger.Error(ex);
            }
        }

        private void XtraMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (DialogResult.Yes != DevExpress.XtraEditors.XtraMessageBox.Show("确定要退出程序吗？", "警告", MessageBoxButtons.YesNo))
            {
                e.Cancel = true;
            }
        }

        private void XtraMain_FormClosed(object sender, FormClosedEventArgs e)
        {
            IdasDeviceDbSave(idasDeviceSet);
        }

        #region 设备信息

        Dictionary<string, IdasDataSet> IdasDeviceDb = new Dictionary<string, IdasDataSet>();

        void IdasDeviceDbLoad(IdasDeviceSet Set)
        {
            try
            {
                idasDeviceSet.ReadXml(@"IdasDevice.xml");
                foreach (var set in Set.IdasDevice.AsEnumerable())
                {
                    IdasDeviceDb.Add(set.Name, new IdasDataSet());
                    IdasDeviceDb[set.Name].ReadXml(string.Format(@"IdasDevice\\{0}.xml", set.Name));
                }
            }
            catch (System.IO.FileNotFoundException e)
            {
                // 没有配置文件就不继续加载。
                return;
            }
        }

        void IdasDeviceDbSave(IdasDeviceSet Set)
        {
            Set.WriteXml(@"IdasDevice.xml");
            foreach (var set in IdasDeviceDb)
            {
                set.Value.WriteXml(string.Format(@"IdasDevice\\{0}.xml", set.Key));
            }
        }

        void LoadIdasDataSet(IdasDataSet Set)
        {
            // 导入选取设备的数据源
            DataBar3DChart.DataSource = DataLineChart.DataSource = this.idasSourceBindingSource.DataSource = Set;
            // 更新数据，确保 之后 Set 与 DataSource 数据一致
            this.idasSourceBindingSource.ResetBindings(false);
        }

        void PutErrorReport(string Device, IdasDataSet.IdasSourceRow Source)
        {
            try
            {
                const string ReportFormat = "<pre>\r\n\t数据源名：{0}\r\n\t异常数据：{1}\r\n\t采集时间：{2}\r\n\t数据源备注：{3}\r\n</pre>";
                var ReportTime = Source.RecentTime.ToString("yyyy年MM月dd日HH时mm分ss秒fff毫秒");
                var Report = string.Format(ReportFormat, Source.Name, Source.RecentValue, ReportTime, Source.Note);

                if ("全部" == Source.ErrorReport || "本地" == Source.ErrorReport)
                {
                    var sw = new StreamWriter(string.Format(@"IdasError\\{0}-{1}-{2}.log", Device, Source.Name, ReportTime));
                    sw.WriteLine(Report);
                    sw.Flush();
                    sw.Close();
                }

                if ("全部" == Source.ErrorReport || "邮件" == Source.ErrorReport)
                {
                    nlog.logger.Info(Report);
                }
            }
            catch(Exception e)
            {
                nlog.logger.Error(e);
            }
        }

        void CheckErrorOperator(string Device, IdasDataSet.IdasSourceRow Source)
        {
            switch (Source.ErrorOperator)
            {
                case '>':
                    if (Source.RecentValue > Source.ErrorValue)
                    {
                        goto ReportError;
                    } 
                    break;
                case '<':
                    if (Source.RecentValue < Source.ErrorValue)
                    {
                        goto ReportError;
                    } 
                    break;
                case '=':
                    if (Source.RecentValue == Source.ErrorValue)
                    {
                        goto ReportError;
                    } 
                    break;
                case '#':
                default:
                    break;
            }
            return;
        ReportError:
            PutErrorReport(Device, Source);
        }

        // 未来移除
        ulong VW001100, VW001102, VW001104;
        void HaoShunReport(string Device, IdasDataSet.IdasSourceRow Source)
        {
            StateAllError.StateIndex = (0 == (VW001100 | VW001102 | VW001104)) ? 3 : 1;
            if (3 == StateAllError.StateIndex)
            {
                System.Media.SystemSounds.Hand.Play();
            }
            //#region 异常报告
            //if (Source.RecentValue != 0)
            //{
            //    StateAllError.StateIndex = 1;
            //    var FileName = string.Format(@"IdasError\\{0}-{1}-{2}.log", Device, Source.Name, Source.RecentTime.ToString("yyyy年MM月dd日HH时mm分ss秒"));
            //    var sw = new StreamWriter(FileName);
            //    const string WLFormat = "{0,-10}\t|\t{1,-10}\n";
            //    sw.WriteLine(WLFormat, "数据源名", "异常数据");
            //    sw.WriteLine(WLFormat, Source.Name, Source.RecentValue);
            //    var No = Convert.ToUInt16(Source.Name.Substring(2, Source.Name.Length - 2) + "0", 8);
            //    foreach (var i in Convert.ToString((ushort)Source.RecentValue, 2).PadLeft(sizeof(ushort) * 8, '0'))
            //    {
            //        var tmp = Convert.ToString(No, 8).PadLeft(2, '0');
            //        sw.WriteLine("V" + WLFormat, tmp.Insert(tmp.Length - 1, "."), i);
            //        No++;
            //    }
            //    sw.WriteLine("备注：{0}", Source.Note);
            //    sw.Flush();
            //    sw.Close();
            //    var sr = new StreamReader((FileName));
            //    nlog.logger.Info(string.Format("<pre>{0}</pre>", sr.ReadToEnd()));
            //    sr.Close();
            //}
            //else
            //{
            //    StateAllError.StateIndex = 3;
            //}
            //#endregion
           
        }

        void CheckHaoShunError(string Device, IdasDataSet.IdasSourceRow Source)
        {
            try
            {
                switch (Source.Name)
                {
                    case "VW001100":
                        VW001100 = Source.RecentValue;
                        HaoShunReport(Device, Source);
                        break;
                    case "VW001102":
                        VW001102 = Source.RecentValue;
                        HaoShunReport(Device, Source);
                        break;
                    case "VW001104":
                        VW001104 = Source.RecentValue;
                        HaoShunReport(Device, Source);
                        break;
                    case "VW000750":
                        VW000750.Value = (float)Source.RecentValue;
                        break;
                    case "VW002100":
                        lock (VW002100.Text)
                        {
                            VW002100.Text = Source.RecentValue.ToString();
                        }
                        break;
                    case "VD000828":
                        lock (VD000828.Text)
                        {
                            VD000828.Text = Source.RecentValue.ToString();
                        }
                        break;
                    case "VD000832":
                        lock (VD000832.Text)
                        {
                            VD000832.Text = Source.RecentValue.ToString();
                        }
                        break;
                }
            }
            catch(Exception e)
            {
                nlog.logger.Error(e.ToString());
            }
        }

        void ASyncSaveData(string DevName, IdasDataSet.IdasSourceRow source)
        {
            lock(source)
            {
                var src = source.GetIdasDataRows();
                if (src.Count() > source.ViewSum)
                {
                    var set = new IdasDataSet.IdasDataDataTable();
                    foreach (var data in src)
                    {
                        var tmp = set.NewIdasDataRow();
                        tmp.ItemArray = data.ItemArray;
                        set.AddIdasDataRow(tmp);
                        data.Delete();
                    }
                    this.BeginInvoke(new EventHandler(delegate
                    {
                        set.WriteXml(string.Format(@"IdasData\\{0}-{1}-{2}-{3}.xml", DevName.ToString(), source.Name.ToString(), set.First().Time.ToString("yyyy年MM月dd日HH时mm分ss秒fff毫秒"), set.Last().Time.ToString("yyyy年MM月dd日HH时mm分ss秒fff毫秒")));
                    }));
                }
            }
        }

        public void IdasUpdateDataSet(string name, string RegId, UInt64 value, DateTime time)
        {
            try
            {
                if (false == IdasDeviceDb.ContainsKey(name))
                {
                    idasDeviceSet.IdasDevice.AddIdasDeviceRow(name, "无备注");
                    IdasDeviceDb.Add(name, new IdasDataSet());
                    idasDeviceSet.WriteXml(@"IdasDevice.xml");
                }

                var Set = IdasDeviceDb[name];

                var source = Set.IdasSource.FindByName(RegId);

                lock (Set)
                {
                    if (null == source)
                    {
                        source = Set.IdasSource.AddIdasSourceRow(RegId, "无备注", value, time, '#', 0, "全部", 100);
                    }

                    // ASyncSaveData(name, source);

                    Set.IdasData.AddIdasDataRow(source, value, time);

                    source.RecentValue = value;
                    source.RecentTime = time;
                }

                new Thread(() => { CheckErrorOperator(name, source); }).Start();

                new Thread(() => { CheckHaoShunError(name, source); }).Start();

            }
            catch(OutOfMemoryException err)
            {
                GC.Collect();
                nlog.logger.Error(err);
            }
            catch (Exception err)
            {
                nlog.logger.Error(err);
                // throw e;
            }
        }

        // 存储状态被改变为未选中的线条（欲被刷新恢复）
        HashSet<string> SerieChecked = new HashSet<string>();

        private void DataLineChart_LegendItemChecked(object sender, LegendItemCheckedEventArgs e)
        {
            var key = e.CheckedElement.Tag.ToString();
            // 新状态为假的，意味着要被刷新恢复，所以要存在字典容器里。
            if (false == e.NewCheckState)
            {
                // 不存在就添加
                if (false == SerieChecked.Contains(key))
                {
                    SerieChecked.Add(key);
                }
            }
            // 默认状态为选中，刷新重置的状态不会通知，则新状态为真的，都是原来点击过的，只需要从容器中移除即可。
            else
            {
                SerieChecked.Remove(key);
            }
        }

        void RestoreChart(ChartControl Chart)
        {
            if(SerieChecked.Count > 0)
            {
                var series = Chart.Series.ToArray();
                foreach (var item in SerieChecked)
                {
                    var result = Array.Find(series, s => s.Name == item);
                    if(null != result)
                    {
                        result.CheckedInLegend = false;
                    }
                }
            }
        }

        private void ReflashChart_Tick(object sender, EventArgs e)
        {
            switch (数据视图页.SelectedTabPage.Name)
            {
                case "数据折线图":
                    DataLineChart.RefreshData();
                    RestoreChart(DataLineChart);
                    break;
                case "数据统计图":
                    RestoreChart(DataBar3DChart);
                    break;
            }
        }

        #endregion

        #region 设备选项

        private void windowsUIButtonPanel1_ButtonClick(object sender, ButtonEventArgs e)
        {
            var button = ((WindowsUIButton)e.Button);
            string DevName;
            switch (button.Caption)
            {
                case "生成测试数据":
                    UInt64 value0 = (UInt64)new Random().Next(100);
                    IdasUpdateDataSet("test0", (value0 % 10).ToString(), value0, DateTime.Now);

                    UInt64 value1 = (UInt64)new Random().Next(2);
                    IdasUpdateDataSet("test1", (value0 % 10).ToString(), value1, DateTime.Now);
                    break;
                case "清空所有数据":
                    ((IdasDataSet)this.idasSourceBindingSource.DataSource).Clear();
                    idasDeviceSet.Clear();
                    break;
                case "保存设备配置":
                    try
                    {
                        DevName = 设备信息.Text.Split('：').Last();
                        var backup = new IdasDataSet();
                        foreach (var Src in IdasDeviceDb[DevName].IdasSource)
                        {
                            backup.IdasSource.AddIdasSourceRow(Src.Name, Src.Note, ulong.MinValue, DateTime.MinValue, Src.ErrorOperator, Src.ErrorValue, Src.ErrorReport, Src.ViewSum);
                        }
                        var dir = string.Format(@"IdasDevice\\{0}-{1}.IdasConfig", DevName, DateTime.Now.ToBinary());
                        IdasDeviceDb[DevName].IdasSource.WriteXml(dir);
                        XtraMessageBox.Show(this, string.Format("配置已保存在本软件如下位置：\n{0}。", dir));
                    }
                    catch(Exception err)
                    {
                        XtraMessageBox.Show(this, err.ToString());
                    }
                    break;
                case "读取设备配置":
                    try
                    {
                        var dlg = new OpenFileDialog();

                        // Filter by All Files
                        dlg.Filter = "Idas配置文件|*.IdasConfig";

                        dlg.InitialDirectory = System.Environment.CurrentDirectory + "\\IdasDevice";
                        
                        if(DialogResult.OK == dlg.ShowDialog())
                        {
                            DevName = 设备信息.Text.Split('：').Last();
                            var backup = new IdasDataSet();
                            backup.ReadXml(dlg.FileName);
                            var NowIdasSource = IdasDeviceDb[DevName].IdasSource;
                            foreach (var Src in backup.IdasSource)
                            {
                                try
                                {
                                    var src = NowIdasSource.FindByName(Src.Name);
                                    // 加载配置
                                    src.Note = Src.Note;
                                    src.ErrorOperator = Src.ErrorOperator;
                                    src.ErrorValue = Src.ErrorValue;
                                    src.ErrorReport = Src.ErrorReport;
                                }
                                catch (Exception)
                                {
                                    // 不重要错误，可忽略。
                                }
                            }
                            XtraMessageBox.Show(this, string.Format("指定配置已加载。"));
                        }
                    }
                    catch(Exception err)
                    {
                        XtraMessageBox.Show(this, err.ToString());
                    }
                    break;
                default:
                    break;
            }
        }

        private void TaskEachDay_Tick(object sender, EventArgs e)
        {
            var now = DateTime.Now;
            this.TaskEachDay.Interval = (int)((now.AddSeconds(1) - now).TotalMilliseconds);
            dateTimeToday.ResetText();
        }

        private void 设备列表控件_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            var hInfo = 设备列表控件.CalcHitInfo(new System.Drawing.Point(e.X, e.Y));
            if (e.Button == MouseButtons.Left && e.Clicks == 2)
            {
                //（尝试获取结点）判断光标是否在行范围内
                if (null != hInfo.Node)
                {
                    string DeviceName = hInfo.Node.GetDisplayText("Name");
                    // 重新加载其他数据源更新设备信息
                    LoadIdasDataSet(IdasDeviceDb[DeviceName]);
                    设备信息.Text = "设备名称：" + DeviceName;
                    // 确保设备信息显示
                    if (数据视图页.IsHidden == true)
                    {
                        数据视图页.RestoreFromCustomization();   //恢复到原来的Layout上面   
                        数据视图页.Move(设备信息, DevExpress.XtraLayout.Utils.InsertType.Top);
                    }
                }
            }
        }

        void NetListCheck()
        {
            listBox主机地址列表.Items.Clear();
            foreach (var item in Dns.GetHostEntry(Dns.GetHostName()).AddressList.Where(s => s.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork))
            {
                listBox主机地址列表.Items.Add(item.ToString());
            }
        }

        private void NetListCheck_Tick(object sender, EventArgs e)
        {
            NetListCheck();
        }

        private void buttonEditReportEmail_ButtonClick(object sender, DevExpress.XtraEditors.Controls.ButtonPressedEventArgs e)
        {
            try
            {
                // 更换配置发送的邮箱
                ((NLog.Targets.MailTarget)NLog.LogManager.Configuration.FindTargetByName("infoMail")).To = buttonEditReportEmail.EditValue.ToString();

                // 存储到配置中
                var AppConfig = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                AppConfig.AppSettings.Settings["ErrorReportEmail"].Value = buttonEditReportEmail.EditValue.ToString();
                AppConfig.Save();

                XtraMessageBox.Show(this, "设置成功");
            }
            catch (Exception ex)
            {
                XtraMessageBox.Show(this, ex.ToString());
            }
        }

        private void buttonEditSaveDataTime_ButtonClick(object sender, DevExpress.XtraEditors.Controls.ButtonPressedEventArgs e)
        {
            try
            {
                // 存储到配置中
                var AppConfig = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                AppConfig.AppSettings.Settings["SaveDataTime"].Value = buttonEditSaveDataTime.EditValue.ToString();
                AppConfig.Save();

                TaskSaveDataTime.Interval = int.Parse(buttonEditSaveDataTime.EditValue.ToString())*1000;

                XtraMessageBox.Show(this, "保存成功");
            }
            catch (Exception ex)
            {
                XtraMessageBox.Show(this, ex.ToString());
            }
        }

        private void 设备选项_Shown(object sender, EventArgs e)
        {
            NetListCheck();

            textBoxIpPort.Text = (ZwIdas.Port).ToString();
            // textBoxIpPort.Text = Idas.Port.ToString();

            // 从配置中读取保存时间间隔
            buttonEditSaveDataTime.EditValue = ConfigurationManager.AppSettings["SaveDataTime"].ToString();

            // 从配置中读取错误报告邮箱
            buttonEditReportEmail.EditValue = ConfigurationManager.AppSettings["ErrorReportEmail"].ToString();
            
            // 更换配置发送的邮箱
            ((NLog.Targets.MailTarget)NLog.LogManager.Configuration.FindTargetByName("infoMail")).To = buttonEditReportEmail.EditValue.ToString();

            TaskSaveDataTime.Interval = int.Parse(buttonEditSaveDataTime.EditValue.ToString()) * 1000;
        }

        private void TaskSaveDataTime_Tick(object sender, EventArgs e)
        {
            foreach (var device in IdasDeviceDb)
            {
                foreach (var source in device.Value.IdasSource)
                {
                    ASyncSaveData(device.Key, source);
                }
            }
        }

        #endregion
    }
}