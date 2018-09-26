using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using ModuleTech;
using ModuleTech.Gen2;
using ModuleLibrary;
using System.Threading;

namespace ModuleReaderManager
{
    public partial class regulatoryFrm : Form
    {
        Reader modrdr = null;
        public regulatoryFrm(Reader rdr)
        {
            modrdr = rdr;
            InitializeComponent();
        }

        private void btnsetopfre_Click(object sender, EventArgs e)
        {
            if (this.tbopfre.Text.Trim() == string.Empty)
            {
                MessageBox.Show("请输入频点");
                return;
            }
            try
            {
                modrdr.ParamSet("setOperatingFrequency", uint.Parse(this.tbopfre.Text.Trim()));
            }
            catch
            {
                MessageBox.Show("设置失败");
            }
        }

        private void btntransCW_Click(object sender, EventArgs e)
        {
            if (this.btnsetopant.Enabled)
            {
                MessageBox.Show("请先输入天线");
                return;
            }
            try
            {
                modrdr.ParamSet("transmitCWSignal", 1);
                this.btnstopCW.Enabled = true;
                this.btntransCW.Enabled = false;
            }
            catch
            {
                MessageBox.Show("发射失败");
            }
        }

        private void btnstopCW_Click(object sender, EventArgs e)
        {
            try
            {
                modrdr.ParamSet("transmitCWSignal", 0);
                this.btntransCW.Enabled = true;
                this.btnstopCW.Enabled = false;
            }
            catch
            {
                MessageBox.Show("停止失败");
            }
        }

        private void btnPRBSOn_Click(object sender, EventArgs e)
        {
            if (this.btnsetopant.Enabled)
            {
                MessageBox.Show("请先输入天线");
                return;
            }
            if (this.tbdur.Text.Trim() == string.Empty)
            {
                MessageBox.Show("请输入时长");
                return;
            }
            int dur = int.Parse(this.tbdur.Text.Trim());
            if (dur > 65535)
            {
                MessageBox.Show("时长必须小于65535");
                return;
            }
            ushort usdur = (ushort)dur;
            int aa = Environment.TickCount;
            try
            {
                this.btnPRBSOn.Enabled = false;
                modrdr.ParamSet("turnPRBSOn", usdur);
            }
            catch (Exception exx)
            {
                MessageBox.Show("发射失败"+exx.ToString());
            }
 //           int bb = (dur - (Environment.TickCount - aa));
 //           if (bb > 0)
  //              Thread.Sleep(bb+1500);
            this.btnPRBSOn.Enabled = true;
        }

        private void regulatoryFrm_Load(object sender, EventArgs e)
        {
            this.btnstopCW.Enabled = false;
            this.btntransCW.Enabled = true;
            
        }

        private void btnsetopant_Click(object sender, EventArgs e)
        {
            if (this.tbopant.Text.Trim() == string.Empty)
            {
                MessageBox.Show("请输入天线");
                return;
            }
            int ant = int.Parse(this.tbopant.Text);
            modrdr.ParamSet("setRegulatoryOpAnt", ant);
            this.tbopant.Enabled = false;
            this.btnsetopant.Enabled = false;
        }
        public class Fre_Vswr
        {
            public int Fre { get; set; }
            public float Vswr { get; set; }
        }
        public class Fre_VswrComper : IComparer<Fre_Vswr>
        {
            public int Compare(Fre_Vswr x, Fre_Vswr y)
            {
                return x.Fre.CompareTo(y.Fre);
            }
        }
        private void btntestvswr_Click(object sender, EventArgs e)
        {
            VswrQueryParam vqp = new VswrQueryParam(); 
            try 
            {
                vqp.AntId = byte.Parse(tbvswrantid.Text.Trim());
            }
            catch
            {
                MessageBox.Show("请输入合法的天线id");
                return;
            }
            try
            {
                vqp.Power = ushort.Parse(tbvswrpwr.Text.Trim());
            }
            catch
            {
                MessageBox.Show("请输入合法的输出功率");
                return;
            }
            
            vqp.Rg = ModuleTech.Region.NA;
            modrdr.ParamSet("AntPowerVswr", vqp);
            Dictionary<int, int> antvs = null;
            try
            {
                antvs = (Dictionary<int, int>)modrdr.ParamGet("AntPowerVswr");
            }
            catch (Exception ex)
            {
                MessageBox.Show("测试失败:" + ex.ToString());
                return;
            }
            string vswrstr = "";
            this.rtbVswrInfo.Text = "";
            List<Fre_Vswr> vswrlist = new List<Fre_Vswr>();
            foreach (int fre in antvs.Keys)
            {
                Fre_Vswr tmp = new Fre_Vswr();
                tmp.Fre = fre;
                float rl = (float)Math.Pow((double)10, (double)(((float)antvs[fre] / (float)10) / (float)20));
                tmp.Vswr = (1 + rl) / (rl - 1);
                vswrlist.Add(tmp);
            }
            vswrlist.Sort(new Fre_VswrComper());
            foreach (Fre_Vswr fv in vswrlist)
            {
                vswrstr += fv.Fre.ToString() + ":" + fv.Vswr.ToString("#.00") +" ";
            }
            this.rtbVswrInfo.Text = vswrstr;
        }
    }
    
}
