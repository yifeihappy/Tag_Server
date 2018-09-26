using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using ModuleTech;
using System.Diagnostics;
using ModuleTech.Gen2;
using ModuleLibrary;
//using System.Linq;

namespace ModuleReaderManager
{
    public partial class Frm16AntSet : Form
    {
        public Frm16AntSet(Form1 frm)
        {
            InitializeComponent();
            Mfrm = frm;
        }
        Form1 Mfrm = null;

        private void Frm16AntSet_Load(object sender, EventArgs e)
        {
            if (!(Mfrm.modulerdr.HwDetails.board == Reader.MaindBoard_Type.MAINBOARD_ARM9 ||
                Mfrm.modulerdr.HwDetails.board == Reader.MaindBoard_Type.MAINBOARD_ARM9_WIFI))
                grouppanel.Enabled = false;

            if (Mfrm.readerantnumber == 8)
            {
                foreach (Control ctl in gbinvants.Controls)
                {
                    if (ctl.Name.StartsWith("cbant"))
                    {
                        int antid = int.Parse(ctl.Name.Substring(5, ctl.Name.Length - 5));
                        if (antid > 8)
                            ctl.Enabled = false;
                    }
                }

                foreach (Control ctl in gbrpwrs.Controls)
                {
                    if (!ctl.Name.StartsWith("tb"))
                        continue;
                    byte antid = byte.Parse(ctl.Name.Substring(6, ctl.Name.Length - 6));
                    if (antid > 8)
                        ctl.Enabled = false;
                }
                foreach (Control ctl in gbwpwrs.Controls)
                {
                    if (!ctl.Name.StartsWith("tb"))
                        continue;
                    byte antid = byte.Parse(ctl.Name.Substring(6, ctl.Name.Length - 6));
                    if (antid > 8)
                        ctl.Enabled = false;
                }
            }

            foreach (AntAndBoll ab in Mfrm.invants16setting)
            {
                ((CheckBox)gbinvants.Controls["cbant" + ab.antid]).Checked = true;
            }

            int opstep = 0;
            try
            {
                int[] connectedants = (int[])Mfrm.modulerdr.ParamGet("ConnectedAntennas");
                for (int c = 0; c < connectedants.Length; ++c)
                    gbinvants.Controls["cbant" + connectedants[c]].ForeColor = Color.Green;

                opstep = 1;
                maxp = (int)Mfrm.modulerdr.ParamGet("RfPowerMax");
                minp = (int)Mfrm.modulerdr.ParamGet("RfPowerMin");
                AntPower[] pwrs = (AntPower[])Mfrm.modulerdr.ParamGet("AntPowerConf");
                foreach (AntPower pwr in pwrs)
                {
                    gbrpwrs.Controls["tbrpwr" + pwr.AntId].Text = pwr.ReadPower.ToString();
                    gbwpwrs.Controls["tbwpwr" + pwr.AntId].Text = pwr.WritePower.ToString();
                }
            }
            catch (System.Exception ex)
            {
                if (opstep == 0)
                    MessageBox.Show("获取天线状态失败:" + ex.ToString());
                else
                    MessageBox.Show("获取天线功率失败:" + ex.ToString());
                return;
            }

            if (Mfrm.modulerdr.HwDetails.board == Reader.MaindBoard_Type.MAINBOARD_ARM9 ||
                Mfrm.modulerdr.HwDetails.board == Reader.MaindBoard_Type.MAINBOARD_ARM9_WIFI)
            {
                int sop = (int)Mfrm.modulerdr.ParamGet("MaxEPCLength");
                if ((sop & 0x8000) == 0x8000)
                    cbisdebug.Checked = true;
                if (((sop >> 9) & 0x03) == 1)
                    cbiseverevert.Checked = true;
            }

            this.cbrevert.Checked = Mfrm.is16AntRevert;
        }

        private void cballsel_CheckedChanged(object sender, EventArgs e)
        {
            if (cballsel.Checked)
            {
                foreach (Control ctl in gbinvants.Controls)
                {
                    if (ctl.Name.StartsWith("cbant"))
                    {
                        CheckBox cbox = (CheckBox)ctl;
                        if (ctl.Enabled)
                            cbox.Checked = true;
                    }
                }
            }
            else
            {
                foreach (Control ctl in gbinvants.Controls)
                {
                    if (ctl.Name.StartsWith("cbant"))
                    {
                        CheckBox cbox = (CheckBox)ctl;
                        if (ctl.Enabled)
                            cbox.Checked = false;
                    }
                }
            }
        }
        int maxp;
        int minp;
        private void btnSetAllSame_Click(object sender, EventArgs e)
        {
            int sameval = 0;
            try
            {
                sameval = int.Parse(tbsameval.Text.Trim());
            }
            catch
            {
                MessageBox.Show("请输入合法的功率值，有效值为" + minp.ToString() + "-" + maxp.ToString());
                return;
            }
            if (sameval < minp || sameval > maxp)
            {
                MessageBox.Show("请输入合法的功率值，有效值为" + minp.ToString() + "-" + maxp.ToString());
                return;
            }
            foreach (Control ctl in gbrpwrs.Controls)
            {
                if (ctl.Name.StartsWith("tbrpwr") && ctl.Enabled)
                    ctl.Text = sameval.ToString();
            }
            foreach (Control ctl in gbwpwrs.Controls)
            {
                if (ctl.Name.StartsWith("tbwpwr") && ctl.Enabled)
                    ctl.Text = sameval.ToString();
            }
            btnsetpwrs_Click(null, null);
        }

        private void btnsetpwrs_Click(object sender, EventArgs e)
        {
            Dictionary<byte, AntPower> dicpwrs = new Dictionary<byte, AntPower>(); 
            foreach (Control ctl in gbrpwrs.Controls)
            {
                if (!ctl.Name.StartsWith("tb"))
                    continue;
                if (!ctl.Enabled)
                    continue;
                int pval = 0;
                byte antid = byte.Parse(ctl.Name.Substring(6, ctl.Name.Length - 6));
                try
                {
                    pval = int.Parse(ctl.Text.Trim());
                }
                catch
                {
                    MessageBox.Show("天线" + antid + "读功率值非法，合法值为" + minp.ToString() + "-" + maxp.ToString());
                    return;
                }
                if (pval < minp || pval > maxp)
                {
                    MessageBox.Show("天线" + antid + "读功率值非法，合法值为" + minp.ToString() + "-" + maxp.ToString());
                    return;
                }

                AntPower tmppwr = new AntPower();
                tmppwr.AntId = antid;
                tmppwr.ReadPower = ushort.Parse(ctl.Text.Trim());
                dicpwrs.Add(antid, tmppwr);
            }
            foreach (Control ctl in gbwpwrs.Controls)
            {
                if (!ctl.Name.StartsWith("tb"))
                    continue;
                if (!ctl.Enabled)
                    continue;
                int pval = 0;
                byte antid = byte.Parse(ctl.Name.Substring(6, ctl.Name.Length - 6));
                try
                {
                    pval = int.Parse(ctl.Text.Trim());
                }
                catch
                {
                    MessageBox.Show("天线" + antid + "写功率值非法，合法值为" + minp.ToString() + "-" + maxp.ToString());
                    return;
                }
                if (pval < minp || pval > maxp)
                {
                    MessageBox.Show("天线" + antid + "写功率值非法，合法值为" + minp.ToString() + "-" + maxp.ToString());
                    return;
                }
                AntPower tmpwpwr = dicpwrs[antid];
                tmpwpwr.WritePower = ushort.Parse(ctl.Text.Trim());
                dicpwrs[antid] = tmpwpwr;
            }

            try
            {
                AntPower[] topwrlist = new AntPower[dicpwrs.Values.Count];
                int pwrcnt = 0;
                foreach (AntPower ap in dicpwrs.Values)
                {
                    topwrlist[pwrcnt++] = ap;
                }
                Mfrm.modulerdr.ParamSet("AntPowerConf", topwrlist);
                MessageBox.Show("设置成功");
            }
            catch (Exception ex)
            {
                MessageBox.Show("设置功率失败:" + ex.ToString());
                return;
            }
        }

        public class AntAndBollComper1 : IComparer<AntAndBoll>
        {
            public int Compare(AntAndBoll x, AntAndBoll y)
            {
                return x.antid.CompareTo(y.antid);
            }
        }

        public class AntAndBollComper2 : IComparer<AntAndBoll>
        {
            public int Compare(AntAndBoll x, AntAndBoll y)
            {
                return y.antid.CompareTo(x.antid);
            }
        }

        private void btnsetinvants_Click(object sender, EventArgs e)
        {
            Mfrm.invants16setting.Clear();
            foreach (Control ctl in gbinvants.Controls)
            {
                if (ctl.Name.StartsWith("cbant"))
                {
                    CheckBox cbox = (CheckBox)ctl;
                    if (cbox.Checked)
                    {
                        Mfrm.invants16setting.Add(new AntAndBoll(
                            int.Parse(ctl.Name.Substring(5, ctl.Name.Length - 5)),
                            (ctl.ForeColor == Color.Green)));
                    }
                }
            }

            if (Mfrm.invants16setting.Count == 0)
            {
                MessageBox.Show("请选择盘存使用的天线");
                return;
            }
            else
            {
                IComparer<AntAndBoll> comper = null;
                if (cbrevert.Checked)
                {
                    comper = new AntAndBollComper2();
                    Mfrm.is16AntRevert = true;
                }
                else
                {
                    comper = new AntAndBollComper1();
                    Mfrm.is16AntRevert = false;
                }
                Mfrm.invants16setting.Sort(comper);
                
                MessageBox.Show("设置成功");
            }
        }

        private void btnadvset_Click(object sender, EventArgs e)
        {
            int option = 0;
            if (cbisdebug.Checked)
            {
                option = 0x8000;
                option |= (3 << 11);
            }
            if (cbiseverevert.Checked)
                option |= (1 << 9);
            try
            {
                Mfrm.modulerdr.ParamSet("MaxEPCLength", option);
                MessageBox.Show("设置成功");
            }
            catch (System.Exception ex)
            {
                MessageBox.Show("设置失败:"+ex.ToString());
            }
            
        }

        private void button1_Click(object sender, EventArgs e)
        {
            List<int> anttab = new List<int>();
            anttab.Add(1);
            anttab.Add(915250);
            anttab.Add(916250);
            anttab.Add(917250);
            try
            {
                Mfrm.modulerdr.ParamSet("SingleAntHopTable", anttab.ToArray());
                MessageBox.Show("设置成功");
            }
            catch (System.Exception ex)
            {
                MessageBox.Show("设置失败:" + ex.ToString());
            }
        }
    }
}
