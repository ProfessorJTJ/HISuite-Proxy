using System;
using System.Collections.Generic;
using System.Windows.Forms;
using MaterialSkin.Controls;
using System.Net;
using System.Threading;

namespace HiSuite_Proxy
{
    public partial class FirmFinder : MaterialForm
    {
        Form1 StarterForm = null;
        List<Thread> RunningThreads = new List<Thread>();
        public FirmFinder(Form1 form1)
        {
            InitializeComponent();
            CheckForIllegalCrossThreadCalls = false;

            StarterForm = form1;
            ChangingsHandler();

            this.FormClosing += (s, e) =>
            {
                for (int i = 0, j = RunningThreads.Count; i < j; i ++)
                {
                    try
                    {
                        RunningThreads[i].Abort();
                    }
                    catch
                    {

                    }
                }
                RunningThreads.Clear();
                e.Cancel = true;
                this.Hide();
            };
        }

        private void ChangingsHandler()
        {
            materialCheckBox1.CheckedChanged += delegate
            {
                StarterForm._customData.CustomBase = materialCheckBox1.Checked;
                StarterForm._customData.CustomBaseID = materialSingleLineTextField3.Text;
            };

            materialCheckBox2.CheckedChanged += delegate
            {
                StarterForm._customData.CustomPreload = materialCheckBox2.Checked;
                StarterForm._customData.CustomPreloadID = materialSingleLineTextField4.Text;
            };

            materialCheckBox3.CheckedChanged += delegate
            {
                StarterForm._customData.CustomCust = materialCheckBox3.Checked;
                StarterForm._customData.CustomCustID = materialSingleLineTextField5.Text;
            };

            materialCheckBox4.CheckedChanged += delegate
            {
                StarterForm._customData.LocalBase = materialCheckBox4.Checked;
                if (StarterForm._customData.LocalBase == true)
                {
                    OpenFileDialog dialog = new OpenFileDialog();
                    dialog.FileName = materialSingleLineTextField6.Text;
                    if (dialog.ShowDialog() == DialogResult.OK)
                    {
                        materialSingleLineTextField6.Text = dialog.FileName;
                    }
                    StarterForm._customData.LocalBaseDir = materialSingleLineTextField6.Text;
                }
            };

            materialCheckBox5.CheckedChanged += delegate
            {
                StarterForm._customData.LocalPreload = materialCheckBox5.Checked;
                if (StarterForm._customData.LocalPreload == true)
                {
                    OpenFileDialog dialog = new OpenFileDialog();
                    dialog.FileName = materialSingleLineTextField7.Text;
                    if (dialog.ShowDialog() == DialogResult.OK)
                    {
                        materialSingleLineTextField7.Text = dialog.FileName;
                    }
                    StarterForm._customData.LocalPreloadDir = materialSingleLineTextField7.Text;
                }
            };

            materialCheckBox6.CheckedChanged += delegate
            {
                StarterForm._customData.LocalCust = materialCheckBox6.Checked;
                if (StarterForm._customData.LocalCust == true)
                {
                    OpenFileDialog dialog = new OpenFileDialog();
                    dialog.FileName = materialSingleLineTextField8.Text;
                    if (dialog.ShowDialog() == DialogResult.OK)
                    {
                        materialSingleLineTextField8.Text = dialog.FileName;
                    }
                    StarterForm._customData.LocalCustDir = materialSingleLineTextField8.Text;
                }
            };

            materialSingleLineTextField3.TextChanged += delegate
            {
                StarterForm._customData.CustomBaseID = materialSingleLineTextField3.Text;
            };

            materialSingleLineTextField4.TextChanged += delegate
            {
                StarterForm._customData.CustomPreloadID = materialSingleLineTextField4.Text;
            };

            materialSingleLineTextField5.TextChanged += delegate
            {
                StarterForm._customData.CustomCustID = materialSingleLineTextField5.Text;
            };

            materialSingleLineTextField6.Click += delegate
            {
                if (!StarterForm._customData.LocalBase)
                    return;
                OpenFileDialog dialog = new OpenFileDialog();
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    materialSingleLineTextField6.Text = dialog.FileName;
                    StarterForm._customData.LocalBaseDir = dialog.FileName;
                }
            };

            materialSingleLineTextField7.Click += delegate
            {
                if (!StarterForm._customData.LocalPreload)
                    return;
                OpenFileDialog dialog = new OpenFileDialog();
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    materialSingleLineTextField7.Text = dialog.FileName;
                    StarterForm._customData.LocalPreloadDir = dialog.FileName;
                }
            };

            materialSingleLineTextField8.Click += delegate
            {
                if (!StarterForm._customData.LocalCust)
                    return;
                OpenFileDialog dialog = new OpenFileDialog();
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    materialSingleLineTextField8.Text = dialog.FileName;
                    StarterForm._customData.LocalCustDir = dialog.FileName;
                }
            };
        }

        private bool checkingthread = false, checkingdetialsthread = false;
        private void materialRaisedButton1_Click(object sender, EventArgs e)
        {
            if(checkingthread)
            {
                MessageBox.Show("Already checking please wait");
                return;
            }
            string url = materialSingleLineTextField1.Text;
            if(url.Contains("/files/p3/s15") && url.Contains("/v"))
            {
                Uri result;
                if(Uri.TryCreate(url, UriKind.Absolute, out result) && ( result.Scheme == Uri.UriSchemeHttp || result.Scheme == Uri.UriSchemeHttps ))
                {
                    checkingthread = true;
                    Thread thethread = new Thread(() =>
                    {
                        CheckFirmWareData(url);
                        checkingthread = false;
                    });
                    RunningThreads.Add(thethread);
                    thethread.Start();
                }
                else
                {
                    MessageBox.Show("Invalid URL Passed");
                }
            }
            else
            {
                MessageBox.Show("Invalid URL Passed");
            }
        }
       
        
        private void CheckFirmWareData(string url)
        {
            try
            {
                this.Invoke(new Action(() =>
                {
                    materialRadioButton1.Checked = false;
                    materialRadioButton1.Enabled = false;
                    materialRadioButton1.Text = "Checking";
                }));
                RomDetails.RomDetailsClass details = new RomDetails.RomDetailsClass();
                RomDetails.GetFirmwareDetails(this, url, ref details, 0);

                this.Invoke(new Action(() =>
                {
                    if (details.ApprovedForInstall)
                    {
                        materialRadioButton1.Text = "Approved for installation";
                        materialRadioButton1.Enabled = true;
                        materialRadioButton1.Checked = true;
                    }
                    else
                    {
                        materialRadioButton1.Text = "Not Approved for installation";
                        materialRadioButton1.Enabled = false;
                        materialRadioButton1.Checked = false;
                    }
                }));
            }
            catch(Exception e)
            {
                this.Invoke(new Action(() =>
                {
                    materialRadioButton1.Enabled = false;
                    materialRadioButton1.Text = "ERROR";
                }));
                MessageBox.Show(e.Message, "ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }



        public class gZipWebClient : WebClient
        {
            private int Timeout = 45000;
            public bool Keepalive = false;
            public void SetTimeout(int timeout)
            {
                Timeout = timeout;
            }
            protected override WebRequest GetWebRequest(Uri address)
            {
                HttpWebRequest request = base.GetWebRequest(address) as HttpWebRequest;
                request.Timeout = Timeout;
                request.ReadWriteTimeout = Timeout;
                request.KeepAlive = Keepalive;
                request.AllowAutoRedirect = false;
                request.ProtocolVersion = new Version("1.1");
                request.AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip;
                return request;
            }
        }

        private void CheckFirmWareDetails(string url)
        {
            try
            {
                this.Invoke(new Action(() =>
                {
                    dataGridView1.Rows.Clear();
                }));
                RomDetails.RomDetailsClass details = new RomDetails.RomDetailsClass();
                RomDetails.GetFirmwareDetails(this, url, ref details, 1);
                
                this.Invoke(new Action(() =>
                {
                    for (int i = 0, j = details.SupportedVersions.Length; i < j; i++)
                    {
                        if (details.SupportedVersions[i].Length > 2)
                        {
                            dataGridView1.Rows.Add((i + 1).ToString(), details.SupportedVersions[i]);
                        }
                    }
                }));
            }
            catch (Exception e)
            {
                if(!e.Message.StartsWith("Thread was being"))
                {
                    MessageBox.Show(e.Message, "ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
        private void materialRaisedButton2_Click(object sender, EventArgs e)
        {
            if (checkingdetialsthread)
            {
                MessageBox.Show("Already checking please wait");
                return;
            }
            string url = materialSingleLineTextField1.Text;
            if (url.Contains("/files/p3/s15") && url.Contains("/v"))
            {
                Uri result;
                if (Uri.TryCreate(url, UriKind.Absolute, out result) && (result.Scheme == Uri.UriSchemeHttp || result.Scheme == Uri.UriSchemeHttps))
                {
                    checkingdetialsthread = true;

                    Thread thethread = new Thread(() =>
                    {
                        CheckFirmWareDetails(url);
                        checkingdetialsthread = false;
                    });
                    RunningThreads.Add(thethread);
                    thethread.Start();
                }
                else
                {
                    MessageBox.Show("Invalid URL Passed");
                }
            }
            else
            {
                MessageBox.Show("Invalid URL Passed");
            }
        }
    }
}
