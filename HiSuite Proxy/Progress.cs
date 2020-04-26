using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using MaterialSkin.Controls;
using System.Threading;

namespace HiSuite_Proxy
{
    public partial class Progress : MaterialForm
    {
        public Progress(string text)
        {
            InitializeComponent();
            CheckForIllegalCrossThreadCalls = false;
            this.Text = text;
        }
        public void SetProgress(int value, string title = "")
        {
            progressBar1.Value = value;
            if (title.Length > 2)
            {
                this.Text = title;
            }
        }

        private void materialRaisedButton1_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
