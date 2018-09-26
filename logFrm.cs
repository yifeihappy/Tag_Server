using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using ModuleTech;
using ModuleLibrary;

namespace ModuleReaderManager
{
    public partial class logFrm : Form
    {
        public logFrm()
        {
            InitializeComponent();
        }
        Reader rdr = null;
        private void btngetlog_Click(object sender, EventArgs e)
        {
            if (this.tbrdraddr.Text.Trim() == string.Empty)
            {
                MessageBox.Show("请输入读写器地址");
                return;
            }
            try
            {
                rdr = Reader.Create(this.tbrdraddr.Text.Trim(), 4);
                rdr.ParamSet("StartGetLog", true);
                rtblogstr.Text = "";
                while (true)
                {
                    try
                    {
                        string logblk = (string)rdr.ParamGet("NextLogBlock");
                        rtblogstr.Text += logblk;
                    }
                    catch (OpFaidedException exx)
                    {
                        if (exx.ErrCode == 0xA010 || 0xA00F == exx.ErrCode)
                            return;
                    }
                    catch (System.Exception ex)
                    {
                        MessageBox.Show("操作失败：" + ex.ToString());
                        return;
                    }
                }
    
            }
            catch
            {
                MessageBox.Show("连接读写器失败");
                return;
            }
        }

        private void btnsoftreboot_Click(object sender, EventArgs e)
        {
            if (rdr == null)
            {
                MessageBox.Show("请先获取日志和读写器建立连接后，才可以使用软重启功能");
                return;
            }
            try
            {
                rdr.ParamSet("BootFirmware", true);
                MessageBox.Show("操作成功");
            }
            catch (System.Exception ex)
            {
                MessageBox.Show("操作失败：" + ex.ToString());
                return;
            }
        }
    }
}
