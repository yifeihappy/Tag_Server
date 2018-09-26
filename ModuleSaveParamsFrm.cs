using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

using ModuleTech;
using ModuleTech.Gen2;
using ModuleTech.ISO180006b;
using ModuleLibrary;
using System.Diagnostics;

namespace ModuleReaderManager
{
    public partial class ModuleSaveParamsFrm : Form
    {
        public ModuleSaveParamsFrm(ReaderParams paras, Reader rd)
        {
            InitializeComponent();
            m_params = paras;
            rdr = rd;
        }
        ReaderParams m_params;
        Reader rdr;

        private void getallparams()
        {
            button_mpant1get.PerformClick();
            button_mpantgpoget.PerformClick();
            if (rdr.HwDetails.module != Reader.Module_Type.MODOULE_SLR1100)
            {
                button_mpautoget.PerformClick();
                button_mpbaudget.PerformClick();
                groupBox2.Visible = true;
                groupBox18.Visible = false;
                btndetantsget.PerformClick();
            }
            else
            {
                groupBox4.Enabled = false;
                button_readytoupdate.Enabled = false;
                groupBox3.Enabled = false;
                groupBox2.Visible = false;
                groupBox18.Visible = true;
                button_getantpw.PerformClick();
            }
            btngetrg.PerformClick();
            button_mpfreget.PerformClick();
            button_mpfretmget.PerformClick();
            btngetdataantunique.PerformClick();
            btngetdataemdunique.PerformClick();
            btngetdatarechrssi.PerformClick();
            btngetgen2encode.PerformClick();
            btngetgen2target.PerformClick();
            btngetinvmode.PerformClick();
            btngetmel.PerformClick();
          
            btnge2sessget.PerformClick();
            btngen2qget.PerformClick();
        }
        private void ModuleSaveParamsFrm_Load(object sender, EventArgs e)
        {
            getallparams();
        }
        public void Sort(ref uint[] array)
        {
            uint tmpIntValue = 0;
            for (int xIndex = 0; xIndex < array.Length; xIndex++)
            {
                for (int yIndex = 0; yIndex < array.Length; yIndex++)
                {
                    if (array[xIndex] < array[yIndex])
                    {
                        tmpIntValue = array[xIndex];
                        array[xIndex] = array[yIndex];
                        array[yIndex] = tmpIntValue;
                    }
                }
            }
        }
        private void allcheckBox_CheckedChanged(object sender, EventArgs e)
        {
            if (allcheckBox.Checked)
            {

                for (int i = 0; i < lvhoptb.Items.Count; i++)
                {
                    lvhoptb.Items[i].Checked = true;
                }

            }
            else
            {
                for (int i = 0; i < lvhoptb.Items.Count; i++)
                {
                    lvhoptb.Items[i].Checked = false;
                }
            }
        }
        private void button_mpfreget_Click(object sender, EventArgs e)
        {
            try
            {
               
                uint[] htbsp=(uint[])rdr.ParamGet("ModuleSave_Frenqency");
               
                if (htbsp.Length == 0)
                {
                    // 缓冲参数
                   
                    uint[] htb = (uint[])rdr.ParamGet("FrequencyHopTable");
                    if (htb != null)
                        Sort(ref htb);
                    
                    int cnt = 0;
                    uint curchal = htb[htb.Length - 1];
                    lvhoptb.Items.Clear();
                    foreach (uint fre in htb)
                    {
                        cnt++;
                        ListViewItem item = new ListViewItem(fre.ToString());
                        if (m_params.readertype == ReaderType.PR_ONEANT)
                        {
                            if (cnt == htb.Length)
                                break;
                            if (curchal == fre)
                                item.Checked = true;

                        }
                        else
                            item.Checked = true;

                        lvhoptb.Items.Add(item);
                    }
                }
                else
                {
                    Sort(ref htbsp);
                    int cnt = 0;
                    uint curchal = htbsp[htbsp.Length - 1];
                    lvhoptb.Items.Clear();
                    foreach (uint fre in htbsp)
                    {
                        cnt++;
                        ListViewItem item = new ListViewItem(fre.ToString());
                        if (m_params.readertype == ReaderType.PR_ONEANT)
                        {
                            if (cnt == htbsp.Length)
                                break;
                            if (curchal == fre)
                                item.Checked = true;

                        }
                        else
                            item.Checked = true;

                        lvhoptb.Items.Add(item);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("获取失败:"+ex.Message);
            }
        
        }

        private void button_mpfreset_Click(object sender, EventArgs e)
        {
            uint[] shtb = null;
             List<uint> htb = new List<uint>();
            foreach (ListViewItem item in lvhoptb.Items)
            {
                if (item.Checked)
                    htb.Add(uint.Parse(item.SubItems[0].Text));
            }
            if(htb.Count<1)
            {
                MessageBox.Show("请选择其中一个");
                return;
            }
            try
            {
                 
                    shtb = htb.ToArray();
                rdr.ParamSet("ModuleSave_Frenqency",shtb);
            }
            catch(Exception mex)
            {
                MessageBox.Show("保存失败 "+mex.Message);
                return;
            }
 
            MessageBox.Show("保存成功");
        }

        private void button_mpfretmget_Click(object sender, EventArgs e)
        {
            textBox_fretime.Text = "";
             int val=(int) rdr.ParamGet("ModuleSave_Frenqency_time");
             if (val != -1)
                 textBox_fretime.Text = val.ToString();
             else
             {
                 textBox_fretime.Text = "400";
             }
        }

        private void button_mpfretmset_Click(object sender, EventArgs e)
        {
            try
            {
                int val = 0x5A;
 
                if (textBox_fretime.Text == "")
                    throw new Exception("无效值");

                val = int.Parse(textBox_fretime.Text);

                rdr.ParamSet("ModuleSave_Frenqency_time", val);
            }
            catch (Exception ex)
            {
                MessageBox.Show("保存失败:" + ex.Message);
                return;
            }

            MessageBox.Show("保存成功");
        }

        private void button_mpant1get_Click(object sender, EventArgs e)
        {
           
            textBox_ant1rpow.Text = "";
            textBox_ant1wpow.Text = "";
            textBox_ant1time.Text = "";
            try
            {
                ushort[] vals = (ushort[])rdr.ParamGet("ModuleSave_Ant_Power_Time");
                if (vals[0] != 0 && vals[1] != 0)
                {
                    textBox_ant1rpow.Text = vals[0].ToString();
                    textBox_ant1wpow.Text = vals[1].ToString();
                    textBox_ant1time.Text = vals[2].ToString();
                }
                else
                {
                    textBox_ant1rpow.Text = "2000";
                    textBox_ant1wpow.Text = "2000";
                    textBox_ant1time.Text = "0";
                }
                
            }catch(Exception ex)
            {
                MessageBox.Show("获取失败:" + ex.Message);
                return;
            }
        }

        private void button_mpant1set_Click(object sender, EventArgs e)
        {
            try
            {
                ushort[] vals = null;
                
                    if (textBox_ant1rpow.Text == ""||textBox_ant1wpow.Text==""||textBox_ant1time.Text=="")
                        throw new Exception("无效值");
                    vals = new ushort[3];
                    vals[0] = ushort.Parse(textBox_ant1rpow.Text);
                    vals[1] = ushort.Parse(textBox_ant1wpow.Text);
                    vals[2] = ushort.Parse(textBox_ant1time.Text);
                 
                rdr.ParamSet("ModuleSave_Ant_Power_Time", vals);
            }
            catch (Exception ex)
            {
                MessageBox.Show("保存失败:" + ex.Message);
                return;
            }

            MessageBox.Show("保存成功");
        }

        private void button_mpautoget_Click(object sender, EventArgs e)
        {
            try
            {
                bool val = (bool)rdr.ParamGet("ModuleSave_AutoApp");
                if (val)
                    radioButton_mpautoyes.Checked = true;
                else
                    radioButton_mpautono.Checked = true;
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message);
                return;
            }
        }

        private void button_mpautoset_Click(object sender, EventArgs e)
        {
            try
            {
                bool val;
                if (radioButton_mpautoyes.Checked)
                    val = true;
                else
                    val = false;
                rdr.ParamSet("ModuleSave_AutoApp", val);
            }
            catch (Exception ex)
            {
                MessageBox.Show("保存失败:" + ex.Message);
                return;
            }

            MessageBox.Show("保存成功");
        }

        private void button_mpbaudget_Click(object sender, EventArgs e)
        {
            try
            {
                int baud = (int)rdr.ParamGet("ModuleSave_Baudrate");

                comboBox_mpbaud.SelectedIndex = comboBox_mpbaud.Items.IndexOf(baud.ToString());
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                return;
            }
        }

        private void button_mpbaudset_Click(object sender, EventArgs e)
        {
            
           try
            {
                int val=int.Parse(comboBox_mpbaud.Text);
                rdr.ParamSet("ModuleSave_Baudrate",val);
            }
            catch (Exception ex)
            {
                MessageBox.Show("保存失败:" + ex.Message);
                return;
            }

            MessageBox.Show("保存成功");
        }

        private void btnge2sessget_Click(object sender, EventArgs e)
        {
            try
            {
                Session ses = (Session)rdr.ParamGet("ModuleSave_ProtocolConfig_Gen2_Session");
                int val = -1;
                switch (ses)
                {
                    case Session.Session0:
                        val = 0;
                        break;
                    case Session.Session1:
                        val = 1;
                        break;

                    case Session.Session2:
                        val = 2;
                        break; ;
                    case Session.Session3:
                        val = 3;
                        break;
                    default:
                        val = 0;
                        break;
                }

 
                cbbsession.SelectedIndex = val;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                return;
            }
        }

        private void btngen2sessset_Click(object sender, EventArgs e)
        {
            try
            {
                int val = cbbsession.SelectedIndex;
                Session sn;
                if (val == 0)
                    sn = Session.Session0;
                else if (val == 1)
                    sn = Session.Session1;
                else if (val == 2)
                    sn = Session.Session2;
                else
                    sn = Session.Session3;
                rdr.ParamSet("ModuleSave_ProtocolConfig_Gen2_Session", sn);
            }
            catch (Exception ex)
            {
                MessageBox.Show("保存失败:" + ex.Message);
                return;
            }

            MessageBox.Show("保存成功");
        }

        private void btngetgen2target_Click(object sender, EventArgs e)
        {
            try
            {
                Target tg = (Target)rdr.ParamGet("ModuleSave_ProtocolConfig_Gen2_Target");
                int val = -1;
                switch (tg)
                {
                    case Target.A:
                        val = 0;
                        break;
                    case Target.B:
                        val = 1;
                        break;

                    case Target.AB:
                        val = 2;
                        break; ;
                    case Target.BA:
                        val = 3;
                        break;
                    default:
                        val = 0;
                        break;
                }

                cbbgen2target.SelectedIndex = val;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                return;
            }
        }

        private void btnsetgen2target_Click(object sender, EventArgs e)
        {
            try
            {
                int val = cbbgen2target.SelectedIndex;
                Target tg;
                if (val == 0)
                    tg = Target.A;
                else if (val == 1)
                    tg = Target.B;
                else if (val == 2)
                    tg = Target.AB;
                else
                    tg = Target.BA;
              
                rdr.ParamSet("ModuleSave_ProtocolConfig_Gen2_Target", tg);
            }
            catch (Exception ex)
            {
                MessageBox.Show("保存失败:" + ex.Message);
                return;
            }

            MessageBox.Show("保存成功");
        }

        private void btngen2qget_Click(object sender, EventArgs e)
        {
            try
            {
                int val = (int)rdr.ParamGet("ModuleSave_ProtocolConfig_Gen2_Q");
                if (val == 90)
                    cbbGen2Q.SelectedIndex = 0;
                else
                    cbbGen2Q.SelectedIndex = val+1;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                return;
            }
        }

        private void btngen2qset_Click(object sender, EventArgs e)
        {
            try
            {
                int val = cbbGen2Q.SelectedIndex-1;
               
                rdr.ParamSet("ModuleSave_ProtocolConfig_Gen2_Q", val);
            }
            catch (Exception ex)
            {
                MessageBox.Show("保存失败:" + ex.Message);
                return;
            }

            MessageBox.Show("保存成功");
        }

        private void btngetgen2encode_Click(object sender, EventArgs e)
        {
            try
            {
                 int val = (int)rdr.ParamGet("ModuleSave_ProtocolConfig_Gen2_M");
                if (val == 90)
                    cbbgen2encode.SelectedIndex = 2;
                else
                    cbbgen2encode.SelectedIndex = val;
               
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                return;
            }
        }

        private void btnsetgen2encode_Click(object sender, EventArgs e)
        {
            try
            {
                int val = cbbgen2encode.SelectedIndex;
                rdr.ParamSet("ModuleSave_ProtocolConfig_Gen2_M", val);
            }
            catch (Exception ex)
            {
                MessageBox.Show("保存失败:" + ex.Message);
                return;
            }

            MessageBox.Show("保存成功");
        }

        private void btndetantsget_Click(object sender, EventArgs e)
        {
            try
            {
                int val = (int)rdr.ParamGet("ModuleSave_ReaderConfig_Check_Antenna_Connection");
                if (val == 90)
                    cbbdetectants.SelectedIndex = 1;
                else
                    cbbdetectants.SelectedIndex = val;

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                return;
            }
        }

        private void btndetantsset_Click(object sender, EventArgs e)
        {
            try
            {
                int val = cbbdetectants.SelectedIndex;
                rdr.ParamSet("ModuleSave_ReaderConfig_Check_Antenna_Connection", val);
            }
            catch (Exception ex)
            {
                MessageBox.Show("保存失败:" + ex.Message);
                return;
            }

            MessageBox.Show("保存成功");
        }

        private void btngetmel_Click(object sender, EventArgs e)
        {
            try
            {
                int val = (int)rdr.ParamGet("ModuleSave_ReaderConfig_Maximum_EPC_Length");
                if (val == 90)
                    cbbMaxEPCLength.SelectedIndex = 0;
                else
                    cbbMaxEPCLength.SelectedIndex = val;

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                return;
            }
        }

        private void btnsetmel_Click(object sender, EventArgs e)
        {
            try
            {
                int val = cbbMaxEPCLength.SelectedIndex;
                rdr.ParamSet("ModuleSave_ReaderConfig_Maximum_EPC_Length", val);
            }
            catch (Exception ex)
            {
                MessageBox.Show("保存失败:" + ex.Message);
                return;
            }

            MessageBox.Show("保存成功");
        }

        private void btngetdataantunique_Click(object sender, EventArgs e)
        {
            try
            {
                int val = (int)rdr.ParamGet("ModuleSave_ReaderConfig_Antenna_Identifier");
                
                if (val==0||val==90)
                    rbtagdataisant.Checked = true;
                else
                    rbtagdataisnoant.Checked = true;

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                return;
            }
        }

        private void btnsetdataantunique_Click(object sender, EventArgs e)
        {
            try
            {
                int val;
                if (rbtagdataisant.Checked)
                    val = 0;
                else
                    val = 1;

                rdr.ParamSet("ModuleSave_ReaderConfig_Antenna_Identifier", val);
            }
            catch (Exception ex)
            {
                MessageBox.Show("保存失败:" + ex.Message);
                return;
            }

            MessageBox.Show("保存成功");
        }

        private void btngetdataemdunique_Click(object sender, EventArgs e)
        {
            try
            {
                int val = (int)rdr.ParamGet("ModuleSave_ReaderConfig_TagData_Identifier");
                if (val == 0 || val == 90)
                    rbtagdataisnoemd.Checked = true;
                else
                    rbtagdataisemd.Checked = true;

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                return;
            }
        }

        private void btnsetdataemdunique_Click(object sender, EventArgs e)
        {
            try
            {
                int val;
                if (rbtagdataisemd.Checked)
                    val = 0;
                else
                    val = 1;
                rdr.ParamSet("ModuleSave_ReaderConfig_TagData_Identifier", val);
            }
            catch (Exception ex)
            {
                MessageBox.Show("保存失败:" + ex.Message);
                return;
            }

            MessageBox.Show("保存成功");
        }

        private void button_mpantgpoget_Click(object sender, EventArgs e)
        {
            try
            {
                int val = (int)rdr.ParamGet("ModuleSave_ReaderConfig_GPIO_Antenna_Switch");
                if (val == 90)
                    comboBox_gpoforant.SelectedIndex = 0;
                else
                    comboBox_gpoforant.SelectedIndex = val;

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                return;
            }
        }

        private void button_mpantgposet_Click(object sender, EventArgs e)
        {
            try
            {
                int val = comboBox_gpoforant.SelectedIndex;
                rdr.ParamSet("ModuleSave_ReaderConfig_GPIO_Antenna_Switch", val);
            }
            catch (Exception ex)
            {
                MessageBox.Show("保存失败:" + ex.Message);
                return;
            }

            MessageBox.Show("保存成功");
        }

        private void btngetinvmode_Click(object sender, EventArgs e)
        {
            try
            {
                int val = (int)rdr.ParamGet("ModuleSave_ReaderConfig_Transmit_Mode");
                if (val == 90)
                    cbbinvmode.SelectedIndex = 1;
                else
                    cbbinvmode.SelectedIndex = val;

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                return;
            }
        }

        private void btnsetinvmode_Click(object sender, EventArgs e)
        {
            try
            {
                int val = cbbinvmode.SelectedIndex;
                rdr.ParamSet("ModuleSave_ReaderConfig_Transmit_Mode", val);
            }
            catch (Exception ex)
            {
                MessageBox.Show("保存失败:" + ex.Message);
                return;
            }

            MessageBox.Show("保存成功");
        }

        private void btngetdatarechrssi_Click(object sender, EventArgs e)
        {

            try
            {
                int val = (int)rdr.ParamGet("ModuleSave_ReaderConfig_Record_highest_RSSI");
                if (val == 0 || val == 90)
                    rbtagdataisrecrssi.Checked = true;
                else
                    rbtagdataisnorecrssi.Checked = true;

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                return;
            }
        }

        private void btnsetdatarechrssi_Click(object sender, EventArgs e)
        {
            try
            {
                int val;
                if (rbtagdataisrecrssi.Checked)
                    val = 0;
                else
                    val = 1;
                rdr.ParamSet("ModuleSave_ReaderConfig_Record_highest_RSSI", val);
            }
            catch (Exception ex)
            {
                MessageBox.Show("保存失败:" + ex.Message);
                return;
            }

            MessageBox.Show("保存成功");
        }

        private void btngetrg_Click(object sender, EventArgs e)
        {
            try
            {

                ModuleTech.Region rg = (ModuleTech.Region)rdr.ParamGet("ModuleSave_Region");
                rdr.ParamSet("Region", rg);
                switch (rg)
                {
                    case ModuleTech.Region.CN:
                        this.cbbregion.SelectedIndex = 6;
                        break;
                    case ModuleTech.Region.EU:
                    case ModuleTech.Region.EU2:
                    case ModuleTech.Region.EU3:
                        this.cbbregion.SelectedIndex = 4;
                        break;
                    case ModuleTech.Region.IN:
                        this.cbbregion.SelectedIndex = 5;
                        break;
                    case ModuleTech.Region.JP:
                        this.cbbregion.SelectedIndex = 2;
                        break;
                    case ModuleTech.Region.KR:
                        this.cbbregion.SelectedIndex = 3;
                        break;
                    case ModuleTech.Region.NA:
                        this.cbbregion.SelectedIndex = 1;
                        break;
                    case ModuleTech.Region.PRC:
                        this.cbbregion.SelectedIndex = 0;
                        break;
                    case ModuleTech.Region.OPEN:
                        this.cbbregion.SelectedIndex = 7;
                        break;
                    case ModuleTech.Region.PRC2:
                        this.cbbregion.SelectedIndex = 8;
                        break;
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show("获取失败");
            }
        }

        private void btnsetrg_Click(object sender, EventArgs e)
        {
            ModuleTech.Region rg = ModuleTech.Region.UNSPEC;
           
            if (this.cbbregion.SelectedIndex == -1)
            {
                MessageBox.Show("请选择区域");
                return;
            }
            switch (this.cbbregion.SelectedIndex)
            {
                case 0:
                    rg = ModuleTech.Region.PRC;//中国
                    break;
                case 1:
                    rg = ModuleTech.Region.NA;//北美
                    break;
                case 2:
                    rg = ModuleTech.Region.JP;//日本
                    break;
                case 3:
                    rg = ModuleTech.Region.KR;//韩国
                    break;
                case 4:
                    rg = ModuleTech.Region.EU3;//欧洲
                    break;
                case 5:
                    rg = ModuleTech.Region.IN;//印度
                    break;
                case 6:
                    //rg = ModuleTech.Region.CN;//加拿大
                    {
                        MessageBox.Show("不支持");
                        return;
                    }
                    break;
                case 7:
                    rg = ModuleTech.Region.OPEN;//全频段
                    break;
                case 8:
                    rg = ModuleTech.Region.PRC2;//中国2
                    break;
                 
            }

           
            try
            {
                rdr.ParamSet("ModuleSave_Region", rg);
                button_mpfreget.PerformClick();
            }
            catch (Exception mex)
            {
                MessageBox.Show("保存失败 " + mex.Message);
                return;
            }

            MessageBox.Show("保存成功");
        }

        private void button_default_Click(object sender, EventArgs e)
        {
            try
            {
                rdr.ParamSet("ModuleSave_default", true);
                getallparams();
            }
            catch (Exception mex)
            {
                MessageBox.Show("保存失败 " + mex.Message);
                return;
            }

            MessageBox.Show("保存成功");
        }

        private void button_regiondef_Click(object sender, EventArgs e)
        {
            try
            {
                rdr.ParamSet("ModuleSave_Region", null);
                btngetrg.PerformClick();
                button_mpfreget.PerformClick();
            }
            catch (Exception mex)
            {
                MessageBox.Show("保存失败 " + mex.Message);
                return;
            }
            MessageBox.Show("保存成功");
        }

        private void button_fredef_Click(object sender, EventArgs e)
        {
            try
            {
                rdr.ParamSet("ModuleSave_Frenqency", null);
                button_mpfreget.PerformClick();
            }
            catch (Exception mex)
            {
                MessageBox.Show("保存失败 " + mex.Message);
                return;
            }
            MessageBox.Show("保存成功");
        }

        private void button_fretimedef_Click(object sender, EventArgs e)
        {
            try
            {
                int val = 0x5A;
 
                rdr.ParamSet("ModuleSave_Frenqency_time", val);
                button_mpfretmget.PerformClick();
            }
            catch (Exception ex)
            {
                MessageBox.Show("保存失败:" + ex.Message);
                return;
            }
            MessageBox.Show("保存成功");
        }

        private void button_ant1def_Click(object sender, EventArgs e)
        {
            button_antpwdef_Click(sender, e);
 
        }

        private void button_readytoupdate_Click(object sender, EventArgs e)
        {
            try
            {
                rdr.ParamSet("ModuleSave_readytoupdate", true);
              
            }
            catch (Exception mex)
            {
                MessageBox.Show("保存失败 " + mex.Message);
                return;
            }

            MessageBox.Show("保存成功");
        }

        private void button_autodef_Click(object sender, EventArgs e)
        {
            try
            {
                
                rdr.ParamSet("ModuleSave_AutoApp", false);
                button_mpautoget.PerformClick();
            }
            catch (Exception ex)
            {
                MessageBox.Show("保存失败:" + ex.Message);
                return;
            }

            MessageBox.Show("保存成功");
        }

        private void button_readerconfigdef_Click(object sender, EventArgs e)
        {
            try
            {

                rdr.ParamSet("ModuleSave_ReaderConfig_default", true);
                btndetantsget.PerformClick();
                btngetmel.PerformClick();
                btngetdataantunique.PerformClick();
                btngetdataemdunique.PerformClick();
                btngetdatarechrssi.PerformClick();
                button_mpantgpoget.PerformClick();
                btngetinvmode.PerformClick();
            }
            catch (Exception ex)
            {
                MessageBox.Show("保存失败:" + ex.Message);
                return;
            }

            MessageBox.Show("保存成功");
        }

        private void button_protocoldef_Click(object sender, EventArgs e)
        {
            try
            {

                rdr.ParamSet("ModuleSave_ProtocolConfig_default", true);
                btnge2sessget.PerformClick();
                btngetgen2target.PerformClick();
                btngen2qget.PerformClick();
                btngetgen2encode.PerformClick();
            }
            catch (Exception ex)
            {
                MessageBox.Show("保存失败:" + ex.Message);
                return;
            }

            MessageBox.Show("保存成功");
        }

        private void button_baudrdef_Click(object sender, EventArgs e)
        {
            try
            {
                rdr.ParamSet("ModuleSave_Baudrate", 115200);
                button_mpbaudget.PerformClick();
            }
            catch (Exception ex)
            {
                MessageBox.Show("保存失败:" + ex.Message);
                return;
            }

            MessageBox.Show("保存成功");
        }

        private void comboBox_mpbaud_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void button_antpwdef_Click(object sender, EventArgs e)
        {
            try
            {
                ushort[] vals = null;
                rdr.ParamSet("ModuleSave_Ant_Power_Time", vals);

                if (rdr.HwDetails.module == Reader.Module_Type.MODOULE_SLR1200)
                {
                    textBox_ant1rpow.Text = "1000";
                    textBox_ant1wpow.Text = "1000";
                    textBox_ant1time.Text = "0";
                }
                else if (rdr.HwDetails.module == Reader.Module_Type.MODOULE_SLR5100)
                {
                    textBox_ant1rpow.Text = "1000";
                    textBox_ant1wpow.Text = "1000";
                    textBox_ant1time.Text = "0";
                }
                else if (rdr.HwDetails.module == Reader.Module_Type.MODOULE_SLR5200)
                {
                    textBox_ant1rpow.Text = "1000";
                    textBox_ant1wpow.Text = "1000";
                    textBox_ant1time.Text = "0";
                }
                else if (rdr.HwDetails.module == Reader.Module_Type.MODOULE_SLR5300)
                {
                    textBox_ant1rpow.Text = "2000";
                    textBox_ant1wpow.Text = "2000";
                    textBox_ant1time.Text = "0";
                }
                else if (rdr.HwDetails.module == Reader.Module_Type.MODOULE_SLR1100)
                {
                    textBox_ant41rpow.Text = "2000";
                    textBox_ant41wpow.Text = "2000";
                    textBox_ant41time.Text = "0";

                    textBox_ant42rpow.Text = "2000";
                    textBox_ant42wpow.Text = "2000";
                    textBox_ant42time.Text = "0";

                    textBox_ant43rpow.Text = "2000";
                    textBox_ant43wpow.Text = "2000";
                    textBox_ant43time.Text = "0";

                    textBox_ant44rpow.Text = "2000";
                    textBox_ant44wpow.Text = "2000";
                    textBox_ant44time.Text = "0";
                }

                checkBox_ant1.Checked = false;
                checkBox_ant2.Checked = false;
                checkBox_ant3.Checked = false;
                checkBox_ant4.Checked = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show("保存失败:" + ex.Message);
                return;
            }
            MessageBox.Show("保存成功");
        }

        private void button_setantpw_Click(object sender, EventArgs e)
        {
            try
            {
                ushort[] vals = null;

                if (textBox_ant41rpow.Text == "" || textBox_ant41wpow.Text == "" || textBox_ant41time.Text == "")
                    throw new Exception("无效值");
                if (textBox_ant42rpow.Text == "" || textBox_ant42wpow.Text == "" || textBox_ant42time.Text == "")
                    throw new Exception("无效值");
                if (textBox_ant43rpow.Text == "" || textBox_ant43wpow.Text == "" || textBox_ant43time.Text == "")
                    throw new Exception("无效值");
                if (textBox_ant44rpow.Text == "" || textBox_ant44wpow.Text == "" || textBox_ant44time.Text == "")
                    throw new Exception("无效值");

                vals = new ushort[12];
                vals[0] = ushort.Parse(textBox_ant41rpow.Text);
                vals[1] = ushort.Parse(textBox_ant41wpow.Text);
                vals[2] = ushort.Parse(textBox_ant41time.Text);

                vals[3] = ushort.Parse(textBox_ant42rpow.Text);
                vals[4] = ushort.Parse(textBox_ant42wpow.Text);
                vals[5] = ushort.Parse(textBox_ant42time.Text);

                vals[6] = ushort.Parse(textBox_ant43rpow.Text);
                vals[7] = ushort.Parse(textBox_ant43wpow.Text);
                vals[8] = ushort.Parse(textBox_ant43time.Text);

                vals[9] = ushort.Parse(textBox_ant44rpow.Text);
                vals[10] = ushort.Parse(textBox_ant44wpow.Text);
                vals[11] = ushort.Parse(textBox_ant44time.Text);

                rdr.ParamSet("ModuleSave_Ant_Power_Time", vals);

               
            }
            catch (Exception ex)
            {
                MessageBox.Show("保存失败:" + ex.Message);
                return;
            }

            MessageBox.Show("保存成功");
        }

        private void button_getantpw_Click(object sender, EventArgs e)
        {
            textBox_ant41rpow.Text = "";
            textBox_ant41wpow.Text = "";
            textBox_ant41time.Text = "";

            textBox_ant41rpow.Text = "";
            textBox_ant41wpow.Text = "";
            textBox_ant41time.Text = "";

            textBox_ant41rpow.Text = "";
            textBox_ant41wpow.Text = "";
            textBox_ant41time.Text = "";

            textBox_ant41rpow.Text = "";
            textBox_ant41wpow.Text = "";
            textBox_ant41time.Text = "";
            try
            {
                ushort[] vals = (ushort[])rdr.ParamGet("ModuleSave_Ant_Power_Time");
                if ((vals[0] != 0 && vals[1] != 0) && (vals[0] != 0xFFFF && vals[1] != 0xFFFF))
                {
                    textBox_ant41rpow.Text = vals[0].ToString();
                    textBox_ant41wpow.Text = vals[1].ToString();
                    textBox_ant41time.Text = vals[2].ToString();

                    textBox_ant42rpow.Text = vals[3].ToString();
                    textBox_ant42wpow.Text = vals[4].ToString();
                    textBox_ant42time.Text = vals[5].ToString();

                    textBox_ant43rpow.Text = vals[6].ToString();
                    textBox_ant43wpow.Text = vals[7].ToString();
                    textBox_ant43time.Text = vals[8].ToString();

                    textBox_ant44rpow.Text = vals[9].ToString();
                    textBox_ant44wpow.Text = vals[10].ToString();
                    textBox_ant44time.Text = vals[11].ToString();
                }
                else
                {

                    textBox_ant41rpow.Text = "2000";
                    textBox_ant41wpow.Text = "2000";
                    textBox_ant41time.Text = "0";

                    textBox_ant42rpow.Text = "2000";
                    textBox_ant42wpow.Text = "2000";
                    textBox_ant42time.Text = "0";

                    textBox_ant43rpow.Text = "2000";
                    textBox_ant43wpow.Text = "2000";
                    textBox_ant43time.Text = "0";

                    textBox_ant44rpow.Text = "2000";
                    textBox_ant44wpow.Text = "2000";
                    textBox_ant44time.Text = "0";
                }

                checkBox_ant1.Checked = false;
                checkBox_ant2.Checked = false;
                checkBox_ant3.Checked = false;
                checkBox_ant4.Checked = false;

                if (vals.Length == 13)
                {
                    byte bt = (byte)vals[12];
                    if ((bt & 0x01) != 0)
                        checkBox_ant1.Checked = true;
                    if ((bt & 0x04) != 0)
                        checkBox_ant2.Checked = true;
                    if ((bt & 0x02) != 0)
                        checkBox_ant3.Checked = true;
                    if ((bt & 0x08) != 0)
                        checkBox_ant4.Checked = true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("获取失败:" + ex.Message);
                return;
            }
        }

        private void textBox4_TextChanged(object sender, EventArgs e)
        {

        }

        private void textBox6_TextChanged(object sender, EventArgs e)
        {

        }

        private void textBox5_TextChanged(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                List<int> li = new List<int>();
                if (checkBox_ant1.Checked)
                    li.Add(1);
                if (checkBox_ant2.Checked)
                    li.Add(2);
                if (checkBox_ant3.Checked)
                    li.Add(3);
                if (checkBox_ant4.Checked)
                    li.Add(4);

                rdr.ParamSet("ModuleSave_Ant_port", li.ToArray());
            }
            catch (Exception ex)
            {
                MessageBox.Show("保存失败:" + ex.Message);
                return;
            }

            MessageBox.Show("保存成功");
           
        }
 
 
    }
}
