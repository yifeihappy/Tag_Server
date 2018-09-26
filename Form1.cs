//#define TEMPERATRATURE

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Threading;
using ModuleTech;
using System.Diagnostics;
using ModuleTech.Gen2;
using ModuleLibrary;
using System.IO;
using System.Runtime.InteropServices;
using ModuleReaderManager.HttpServer;

namespace ModuleReaderManager
{

    public partial class Form1 : Form
    {

        public static Form1 instance;//提供给Server调用
        public static string EPC;//EPC 号
        private static String ipAddress = "0.0.0.0";
        private static int portAddress = 9910;


        public Form1()
        {
            instance = this;//赋值
            InitializeComponent();
            Thread thread = new Thread(startHttpServer);
            thread.Start();

            cbbreadertype.SelectedIndex = 0;
            cbant1.Checked = true;

        }
        Dictionary<string, TagInfo> m_Tags = new Dictionary<string, TagInfo>();
        Mutex tagmutex = new Mutex();
        public ReaderParams rParms = new ReaderParams(200, 0, 1);

        bool isInventory = false;
        bool isConnect = false;

        /// <summary>
        /// 启动服务器
        /// </summary>
        private void startHttpServer()
        {
            UHFHttpServer server = new UHFHttpServer(ipAddress, portAddress);
            server.Logger = new ConsoleLogger();
            try
            {
                server.Start();
            }
            catch (Exception e)
            {
                server.Logger.Log(e.ToString());
            }
        }

        /// <summary>
        /// 连接读写器
        /// </summary>
        /// <returns></returns>
        public bool connectReader()
        {
            if(!btnconnect.Enabled)
            {
                return false;
            }
            this.btnconnect.BeginInvoke(new Action(() => { btnconnect_Click(null, null); }));
            Thread.Sleep(300);
            if (btnconnect.Enabled)
            {
               // return false;
            }
            return true;

            
        }

        /// <summary>
        /// 开启读取
        /// </summary>
        /// <returns></returns>
        public bool startReader()
        {
            if(!connectReader())
            {
                return false;
            }
            this.btnstart.BeginInvoke(new Action(() => { btnstart_Click(null, null); }));
            Thread.Sleep(300);
            if (!btnstop.Enabled)
            {
               // return false;
            }
            return true;
        }

        /// <summary>
        /// 停止读取
        /// </summary>
        /// <returns></returns>
        public bool stopReader()
        {
            if (!btnstop.Enabled)
            {
                return false;
            }
            //停止读取
            this.btnstop.BeginInvoke(new Action(() => { btnstop_Click(null, null); }));
            Thread.Sleep(300);
            if (btnstop.Enabled)
            {
                return false;
            }
            if(!disconnectReader())
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// 断开与读写器的连接
        /// </summary>
        /// <returns></returns>
        public bool disconnectReader()
        {
            if(!btndisconnect.Enabled)
            {
                return false;
            }
            this.btndisconnect.BeginInvoke(new Action(() => { btndisconnect_Click(null, null); }));
            Thread.Sleep(300);
            if(!btnconnect.Enabled)
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// 将每次盘点到的新标签加入本地缓冲m_Tags中，已经盘点次数累加
        /// </summary>
        /// <param name="tag"></param>
        void AddTagToDic(TagReadData tag)
        {
            TagInfo tmptag = null;

            if (rParms.isUniByEmd && (tag.Tag.Protocol == TagProtocol.GEN2))
            {
                if (tag.EMDDataString == string.Empty)
                    return;
            }
            //
            tagmutex.WaitOne();

            string keystr = tag.EPCString;
            EPC = tag.EPCString;
            Console.WriteLine("EPC: " + EPC);

            if (rParms.isUniByEmd)
                keystr += tag.EMDDataString;


            if (rParms.isUniByAnt)
                keystr += tag.Antenna.ToString();

            if (m_Tags.ContainsKey(keystr))
            {
                tmptag = m_Tags[keystr];
                if (rParms.isOneReadOneTime)
                    tmptag.readcnt += 1;
                else
                    tmptag.readcnt += tag.ReadCount;

                tmptag.Frequency = tag.Frequency;
                tmptag.RssiRaw = tag.Rssi;
                tmptag.Phase = tag.Phase;
                tmptag.antid = tag.Antenna;

                if (!rParms.isUniByEmd)
                {
                    if (tmptag.emddatastr != tag.EMDDataString)
                    {
                        if (tag.EMDDataString != string.Empty)
                            tmptag.emddatastr = tag.EMDDataString;
                    }
                }

                //added on 3-26
                if (rParms.isIdtAnts)
                {
                    TimeSpan span = tag.Time - tmptag.timestamp;
                    //                  Console.WriteLine("span.TotalMilliseconds:" + span.TotalMilliseconds.ToString()
                    //                      + " tag.ReadCount:" + tag.ReadCount.ToString());
                    if (tag.Rssi <= 0)
                        tmptag.RssiSum += (tag.Rssi + 120) * tag.ReadCount * ((int)span.TotalMilliseconds / 80);
                    else
                        tmptag.RssiSum += tag.Rssi * tag.ReadCount * ((int)span.TotalMilliseconds / 80);
                }

                tmptag.timestamp = tag.Time;
                ///
            }
            else
            {
                TagInfo newtag = null;
                if (rParms.isOneReadOneTime)
                {
                    newtag = new TagInfo(tag.EPCString, 1, tag.Antenna, tag.Time,
                        tag.Tag.Protocol, tag.EMDDataString);
                    newtag.RssiRaw = tag.Rssi;
                    newtag.Phase = tag.Phase;
                    newtag.Frequency = tag.Frequency;
                }
                else
                {
                    newtag = new TagInfo(tag.EPCString, tag.ReadCount, tag.Antenna, tag.Time,
                        tag.Tag.Protocol, tag.EMDDataString);
                    newtag.RssiRaw = tag.Rssi;
                    newtag.Phase = tag.Phase;
                    newtag.Frequency = tag.Frequency;
                }

                m_Tags.Add(keystr, newtag);

                //added on 3-26
                if (rParms.isIdtAnts)
                {
                    if (tag.Rssi <= 0)
                        newtag.RssiSum += (tag.Rssi + 120) * tag.ReadCount;
                    else
                        newtag.RssiSum += tag.Rssi * tag.ReadCount;
                }
                ///
            }
            //if (!tagbuf.ContainsKey(tag.EPCString))
            //    tagbuf.Add(tag.EPCString, tag);
            tagmutex.ReleaseMutex();
        }
        public Reader modulerdr = null;

        delegate void ReconnectHandler(int reason, Exception ex);

        string logstr = "";

        bool IsChkAnts = true;

        ReaderExceptionChecker rechecker = new ReaderExceptionChecker(4, 60);
        void Reconnect(int reason, Exception ex)
        {
            this.rtbopfailmsg.Text = logstr;
            if (reason > 0)
            {
                this.btnconnect.Enabled = true;
                this.btnstart.Enabled = false;
                this.btnstop.Enabled = false;
                this.readparamenu.Enabled = false;
                this.Custommenu.Enabled = false;
                this.tagopmenu.Enabled = false;
                this.MsgDebugMenu.Enabled = false;
                this.timer1.Enabled = false;
                this.btnInvParas.Enabled = false;
                this.btndisconnect.Enabled = true;
                this.btn16antset.Enabled = false;
                for (int f = 1; f <= allAnts.Count; ++f)
                {
                    allAnts[f].Checked = false;
                    allAnts[f].Enabled = false;
                    allAnts[f].ForeColor = antdefaulcolor;
                }

                this.timer1.Enabled = false;
                this.toolStripStatusLabel1.Text = "断开";
                if (modulerdr != null)
                {
                    modulerdr.Disconnect();
                    modulerdr = null;
                    isConnect = false;
                }
                if (reason == 1)
                    MessageBox.Show("重新连接读写器失败");
                else if (reason == 2)
                    MessageBox.Show("读写器异常频率过高:" + ex.ToString());
            }
        }

        string sourceip;
        Color antdefaulcolor;

        public string serialcommunicationmsg = "";

        void AddTagsToDic(object sender, Reader.TagsReadEventArgs tagsArgs)
        {
            //System.Console.WriteLine(DateTime.Now.ToString());
            foreach (TagReadData tag in tagsArgs.Tags)
            {
                AddTagToDic(tag);
            }
        }

        delegate void StartReadingByBtn(object sender, EventArgs e);

        Mutex UiMux = new Mutex();

        /// <summary>
        /// 盘点过程中，出现错误处理。判断是否需要重连，重连处理将重新设置相关函数，参数
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="expArgs"></param>
        void ReadingErrHandler(object sender, Reader.ReadExceptionEventArgs expArgs)
        {
            UiMux.WaitOne();
            Exception ex = expArgs.ReaderException;
            Debug.WriteLine("读写器" + ((Reader)sender).Address + "错误:" + ex.ToString());
            logstr += ex.ToString();
            logstr += "\n";
            Reader expreader = (Reader)sender;
            if (rechecker.IsTrigger())
            {
                this.BeginInvoke(new ReconnectHandler(Reconnect), new object[] { 2, expArgs.ReaderException });
                UiMux.ReleaseMutex();
                return;
            }
            else
                rechecker.AddErr();

            /*
            if (((ModuleException)ex).ErrCode != 0x900c)
            {
                expreader.Disconnect();
                isConnect = false;
                modulerdr = null;
                this.BeginInvoke(new ReconnectHandler(Reconnect), new object[] { 2, expArgs.ReaderException });
                UiMux.ReleaseMutex();
                return;
            }*/

            string rdraddress = expreader.Address;
            expreader.Disconnect();
            modulerdr = null;
            isConnect = false;

            for (int i = 0; i < rParms.reconnectcnt; ++i)
            {
                try
                {
                    modulerdr = Reader.Create(rdraddress, ModuleTech.Region.NA, readerantnumber);
                    modulerdr.TagsRead += AddTagsToDic;
                    modulerdr.ReadException += ReadingErrHandler;
                    isConnect = true;
                    this.BeginInvoke(new StartReadingByBtn(btnstart_Click), new object[] { null, null });
                    break;
                }
                catch
                {
                    if (i == rParms.reconnectcnt - 1)
                    {
                        this.BeginInvoke(new ReconnectHandler(Reconnect), new object[] { 1, null });
                        UiMux.ReleaseMutex();
                        return;
                    }
                    else
                        Thread.Sleep(rParms.connectinterval * 1000);
                }
            }

            this.BeginInvoke(new ReconnectHandler(Reconnect), new object[] { 0, expArgs.ReaderException });
            UiMux.ReleaseMutex();
        }

        public int readerantnumber;
        private void btnconnect_Click(object sender, EventArgs e)
        {
            cbbreadertype.SelectedIndex = 0;
            cbant1.Checked = true;
            int initstep = 0;
            if (this.cbbreadertype.SelectedIndex == -1)
            {
                MessageBox.Show("请选择天线端口数");
                return;
            }

            if (modulerdr != null)
            {
                modulerdr.Disconnect();
                isConnect = false;
                modulerdr = null;
            }

            rParms.resetParams();
            sourceip = this.tbip.Text.Trim();//读卡器串口号
            System.Diagnostics.Debug.WriteLine("sourceip:" + sourceip);
            if (!this.tbip.Text.Trim().ToLower().Contains("com"))
            {
                rParms.hasIP = true;
            }
            else
                rParms.hasIP = false;

            try
            {
                int st = Environment.TickCount;
                System.Diagnostics.Debug.WriteLine("index:" + cbbreadertype.SelectedIndex);
                if (cbbreadertype.SelectedIndex < 4)
                {
                    modulerdr = Reader.Create(sourceip, ModuleTech.Region.NA, cbbreadertype.SelectedIndex + 1);
                    readerantnumber = cbbreadertype.SelectedIndex + 1;
                    rParms.antcnt = readerantnumber;
                }
                else if (cbbreadertype.SelectedIndex == 5)
                {
                    modulerdr = Reader.Create(sourceip, ModuleTech.Region.NA, 16);
                    readerantnumber = 16;
                    rParms.antcnt = 0;
                }
                else if (cbbreadertype.SelectedIndex == 4)
                {
                    modulerdr = Reader.Create(sourceip, ModuleTech.Region.NA, 8);
                    readerantnumber = 8;
                    rParms.antcnt = 0;
                }
                Debug.WriteLine("connect time:" + (Environment.TickCount - st).ToString());


                rParms.AntsState.Clear();
                if (readerantnumber < 8)
                {
                    for (int aa = 1; aa <= cbbreadertype.SelectedIndex + 1; ++aa)
                    {
                        allAnts[aa].Enabled = true;
                        allAnts[aa].ForeColor = Color.Red;
                    }
                }

                rParms.readertype = modulerdr.HwDetails.logictype;
                if (modulerdr.HwDetails.module == Reader.Module_Type.MODOULE_M6E ||
                    modulerdr.HwDetails.module == Reader.Module_Type.MODOULE_M6E_PRC
                    || modulerdr.HwDetails.module == Reader.Module_Type.MODOULE_M6E_MICRO)
                    rParms.isMultiPotl = true;

                if (modulerdr.HwDetails.board == Reader.MaindBoard_Type.MAINBOARD_WIFI)
                    rParms.hasIP = false;

                //判断是否支持多协议
                if (!rParms.isMultiPotl)
                {
                    this.cbpotl6b.Enabled = false;
                    this.cbpotlipx256.Enabled = false;
                    this.cbpotlipx64.Enabled = false;
                    this.cbpotlgen2.Checked = true;
                    this.cbpotlgen2.Enabled = false;
                    this.iso183k6btagopToolStripMenuItem.Enabled = false;
                }
                else
                {
                    this.cbpotlgen2.Checked = false;
                    this.cbpotl6b.Enabled = true;
                    this.cbpotlipx256.Enabled = true;
                    this.cbpotlipx64.Enabled = true;
                    this.cbpotlgen2.Enabled = true;
                    this.iso183k6btagopToolStripMenuItem.Enabled = true;
                }

                //获取连接天线
                if (readerantnumber < 8)
                {
                    int[] connectedants = (int[])modulerdr.ParamGet("ConnectedAntennas");
                    initstep = 1;
                    for (int c = 0; c < connectedants.Length; ++c)
                        allAnts[connectedants[c]].ForeColor = Color.Green;

                    for (int ff = 1; ff <= allAnts.Count; ++ff)
                    {
                        if (allAnts[ff].Enabled)
                        {
                            if (allAnts[ff].ForeColor == Color.Green)
                                rParms.AntsState.Add(new AntAndBoll(ff, true));
                            else
                                rParms.AntsState.Add(new AntAndBoll(ff, false));
                        }
                    }
                }
                /*
                VswrQueryParam vqp = new VswrQueryParam();
                vqp.AntId = (byte)1;
                vqp.Rg = ModuleTech.Region.NA;
                vqp.Power = (ushort)3000;
                vqp.Frequencies = new int[] {903250, 915250, 924250 };
                modulerdr.ParamSet("AntPowerVswr", vqp);
                Dictionary<int, int> antvs = (Dictionary<int, int>)modulerdr.ParamGet("AntPowerVswr");
                
                byte[] errdata = (byte[])modulerdr.ParamGet("ErrorData");*/
                //                byte[] rdl = (byte[])modulerdr.ParamGet("ReaderDetails");
                //                Debug.WriteLine(rdl[0].ToString());
                //                modulerdr.ParamSet("ResetRfidModule", (byte)0);
                rParms.hardvir = (string)modulerdr.ParamGet("HardwareVersion");
                initstep = 2;
                Debug.WriteLine("before SoftwareVersion");
                rParms.softvir = (string)modulerdr.ParamGet("SoftwareVersion");
                initstep = 3;
                char[] sep = new char[1];
                sep[0] = '.';
                string[] verfields = rParms.softvir.Split(sep, StringSplitOptions.None);
                int verint = int.Parse(verfields[0] + verfields[1]);
                if ((modulerdr.HwDetails.board == Reader.MaindBoard_Type.MAINBOARD_ARM7 ||
                    modulerdr.HwDetails.board == Reader.MaindBoard_Type.MAINBOARD_SERIAL) &&
                    modulerdr.HwDetails.module == Reader.Module_Type.MODOULE_SLR1100 &&
                    verint >= 1612)
                    rParms.isFastRead = true;
                if (modulerdr.HwDetails.module == Reader.Module_Type.MODOULE_SLR5900 ||
                    modulerdr.HwDetails.module == Reader.Module_Type.MODOULE_SLR5800 ||
                    modulerdr.HwDetails.module == Reader.Module_Type.MODOULE_SLR6000 ||
                    modulerdr.HwDetails.module == Reader.Module_Type.MODOULE_SLR6100)
                {
                    modulerdr.ParamSet("HopFrequencyMode", 1);
                    rParms.isFastRead = true;
                }

                //获取最大，最小功率值
                rParms.powermax = ((int)modulerdr.ParamGet("RfPowerMax")) / 100;
                initstep = 4;
                rParms.powermin = ((int)modulerdr.ParamGet("RfPowerMin")) / 100;
                initstep = 5;
                rParms.gen2session = (int)modulerdr.ParamGet("Gen2Session");
                initstep = 6;
                rParms.fisrtLoad = true;
                //设置回调函数，标签回调和异常回调
                modulerdr.TagsRead += AddTagsToDic;
                modulerdr.ReadException += ReadingErrHandler;

                isConnect = true;

                this.btnstart.Enabled = true;
                this.readparamenu.Enabled = true;
                this.Custommenu.Enabled = true;
                this.tagopmenu.Enabled = true;
                this.MsgDebugMenu.Enabled = true;
                this.menutest.Enabled = true;
                this.menuoutputtags.Enabled = true;
                this.btnconnect.Enabled = false;
                this.btnInvParas.Enabled = true;
                this.btndisconnect.Enabled = true;
                this.updatemenu.Enabled = false;
                if (readerantnumber >= 8)
                {
                    invants16setting.Clear();
                    this.btn16antset.Enabled = true;
                }
                this.toolStripStatusLabel1.Text = "连接成功";
            }
            catch (Exception ex)
            {

                MessageBox.Show("连接失败，请检查读写器地址是否正确" + ex.ToString() + " step:" + initstep);
                this.toolStripStatusLabel1.Text = "连接失败";
                return;
            }
        }

        /// <summary>
        /// 检查天线是否可用
        /// </summary>
        /// <returns></returns>
        private List<AntAndBoll> CheckAntsValid()
        {
            List<AntAndBoll> selants = new List<AntAndBoll>();

            for (int cc = 1; cc <= allAnts.Count; ++cc)
            {
                if (allAnts[cc].Checked)
                {
                    if (allAnts[cc].ForeColor == Color.Red)
                        selants.Add(new AntAndBoll(cc, false));
                    else
                        selants.Add(new AntAndBoll(cc, true));
                }
            }

            return selants;
        }

        /// <summary>
        /// 判断是否有效二进制字符串
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static int IsValidBinaryStr(string str)
        {
            if (str == "")
                return -3;

            foreach (Char a in str)
            {
                if (!((a == '1') || (a == '0')))
                    return -1;
            }
            return 0;

        }


        /// <summary>
        /// 检查密码是否有效，长度为8的16进制字符串
        /// </summary>
        /// <param name="passwd"></param>
        /// <returns></returns>
        public static int IsValidPasswd(string passwd)
        {
            int ret = IsValidHexstr(passwd, 8);
            if (ret == 0)
            {
                if (passwd.Length != 8)
                    return -4;
            }

            return ret;
        }

        /// <summary>
        /// 判断是否合法的16进制字符串
        /// </summary>
        /// <param name="str"></param>
        /// <param name="len"></param>
        /// <returns></returns>
        public static int IsValidHexstr(string str, int len)
        {
            if (str == "")
                return -3;
            if (str.Length % 4 != 0)
                return -2;
            if (str.Length > len)
                return -4;
            string lowstr = str.ToLower();
            byte[] hexchars = Encoding.ASCII.GetBytes(lowstr);

            foreach (byte a in hexchars)
            {
                if (!((a >= 48 && a <= 57) || (a >= 97 && a <= 102)))
                    return -1;
            }
            return 0;
        }

        int starttm;
        public List<AntAndBoll> invants16setting = new List<AntAndBoll>();
        public bool is16AntRevert = false;

        /// <summary>
        /// 开始盘点操作，检查必要的参数，然后启动后台盘点线程，设置标签以及异常处理事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnstart_Click(object sender, EventArgs e)
        {
            List<AntAndBoll> selants = null;
            if (readerantnumber < 8)
            {
                selants = CheckAntsValid();
                if (selants.Count == 0)
                {
                    MessageBox.Show("请选择天线");
                    return;
                }
            }
            else
            {
                selants = invants16setting;
                if (selants.Count == 0)
                {
                    MessageBox.Show("请点击'16天线设置'按钮选择天线");
                    return;
                }
            }


            if (IsChkAnts && sender != null)
            {
                for (int i = 0; i < selants.Count; ++i)
                {
                    if (selants[i].isConn == false)
                    {
                        DialogResult stat = DialogResult.OK;
                        stat = MessageBox.Show("在未检测到天线的端口执行搜索，真的要执行吗?", "警告",
                                        MessageBoxButtons.OKCancel, MessageBoxIcon.Question,
                                        MessageBoxDefaultButton.Button2);
                        if (stat == DialogResult.OK)
                            break;
                        else
                            return;
                    }
                }
            }

            List<int> antsExe = new List<int>();
            for (int i = 0; i < selants.Count; ++i)
            {
                antsExe.Add(selants[i].antid);
            }

            if ((!this.cbpotl6b.Checked) && (!this.cbpotlgen2.Checked) && (!this.cbpotlipx64.Checked)
                && (!this.cbpotlipx256.Checked))
            {
                MessageBox.Show("请选择协议");
                return;
            }
            // 设置ReadPlan 必要项，指定协议和天线组
            UiMux.WaitOne();
            List<SimpleReadPlan> readplans = new List<SimpleReadPlan>();
            if (this.cbpotl6b.Checked)
                readplans.Add(new SimpleReadPlan(TagProtocol.ISO180006B, antsExe.ToArray(), rParms.weight180006b));
            if (this.cbpotlgen2.Checked)
                readplans.Add(new SimpleReadPlan(TagProtocol.GEN2, antsExe.ToArray(), rParms.weightgen2));
            if (this.cbpotlipx256.Checked)
                readplans.Add(new SimpleReadPlan(TagProtocol.IPX256, antsExe.ToArray(), rParms.weightipx256));
            if (this.cbpotlipx64.Checked)
                readplans.Add(new SimpleReadPlan(TagProtocol.IPX64, antsExe.ToArray(), rParms.weightipx64));
            if (readplans.Count > 1)
                modulerdr.ParamSet("ReadPlan", new MultiReadPlan(readplans.ToArray()));
            else
                modulerdr.ParamSet("ReadPlan", readplans[0]);

            m_Tags.Clear();
            //added on 3-26
            if (rParms.isFastRead)
                rParms.readdur = 50;
            if (rParms.isIdtAnts && rParms.IdtAntsType == 1)
                this.timer1.Interval = rParms.DurIdtval;
            else
                this.timer1.Interval = rParms.readdur;
            ///
            this.timer1.Enabled = true;

            try
            {
                BackReadOption bro = new BackReadOption();
                bro.IsFastRead = rParms.isFastRead;

                bro.ReadDuration = (ushort)rParms.readdur;
                bro.ReadInterval = (uint)rParms.sleepdur;
                bro.FRTMetadata = rParms.FRTMeta;
                modulerdr.ParamSet("BackReadOption", bro);
                if (rParms.GpiTriiger != null)
                    modulerdr.ParamSet("BackReadGPITrigger", rParms.GpiTriiger);
                else
                    modulerdr.ParamSet("BackReadGPITrigger", null);
                modulerdr.StartReading();
                starttm = Environment.TickCount;
            }
            catch (System.Exception ex)
            {
                MessageBox.Show("启动读取失败:" + ex.ToString());
                UiMux.ReleaseMutex();
                return;
            }
            isInventory = true;
            this.labreadtime.Text = "0";

            this.btndisconnect.Enabled = false;
            this.btnstop.Enabled = true;
            this.readparamenu.Enabled = false;
            this.btn16antset.Enabled = false;
            this.Custommenu.Enabled = false;
            this.menutest.Enabled = false;
            this.tagopmenu.Enabled = false;
            this.MsgDebugMenu.Enabled = false;
            this.menuoutputtags.Enabled = false;
            this.btnstart.Enabled = false;
            this.btnInvParas.Enabled = false;
            this.toolStripStatusLabel1.Text = "Inventory";
            UiMux.ReleaseMutex();
        }

        /// <summary>
        /// 停止盘点，刷新界面
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnstop_Click(object sender, EventArgs e)
        {
            UiMux.WaitOne();
            this.timer1.Enabled = false;
            isInventory = false;
            try
            {
                modulerdr.StopReading();
                this.labreadtime.Text = (Environment.TickCount - starttm).ToString();
            }
            catch (Exception exp)
            {
                MessageBox.Show("停止读取失败:" + exp.ToString());
                UiMux.ReleaseMutex();
                return;
            }

            timer1_Tick(null, null);
            this.btnstop.Enabled = false;
            this.readparamenu.Enabled = true;
            if (readerantnumber >= 8)
                this.btn16antset.Enabled = true;
            this.Custommenu.Enabled = true;
            this.menutest.Enabled = true;
            this.tagopmenu.Enabled = true;
            this.MsgDebugMenu.Enabled = true;
            this.btnstart.Enabled = true;
            this.menuoutputtags.Enabled = true;
            this.btndisconnect.Enabled = true;
            this.btnInvParas.Enabled = true;
            this.toolStripStatusLabel1.Text = "";
            IsChkAnts = true;
            UiMux.ReleaseMutex();
        }


        /// <summary>
        /// 清空操作
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button1_Click(object sender, EventArgs e)
        {
            tagmutex.WaitOne();
            m_Tags.Clear();
            tagmutex.ReleaseMutex();
            this.lvTags.Items.Clear();
            this.taglvdic.Clear();
        }
        List<TagInfo> tmplist = new List<TagInfo>();
        //        List<string> dellist = new List<string>();
        Dictionary<string, ListViewItem> taglvdic = new Dictionary<string, ListViewItem>();
        /// <summary>
        /// timer 事件用于刷新界面，显示标签盘点状态
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void timer1_Tick(object sender, EventArgs e)
        {

            tmplist.Clear();

            tagmutex.WaitOne();
#if(TEMPERATRATURE)
            label6.Text = tempture.ToString();
#endif
            foreach (TagInfo tag in m_Tags.Values)
            {
                tmplist.Add(tag);
            }
            tagmutex.ReleaseMutex();

            //added on 3-26
            if (rParms.isIdtAnts)
            {
                Dictionary<string, TagInfo> tmp_Tags = new Dictionary<string, TagInfo>();
                if (rParms.IdtAntsType == 2)
                {
                    List<TagInfo> tags_dy = new List<TagInfo>();
                    List<TagInfo> tags_xy = new List<TagInfo>();
                    List<TagInfo> tags_del = new List<TagInfo>();

                    foreach (TagInfo tag in tmplist)
                    {
                        TimeSpan span = DateTime.Now - tag.timestamp;
                        if (span.Seconds > rParms.AfterIdtWaitval)
                            tags_dy.Add(tag);
                        else
                            tags_xy.Add(tag);
                    }

                    foreach (TagInfo tag_dy in tags_dy)
                    {
                        foreach (TagInfo tag_xy in tags_xy)
                        {
                            bool isfind = false;
                            if (rParms.isUniByEmd)
                            {
                                if ((tag_dy.epcid + tag_dy.emddatastr) == (tag_xy.epcid + tag_xy.emddatastr))
                                    isfind = true;
                            }
                            else
                            {
                                if (tag_dy.epcid == tag_xy.epcid)
                                    isfind = true;
                            }
                            if (isfind)
                            {
                                tags_del.Add(tag_dy);
                                break;
                            }
                        }
                    }

                    foreach (TagInfo tag in tags_del)
                    {
                        tags_dy.Remove(tag);
                    }
                    tmplist = tags_dy;
                }

                foreach (TagInfo tag in tmplist)
                {
                    string unistr = null;
                    if (rParms.isUniByEmd)
                        unistr = tag.epcid + tag.emddatastr;
                    else
                        unistr = tag.epcid;
                    if (tmp_Tags.ContainsKey(unistr))
                    {
                        if (tmp_Tags[unistr].RssiSum < tag.RssiSum)
                        {

                            tmp_Tags.Remove(unistr);
                            tmp_Tags.Add(unistr, tag);
                        }
                    }
                    else
                        tmp_Tags.Add(unistr, tag);
                }

                tagmutex.WaitOne();
                foreach (TagInfo tag in tmplist)
                {
                    string keystr = tag.epcid;

                    if (rParms.isUniByEmd)
                        keystr += tag.emddatastr;

                    if (rParms.isUniByAnt)
                        keystr += tag.antid.ToString();
                    m_Tags.Remove(keystr);
                }

                tagmutex.ReleaseMutex();
                tmplist.Clear();
                foreach (TagInfo tag in tmp_Tags.Values)
                {
                    tmplist.Add(tag);
                }

            }

            int ant1cnt = 0;
            int ant2cnt = 0;
            int ant3cnt = 0;
            int ant4cnt = 0;

            foreach (TagInfo tag in tmplist)
            {
                if (tag.antid == 1)
                    ant1cnt++;
                if (tag.antid == 2)
                    ant2cnt++;
                if (tag.antid == 3)
                    ant3cnt++;
                if (tag.antid == 4)
                    ant4cnt++;

                string epckeystr = tag.epcid;
                if (rParms.isUniByEmd)
                    epckeystr += tag.emddatastr;
                else if (rParms.isUniByAnt)
                    epckeystr += tag.antid.ToString();

                if (taglvdic.ContainsKey(epckeystr))
                {
                    ListViewItem viewitem = taglvdic[epckeystr];
                    int isupdatecolor = 0;
                    if (viewitem.SubItems[4].Text != tag.emddatastr)
                    {
                        isupdatecolor = 1;
                        viewitem.SubItems[4].Text = tag.emddatastr;
                    }

                    if (tag.readcnt != int.Parse(viewitem.SubItems[1].Text))
                    {
                        isupdatecolor = 1;
                        viewitem.SubItems[1].Text = tag.readcnt.ToString();
                    }

                    if (tag.antid != int.Parse(viewitem.SubItems[3].Text))
                    {
                        isupdatecolor = 1;
                        viewitem.SubItems[3].Text = tag.antid.ToString();
                    }

                    viewitem.SubItems[6].Text = tag.RssiRaw.ToString();
                    viewitem.SubItems[8].Text = tag.Phase.ToString();
                    viewitem.SubItems[7].Text = tag.Frequency.ToString();


                    if (rParms.isChangeColor)
                    {
                        if (isupdatecolor == 0)
                        {
                            TimeSpan span = DateTime.Now - tag.timestamp;
                            if (span.Seconds > 2 && span.Seconds < 4)
                                viewitem.BackColor = Color.Silver;
                            else if (span.Seconds >= 4)
                                viewitem.BackColor = Color.DimGray;
                            //            else
                            //                viewitem.BackColor = Color.White;
                        }
                        else
                            viewitem.BackColor = Color.White;
                    }
                }
                else
                {
                    ListViewItem item = new ListViewItem(lvTags.Items.Count.ToString());
                    item.SubItems.Add(tag.readcnt.ToString());
                    item.SubItems.Add(tag.epcid);
                    item.SubItems.Add(tag.antid.ToString());

                    item.SubItems.Add(tag.emddatastr);

                    if (tag.potl == TagProtocol.GEN2)
                        item.SubItems.Add("GEN2");
                    else if (tag.potl == TagProtocol.ISO180006B)
                        item.SubItems.Add("ISO180006B");
                    else if (tag.potl == TagProtocol.IPX256)
                        item.SubItems.Add("IPX256");
                    else if (tag.potl == TagProtocol.IPX64)
                        item.SubItems.Add("IPX64");
                    else
                        item.SubItems.Add("GEN2");

                    item.SubItems.Add(tag.RssiRaw.ToString());
                    item.SubItems.Add(tag.Frequency.ToString());
                    item.SubItems.Add(tag.Phase.ToString());
                    lvTags.Items.Add(item);
                    taglvdic.Add(epckeystr, item);
                }
            }

            this.labant1cnt.Text = ant1cnt.ToString();
            this.labant2cnt.Text = ant2cnt.ToString();
            this.labant3cnt.Text = ant3cnt.ToString();
            this.labant4cnt.Text = ant4cnt.ToString();
            //modify on 3-26
            this.labtotalcnt.Text = lvTags.Items.Count.ToString();
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            UiMux.WaitOne();
            this.timer1.Enabled = false;
            if (isConnect)
            {
                if (isInventory)
                    modulerdr.StopReading();
                modulerdr.Disconnect();
            }
            UiMux.ReleaseMutex();
        }

        Dictionary<int, CheckBox> allAnts = new Dictionary<int, CheckBox>();
        string cur_dir = null;
        //       ProgramLog taginfolog = null;

        private void Form1_Load(object sender, EventArgs e)
        {
            this.cbpotl6b.Enabled = false;
            this.cbpotlipx64.Enabled = false;
            this.cbpotlipx256.Enabled = false;
            this.cbpotlgen2.Enabled = false;
            this.iso183k6btagopToolStripMenuItem.Enabled = false;
            this.Custommenu.Enabled = false;
            this.readparamenu.Enabled = false;
            this.btn16antset.Enabled = false;
            this.tagopmenu.Enabled = false;
            this.MsgDebugMenu.Enabled = false;
            this.menutest.Enabled = false;
            this.menuoutputtags.Enabled = false;
            this.btnstart.Enabled = false;
            this.btnstop.Enabled = false;
            this.btnInvParas.Enabled = false;
            this.toolStripStatusLabel1.Text = "未连接";

            cbant1.Checked = true;//默认选中
            allAnts.Add(1, cbant1);
            allAnts.Add(2, cbant2);
            allAnts.Add(3, cbant3);
            allAnts.Add(4, cbant4);
            antdefaulcolor = cbant1.ForeColor;

            for (int f = 1; f <= allAnts.Count; ++f)
                allAnts[f].Enabled = false;

            //            lvTags.Font = new Font("", 12, FontStyle.Bold);
            cur_dir = Environment.CurrentDirectory;
            //           this.taginfolog = new ProgramLog(3, "taginfo");
        }

        private void readparamenu_Click(object sender, EventArgs e)
        {
            rParms.isIpModify = false;
            rParms.isM5eModify = false;

            if (rParms.hasIP)
            {
                //if (rParms.readertype == ReaderType.MT_TWOANTS ||
                //rParms.readertype == ReaderType.MT_FOURANTS)
                //{
                if (!rParms.isGetIp)
                {
                    ReaderIPInfo ipinfo = null;
                    try
                    {
                        ipinfo = (ReaderIPInfo)modulerdr.ParamGet("IPAddress");
                        rParms.ip = ipinfo.IP;
                        rParms.subnet = ipinfo.SUBNET;
                        rParms.gateway = ipinfo.GATEWAY;
                        if (ipinfo.MACADDR != null)
                        {
                            rParms.macstr = ByteFormat.ToHex(ipinfo.MACADDR);
                        }
                        rParms.isGetIp = true;
                    }
                    catch
                    {
                        rParms.hasIP = false;
                    }

                }
            }

            readerParaform frm = new readerParaform(rParms, modulerdr);
            if (frm.ShowDialog() == DialogResult.Cancel)
                return;

        }



        //Gen2TagFilter filter = null;
        //EmbededCmdData embededdata = null;

        private void btnInvParas_Click(object sender, EventArgs e)
        {
            InventoryParasform frm = new InventoryParasform(this);
            frm.ShowDialog();
        }

        private void updatemenu_Click(object sender, EventArgs e)
        {
            if (rParms.readertype == ReaderType.PR_ONEANT)
            {
                MessageBox.Show("此类型读写器不支持升级操作");
                return;
            }
            updatefrm frm = new updatefrm();
            frm.ShowDialog();
        }

        private void Custommenu_Click(object sender, EventArgs e)
        {
            if (rParms.readertype != ReaderType.PR_ONEANT)
            {
                CustomCmdFrm frm = new CustomCmdFrm(modulerdr, rParms);
                frm.ShowDialog();
            }
            else
                MessageBox.Show("此类型读写器不支持标签特殊指令");
        }


        private void gen2tagopMenuItem_Click(object sender, EventArgs e)
        {
            gen2opForm frm = new gen2opForm(modulerdr, rParms, this);
            frm.StartPosition = FormStartPosition.CenterParent;
            frm.ShowDialog();
        }

        private void iso183k6btagopToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Iso186bopForm frm = new Iso186bopForm(modulerdr, rParms);
            frm.StartPosition = FormStartPosition.CenterParent;
            frm.ShowDialog();
        }

        private void btndisconnect_Click(object sender, EventArgs e)
        {
            Reconnect(3, null);
        }

        private void CountTagMenuItem_Click(object sender, EventArgs e)
        {
            CountTagsFrm frm = new CountTagsFrm(modulerdr, rParms);
            frm.StartPosition = FormStartPosition.CenterParent;
            frm.ShowDialog();
        }

        private void MulperlockMenuItem_Click(object sender, EventArgs e)
        {
            MulperlockFrm frm = new MulperlockFrm(modulerdr, rParms);
            frm.StartPosition = FormStartPosition.CenterParent;
            frm.ShowDialog();
        }


        private void menuabout_Click(object sender, EventArgs e)
        {
            AboutFrm frm = new AboutFrm();
            frm.ShowDialog();
        }

        private void menutest_Click(object sender, EventArgs e)
        {
            regulatoryFrm frm = new regulatoryFrm(modulerdr);
            frm.ShowDialog();
        }

        private void menuitemlog_Click(object sender, EventArgs e)
        {
            logFrm frm = new logFrm();
            frm.ShowDialog();
        }

        private void lvTags_ColumnClick(object sender, ColumnClickEventArgs e)
        {
            if (!btnstart.Enabled)
                return;
            if (e.Column >= 1 && e.Column <= 8)
            {
                tmplist.Clear();
                foreach (ListViewItem viewitem in lvTags.Items)
                {
                    TagProtocol potl = TagProtocol.NONE;
                    if (viewitem.SubItems[5].Text == "GEN2")
                        potl = TagProtocol.GEN2;
                    else if (viewitem.SubItems[5].Text == "ISO180006B")
                        potl = TagProtocol.ISO180006B;
                    else if (viewitem.SubItems[5].Text == "IPX256")
                        potl = TagProtocol.IPX256;
                    else if (viewitem.SubItems[5].Text == "IPX64")
                        potl = TagProtocol.IPX64;

                    TagInfo newtag = new TagInfo(viewitem.SubItems[2].Text, int.Parse(viewitem.SubItems[1].Text),
                        int.Parse(viewitem.SubItems[3].Text), DateTime.Now, potl, viewitem.SubItems[4].Text);
                    newtag.RssiRaw = int.Parse(viewitem.SubItems[6].Text);
                    newtag.Frequency = int.Parse(viewitem.SubItems[7].Text);
                    newtag.Phase = int.Parse(viewitem.SubItems[8].Text);
                    tmplist.Add(newtag);
                }
                this.lvTags.Items.Clear();
                IComparer<TagInfo> tagcmper = null;
                if (e.Column == 1)
                    tagcmper = new TagInfoCompReadCnt();
                else if (e.Column == 2)
                    tagcmper = new TagInfoCompEPCId();
                else if (e.Column == 3)
                    tagcmper = new TagInfoCompAntId();
                else if (e.Column == 4)
                    tagcmper = new TagInfoCompEmdData();
                else if (e.Column == 5)
                    tagcmper = new TagInfoCompPotl();
                else if (e.Column == 6)
                    tagcmper = new TagInfoCompRssi();
                else if (e.Column == 7)
                    tagcmper = new TagInfoCompFreq();
                else if (e.Column == 8)
                    tagcmper = new TagInfoCompPhase();

                tmplist.Sort(tagcmper);
                foreach (TagInfo tag in tmplist)
                {
                    ListViewItem item = new ListViewItem(lvTags.Items.Count.ToString());
                    item.SubItems.Add(tag.readcnt.ToString());
                    item.SubItems.Add(tag.epcid);
                    item.SubItems.Add(tag.antid.ToString());

                    item.SubItems.Add(tag.emddatastr);

                    if (tag.potl == TagProtocol.GEN2)
                        item.SubItems.Add("GEN2");
                    else if (tag.potl == TagProtocol.ISO180006B)
                        item.SubItems.Add("ISO180006B");
                    else if (tag.potl == TagProtocol.IPX256)
                        item.SubItems.Add("IPX256");
                    else if (tag.potl == TagProtocol.IPX64)
                        item.SubItems.Add("IPX64");
                    item.SubItems.Add(tag.RssiRaw.ToString());
                    item.SubItems.Add(tag.Frequency.ToString());
                    item.SubItems.Add(tag.Phase.ToString());
                    lvTags.Items.Add(item);
                }
            }
        }

        /// <summary>
        /// 导出盘点标签数据文件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void menuoutputtags_Click(object sender, EventArgs e)
        {
            string filename = null;
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.Filter = "txt(*.txt)| *.txt|csv(*.csv)|*.csv";
            if (sfd.ShowDialog() == DialogResult.OK)
            {
                filename = sfd.FileName;
                FileInfo fileInfo = new FileInfo(filename);
                fileInfo.Delete();
                StreamWriter streamWriter = fileInfo.CreateText();
                //    streamWriter.WriteLine("格式：epc，读次数，天线，附加数据，协议,RSSI");
                foreach (ListViewItem viewitem in lvTags.Items)
                {
                    string wline = viewitem.SubItems[2].Text + "," + viewitem.SubItems[1].Text + "," +
                        viewitem.SubItems[3].Text + "," + viewitem.SubItems[4].Text + "," +
                        viewitem.SubItems[5].Text + "," + viewitem.SubItems[6].Text + "," +
                        viewitem.SubItems[7].Text + "," + viewitem.SubItems[8].Text;
                    streamWriter.WriteLine(wline);
                }
                streamWriter.Flush();
                streamWriter.Close();
            }

        }

        private void MsgDebugMenu_Click(object sender, EventArgs e)
        {
            FrmMsgDebug frm = new FrmMsgDebug(this);
            frm.ShowDialog();
        }

        private void menuitemmultibankwrite_Click(object sender, EventArgs e)
        {
            MultiBankWriteFrm frm = new MultiBankWriteFrm(modulerdr);
            frm.ShowDialog();
        }

        private void pSAMToolStripMenuItem_Click(object sender, EventArgs e)
        {
            FrmPsam frm = new FrmPsam(modulerdr);
            frm.ShowDialog();
        }

        private void btn16antset_Click(object sender, EventArgs e)
        {
            Frm16AntSet frm = new Frm16AntSet(this);
            frm.ShowDialog();

        }

        private void lvTags_MouseClick_1(object sender, MouseEventArgs e)
        {

        }

        private void toolStripMenuItem1_Click(object sender, EventArgs e)
        {
            string AllEpcs = "";
            foreach (ListViewItem vi in lvTags.Items)
            {
                AllEpcs += vi.SubItems[2].Text + "\r\n";
            }
            if (AllEpcs != string.Empty)
                Clipboard.SetDataObject(AllEpcs.Substring(0, AllEpcs.Length - 2), true);
        }

        private void toolStripMenuItem2_Click(object sender, EventArgs e)
        {
            if (lvTags.SelectedItems.Count != 0)
            {
                string AllEpcs = "";
                foreach (ListViewItem vi in lvTags.SelectedItems)
                {
                    AllEpcs += vi.SubItems[2].Text + "\r\n";
                }
                if (AllEpcs != string.Empty)
                    Clipboard.SetDataObject(AllEpcs.Substring(0, AllEpcs.Length - 2), true);
            }
        }

        /*
        void restrf()
        {
            int cnt = 0;
            BackReadOption bro = new BackReadOption();
            bro.IsFastRead = true;

            bro.ReadDuration = (ushort)50;
            bro.ReadInterval = (uint)100;
            bro.FRTMetadata = new BackReadOption.FastReadTagMetaData();
            bro.FRTMetadata.IsAntennaID = true;
            bro.FRTMetadata.IsEmdData = false;
            bro.FRTMetadata.IsFrequency = false;
            bro.FRTMetadata.IsReadCnt = true;
            bro.FRTMetadata.IsRFU = false;
            bro.FRTMetadata.IsRSSI = false;
            bro.FRTMetadata.IsTimestamp = false;

            modulerdr.ParamSet("BackReadOption", bro);
            modulerdr.ParamSet("BackReadGPITrigger", null);
            modulerdr.ParamSet("ReadPlan",
                new SimpleReadPlan(TagProtocol.GEN2, new int[] { 1, 2 }));

            while (true)
            {
                modulerdr.StartReading();
                Thread.Sleep(500);
                modulerdr.StopReading();

                modulerdr.ParamSet("ResetRfidModule", (byte)0);
                Thread.Sleep(200);
                
                cnt++;
                Debug.WriteLine("ResetRfidModule " + cnt);
                if (cnt == 1000)
                    break;
            }

        }
        private void button2_Click(object sender, EventArgs e)
        {
            Thread th = new Thread(restrf);
            th.Start();
        }*/
    }

    public class AntAndBoll
    {
        public AntAndBoll(int ant, bool conn)
        {
            antid = ant;
            isConn = conn;
        }

        public int antid;
        public bool isConn;
        public UInt16 rpower;
        public UInt16 wpower;
    }

    public class AsyncTagMeta
    {
        public ushort ToFlags()
        {
            ushort ret = 0;
            if (IsReadCnt)
                ret |= 0x1;
            if (IsRSSI)
                ret |= 0x1 << 1;
            if (IsAntID)
                ret |= 0x1 << 2;
            if (IsFrequency)
                ret |= 0x1 << 3;
            if (IsTM)
                ret |= 0x1 << 4;
            if (IsRFU)
                ret |= 0x1 << 5;
            if (IsEmdData)
                ret |= 0x1 << 7;
            return ret;
        }
        public void Reset()
        {
            IsReadCnt = false;
            IsRSSI = false;
            IsAntID = true;
            IsFrequency = false;
            IsTM = false;
            IsRFU = false;
            IsEmdData = false;
        }
        public bool IsReadCnt { get; set; }
        public bool IsRSSI { get; set; }
        public bool IsAntID { get; set; }
        public bool IsFrequency { get; set; }
        public bool IsTM { get; set; }
        public bool IsRFU { get; set; }
        public bool IsEmdData { get; set; }
    }
    public class ReaderParams
    {
        public ReaderParams(int rdur, int sdur, int sess)
        {
            readdur = rdur;
            sleepdur = sdur;
            gen2session = sess;
            isIpModify = false;
            isM5eModify = false;
            fisrtLoad = true;

            ip = "";
            subnet = "";
            gateway = "";
            macstr = "";
            hasIP = false;
            isGetIp = false;
            Gen2Qval = -2;
            isCheckConnection = false;
            isMultiPotl = false;
            antcnt = -1;
            weightgen2 = 30;
            weight180006b = 30;
            weightipx64 = 30;
            weightipx256 = 30;

            isFastRead = false;
            isIdtAnts = false;
            IdtAntsType = 0;
            DurIdtval = 0;
            AfterIdtWaitval = 0;

            //           FixReadCount = 0;
            //           isReadFixCount = false;
            //           isOneReadOneTime = false;

            usecase_ishighspeedblf = false;
            usecase_tagcnt = -1;
            usecase_readperform = -1;
            usecase_antcnt = -1;
            FRTMeta = new BackReadOption.FastReadTagMetaData();

            FRTMeta.IsReadCnt = false;
            FRTMeta.IsRSSI = false;
            FRTMeta.IsAntennaID = true;
            FRTMeta.IsFrequency = false;
            FRTMeta.IsTimestamp = false;
            FRTMeta.IsRFU = false;
            FRTMeta.IsEmdData = false;
            GpiTriiger = null;

            reconnectcnt = 1;
            connectinterval = 5;
        }

        public void resetParams()
        {
            isIpModify = false;
            isM5eModify = false;
            fisrtLoad = true;

            ip = "";
            subnet = "";
            gateway = "";
            macstr = "";
            hasIP = false;
            isGetIp = false;
            Gen2Qval = -2;
            isCheckConnection = false;
            isMultiPotl = false;
            antcnt = -1;
            weightgen2 = 30;
            weight180006b = 30;
            weightipx64 = 30;
            weightipx256 = 30;

            isChangeColor = true;
            isUniByEmd = false;
            isUniByAnt = false;

            isFastRead = false;
            isIdtAnts = false;
            IdtAntsType = 0;
            DurIdtval = 0;
            AfterIdtWaitval = 0;

            FixReadCount = 0;
            isReadFixCount = false;
            isOneReadOneTime = false;

            usecase_ishighspeedblf = false;
            usecase_tagcnt = -1;
            usecase_readperform = -1;
            usecase_antcnt = -1;

            FRTMeta.IsReadCnt = false;
            FRTMeta.IsRSSI = false;
            FRTMeta.IsAntennaID = true;
            FRTMeta.IsFrequency = false;
            FRTMeta.IsTimestamp = false;
            FRTMeta.IsRFU = false;
            FRTMeta.IsEmdData = false;

            GpiTriiger = null;
            reconnectcnt = 1;
            connectinterval = 5;
        }
        public BackReadOption.FastReadTagMetaData FRTMeta;//快速模式 后台盘点返回标签项标志位
        public GPITrigger GpiTriiger;//GPI 触发器
        public bool setGPO1;//是否设置gpo1
        public int gen2session;//gen2 协议 session项
        public int readdur; //盘点时长
        public int sleepdur;//休眠时间
        public int antcnt;//天线个数
        public string hardvir;//硬件版本
        public string softvir;//软件版本
        public ReaderType readertype;//读写器类型

        public List<AntAndBoll> AntsState = new List<AntAndBoll>();//天线状态
        public int ModuleReadervir;
        public string ip;//ip 地址
        public string subnet;//子网
        public string gateway;//网关
        public string macstr;//掩码
        public bool isGetIp;//是否获取ip
        public bool isIpModify;//ip是否更改
        public bool isM5eModify;//是否m5e更改
        public bool fisrtLoad;//首次加载
        public bool hasIP;//拥有ip地址
        public int powermin;//最大功率
        public int powermax;//最小功率
        public int Gen2Qval;//gen2 协议 q值
        public bool isFastRead;//是否快速模式
        public bool isCheckConnection;//是否检查连接
        public bool isMultiPotl;//是否多协议
        public SimpleReadPlan SixteenDevsrp = null;//简单盘点方式
        //        public bool isRevertAnts;

        public int weightgen2;//gen2 权重
        public int weight180006b;//6b 权重
        public int weightipx64;//ipx64 权重
        public int weightipx256;//ipx256 权重

        public bool isChangeColor;//是否更改颜色
        public bool isUniByEmd;//是否附加数据唯一
        public bool isUniByAnt;//是否天线号唯一

        public bool isIdtAnts;//是否天线识别
        public int IdtAntsType;//识别天线类型
        public int DurIdtval;//判决时间
        public int AfterIdtWaitval;//等待时间

        public int FixReadCount;
        public bool isReadFixCount;
        public bool isOneReadOneTime;

        public bool usecase_ishighspeedblf;
        public int usecase_tagcnt;
        public int usecase_readperform;
        public int usecase_antcnt;

        public int reconnectcnt;//重连次数
        public int connectinterval;//重连间隔
    }

    public class TagInfoCompEPCId : IComparer<TagInfo>
    {
        public int Compare(TagInfo x, TagInfo y)
        {
            return x.epcid.CompareTo(y.epcid);
        }
    }

    public class TagInfoCompReadCnt : IComparer<TagInfo>
    {
        public int Compare(TagInfo x, TagInfo y)
        {
            return x.readcnt.CompareTo(y.readcnt);
        }
    }

    public class TagInfoCompPotl : IComparer<TagInfo>
    {
        public int Compare(TagInfo x, TagInfo y)
        {
            return x.potl.CompareTo(y.potl);
        }
    }

    public class TagInfoCompFreq : IComparer<TagInfo>
    {
        public int Compare(TagInfo x, TagInfo y)
        {
            return x.Frequency.CompareTo(y.Frequency);
        }
    }

    public class TagInfoCompPhase : IComparer<TagInfo>
    {
        public int Compare(TagInfo x, TagInfo y)
        {
            return x.Phase.CompareTo(y.Phase);
        }
    }

    public class TagInfoCompRssi : IComparer<TagInfo>
    {
        public int Compare(TagInfo x, TagInfo y)
        {
            return x.RssiRaw.CompareTo(y.RssiRaw);
        }
    }

    public class TagInfoCompEmdData : IComparer<TagInfo>
    {
        public int Compare(TagInfo x, TagInfo y)
        {
            return x.emddatastr.CompareTo(y.emddatastr);
        }
    }

    public class TagInfoCompAntId : IComparer<TagInfo>
    {
        public int Compare(TagInfo x, TagInfo y)
        {
            return x.antid.CompareTo(y.antid);
        }
    }

    public class TagInfo
    {
        public TagInfo(string epc, int rcnt, int ant, DateTime time, TagProtocol potl_, string emdstr)
        {
            epcid = epc;
            readcnt = rcnt;
            antid = ant;
            timestamp = time;
            potl = potl_;
            emddatastr = emdstr;
            RssiSum = 0;
        }
        public string epcid;
        public int readcnt;
        public int antid;
        public TagProtocol potl;
        public DateTime timestamp;
        public string emddatastr;
        public int RssiSum;
        public int RssiRaw;
        public int Frequency;
        public int Phase;
    }

    class DoubleBufferListView : ListView
    {
        public DoubleBufferListView()
        {
            SetStyle(ControlStyles.DoubleBuffer | ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);
            UpdateStyles();
        }
    }

    //日志类
    public class ProgramLog
    {
        int logdayscnt;//日志文件保持数据的天数限制
        string preffixname = null;//日志文件的固定前缀名 

        DateTime lastupdate = DateTime.Now.Date; //上一次创建新日志文件的时间
        StreamWriter curlogfile = null;//当前的日志文件

        public ProgramLog(int daycnts, string prefname)
        {
            logdayscnt = daycnts;
            preffixname = prefname;
            DelLogFile();
            curlogfile = File.CreateText(preffixname + "_" +
                DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss"));
        }
        //获取备份日志文件名字
        public string GetOldLog()
        {
            DirectoryInfo dir = null;
            dir = new DirectoryInfo(Environment.CurrentDirectory);
            FileInfo[] fls = dir.GetFiles();
            List<DateLogFile> logfiles = new List<DateLogFile>();
            foreach (FileInfo fl in fls)
            {
                if (fl.Name.StartsWith(preffixname))
                {
                    logfiles.Add(new DateLogFile(fl));
                }
            }
            return logfiles[0].logfi.Name;
        }
        //获取当前日志文件名 
        public string GetCurLog()
        {
            DirectoryInfo dir = null;
            dir = new DirectoryInfo(Environment.CurrentDirectory);
            FileInfo[] fls = dir.GetFiles();
            List<DateLogFile> logfiles = new List<DateLogFile>();
            foreach (FileInfo fl in fls)
            {
                if (fl.Name.StartsWith(preffixname))
                {
                    logfiles.Add(new DateLogFile(fl));
                }
            }
            return logfiles[logfiles.Count - 1].logfi.Name;
        }

        //删除多余日志，只保持日志文件不多于两个，且是最近创建的两个 
        private void DelLogFile()
        {
            DirectoryInfo dir = null;
            try
            {
                //根据文件名前缀，找出所有日志文件 
                dir = new DirectoryInfo(Environment.CurrentDirectory);
                FileInfo[] fls = dir.GetFiles();
                List<DateLogFile> logfiles = new List<DateLogFile>();
                foreach (FileInfo fl in fls)
                {
                    if (fl.Name.StartsWith(preffixname))
                    {
                        logfiles.Add(new DateLogFile(fl));
                    }
                }
                //根据日志文件创建的时间排序
                logfiles.Sort();
                //删除多余日子文件
                int delcnt = 0;
                if (logfiles.Count > 1)
                {
                    delcnt = logfiles.Count - 1;
                    for (int i = 0; i < delcnt; ++i)
                    {
                        logfiles[i].logfi.Delete();
                    }
                }
            }
            catch
            {
            }
            finally
            {
            }
        }
        //日志写入函数
        public void WriteLine(string line)
        {
            //首先判断是否需要建立新的日志文件，对某类日志文件都会规定一个期限，比如只存储三天的数据。一旦
            //当前日期大于文件创建日期三，则应该关闭当前日志文件，建立一个新的日志文件
            if (DateTime.Now.Date.Subtract(lastupdate).Days > logdayscnt)
            {
                //关闭当前日志文件
                curlogfile.Dispose();
                //删除多余日志文件
                DelLogFile();
                //创建新日志文件，命名规则为：固定前缀+'_'+日期字符串
                curlogfile = File.CreateText(preffixname + "_" +
                    DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss"));
                //更新前一次创建文件时间
                lastupdate = DateTime.Now.Date;
            }
            else //写入日志,一次写一行
            {
                curlogfile.WriteLine(DateTime.Now.ToString() + "--" + line);
                curlogfile.Flush();
            }

        }
        //内部类，用于日志文件按照创建日期进行排序
        class DateLogFile : IComparable<DateLogFile>
        {
            public DateLogFile(FileInfo fi)
            {
                logfi = fi;
            }
            public int CompareTo(DateLogFile other)
            {
                return logfi.CreationTime.CompareTo(other.logfi.CreationTime);
            }

            public FileInfo logfi;
        }
    }

    class ReaderExceptionChecker
    {
        public ReaderExceptionChecker(int maxerrcnt, int dursec)
        {
            dts = new DateTime[maxerrcnt];
            index = 0;
            maxdursec = dursec;
        }
        public void AddErr()
        {
            dts[index++] = DateTime.Now;
        }

        DateTime[] dts;
        int maxdursec;
        int index;
        public bool IsTrigger()
        {
            DateTime now = DateTime.Now;
            if (index == dts.Length - 1)
            {
                if (now.Subtract(dts[0]).TotalSeconds < maxdursec)
                {
                    return true;
                }
                else
                {
                    index = 0;
                    return false;
                }
            }
            else
                return false;
        }
    }
}