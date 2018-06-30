using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace GreenLock
{
    public partial class PowerUserControl : UserControl
    {
        public PowerUserControl()
        {
            InitializeComponent();
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            AppConfig.Instance.PcPower = Double.Parse(this.tbPower.Text);
            AppConfig.Instance.SaveToFile();
        }

        private void PowerUserControl_Load(object sender, EventArgs e)
        {
            this.tbPower.Text = AppConfig.Instance.PcPower.ToString();
        }
    }
}
