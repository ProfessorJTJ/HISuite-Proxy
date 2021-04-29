using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Threading;
using System.IO;
using System.Diagnostics;
using System.Net;

namespace HiSuite_Proxy
{
    public partial class SetUP : Form
    {
        private string HISuiteVersion = null;
        private Color GreenColor = Color.FromArgb(0, 204, 0), RedColor = Color.FromArgb(230, 46, 0), OrangeColor = Color.FromArgb(255, 175, 26);
        Form1 form;
        public SetUP(Form1 Form)
        {
            InitializeComponent();
            form = Form;
            richTextBox1.ReadOnly = true;
            richTextBox1.ScrollBars = RichTextBoxScrollBars.Both;
            CheckForIllegalCrossThreadCalls = false;
            richTextBox1.SelectionFont = new Font(richTextBox1.SelectionFont.Name, 8, FontStyle.Bold);
            new Thread(() =>
            {
                try
                {
                    CheckPatch();
                }
                catch(Exception e)
                {
                    //AppendMessage("Crashed! " + e.Message + Environment.NewLine + e.StackTrace + Environment.NewLine + Environment.NewLine + e.InnerException.StackTrace, RedColor);
                    AppendMessage("Crashed! " + e.Message, RedColor);
                    AppendMessage("Please Contact Developer If Error Continues", RedColor);
                    AppendMessage("https://github.com/ProfessorJTJ/HISuite-Proxy/issues", GreenColor);
                }
            }).Start();         
        }
        private bool firsttime = true;
        private void AppendMessage(string text, Color color)
        {
            richTextBox1.SelectionColor = color;
            DateTime timenow = DateTime.Now;
            string time = string.Format(" [{0:D2}:{1:D2}:{2:D2}]: ",
            timenow.Hour,
            timenow.Minute,
            timenow.Second);
            if (firsttime)
            {
                firsttime = false;
                richTextBox1.AppendText(time + text);
            }
            else
            {
                richTextBox1.AppendText(Environment.NewLine + time + text);
            }
            Thread.Sleep(500);
        }
        private void CheckPatch()
        {
            AppendMessage("Checking if HISuite is Installed...", GreenColor);
            string hisuitedir = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86) + @"\HiSuite";
            if (File.Exists(hisuitedir + @"\httpcomponent.dll"))
            {
                AppendMessage("HiSuite Exists, Proceeding...", GreenColor);

                bool Closed = false;
                Process[] hisuiteopen = Process.GetProcesses();
                foreach(Process proc in hisuiteopen)
                {
                    if(proc.ProcessName.ToLower().Contains("hisuite") && !proc.ProcessName.ToLower().Contains("proxy"))
                    {
                        proc.Kill();
                        Closed = true;
                    }
                }
                if (Closed)
                {
                    AppendMessage("Closing HISuite....", GreenColor);
                    //hisuiteopen[0].Kill();
                    Thread.Sleep(1500);
                }

                AppendMessage("Checking HISuite Version...", GreenColor);
                string settings = hisuitedir + @"\RunInfo.ini";
                if (File.Exists(settings))
                {
                    string data = File.ReadAllText(settings);
                    int whereisit = data.IndexOf("version=");
                    if(whereisit != -1)
                    {
                        whereisit += 8;
                        HISuiteVersion = data;
                        int finish = data.IndexOf('.', whereisit);
                        int versionparse = int.Parse(data.Substring(whereisit, finish - whereisit));
                        if(versionparse < 10)
                        {
                            AppendMessage("HiSuite Version Isn't Supported, Please Download Latest Version....", RedColor);
                            AppendMessage("https://consumer.huawei.com/en/support/hisuite/", GreenColor);
                            return;
                        }
                        else
                        {
                            AppendMessage("HiSuite Version " + versionparse + " Is Installed, Proceeding....", GreenColor);
                        }
                    }
                    else
                    {
                        AppendMessage("Failed to read HISuite Version, Aborting process....", RedColor);
                        AppendMessage("Please Contact Developer If Error Continues", RedColor);
                        AppendMessage("https://github.com/ProfessorJTJ/HISuite-Proxy/issues", GreenColor);
                        return;
                    }
                }
                else
                {
                    AppendMessage("Failed to read HISuite Version, Aborting process....", RedColor);
                    AppendMessage("Please Contact Developer If Error Continues", RedColor);
                    AppendMessage("https://github.com/ProfessorJTJ/HISuite-Proxy/issues", GreenColor);
                    return;
                }
                AppendMessage("Patching HISuite Files....", GreenColor);
                if(form.Patch(hisuitedir + @"\httpcomponent.dll", true))
                {
                    AppendMessage("Patch succeeded, Proceeding....", GreenColor);
                    SetProxySettings();
                }
                else
                {
                    AppendMessage("Patch failed, Aborting process....", RedColor);
                    AppendMessage("Please Contact Developer If Error Continues", RedColor);
                    AppendMessage("https://github.com/ProfessorJTJ/HISuite-Proxy/issues", GreenColor);
                }
            }
            else
            {
                AppendMessage("HiSuite Is not installed, Aborting process....", RedColor);
                Process.Start("https://consumer.huawei.com/en/support/hisuite/");
            }
        }
        private void SetProxySettings()
        {
            AppendMessage("Checking HISuite Proxy Settings...", GreenColor);
            string settings = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + @"\HiSuite\userdata\Setting.ini";
            if(File.Exists(settings))
            {
                string[] settingsdata = File.ReadAllLines(settings);
                bool changedsmth = false, foundline = false;
                for(int i = 0, j = settingsdata.Length; i < j;  i++)
                {
                    string settingdata = settingsdata[i];
                    if (settingdata.StartsWith("proxytype"))
                    {
                        foundline = true;
                        if (!settingdata.EndsWith("=2"))
                        {
                            changedsmth = true;
                            settingsdata[i] = "proxytype=2";
                        }
                    }
                    else if (settingdata.StartsWith("hostaddr"))
                    {
                        foundline = true;
                        if (!settingdata.EndsWith("=127.0.0.1"))
                        {
                            changedsmth = true;
                            settingsdata[i] = "hostaddr=127.0.0.1";
                        }
                    }
                    else if (settingdata.StartsWith("port"))
                    {
                        foundline = true;
                        if (!settingdata.EndsWith("=7777"))
                        {
                            changedsmth = true;
                            settingsdata[i] = "port=7777";
                        }
                    }
                    else if (settingdata.StartsWith("username_s"))
                    {
                        foundline = true;
                        if (changedsmth)
                            settingsdata[i] = "username_s=";
                    }
                    else if (settingdata.StartsWith("password_s"))
                    {
                        foundline = true;
                        if (changedsmth)
                            settingsdata[i] = "password_s=";
                    }
                }
                if(!foundline)
                {
                    List<string> nsettings = new List<string>(settingsdata);
                    nsettings.Add("[proxy]");
                    nsettings.Add("proxytype=2");
                    nsettings.Add("hostaddr=127.0.0.1");
                    nsettings.Add("port=7777");
                    nsettings.Add("username=");
                    nsettings.Add("password=");
                    nsettings.Add("user_iv=");
                    nsettings.Add("psw_iv=");
                    nsettings.Add("username_s=");
                    nsettings.Add("password_s=");
                    AppendMessage("Successfully Set Proxy Settings, Proceeding...", GreenColor);
                    File.WriteAllLines(settings, nsettings);
                }
                else
                {
                    if (changedsmth)
                    {
                        AppendMessage("Successfully Set Proxy Settings, Proceeding...", GreenColor);
                        File.WriteAllLines(settings, settingsdata);
                    }
                    else
                    {
                        AppendMessage("Proxy Settings Are Already Set, Proceeding...", OrangeColor);
                    }
                }
                CheckHostFile();
            }
            else
            {
                AppendMessage("Couldn't find HISuite Settings file....", RedColor);
                AppendMessage("Please Contact Developer If Error Continues", RedColor);
                AppendMessage("https://github.com/ProfessorJTJ/HISuite-Proxy/issues", GreenColor);
            }
        }
        private void CheckHostFile()
        {
            AppendMessage("Checking Hosts File For Redundant Entries....", GreenColor);
            string hostsfile = Environment.GetFolderPath(Environment.SpecialFolder.System) + @"\drivers\etc\hosts";
            if(File.Exists(hostsfile))
            {
                string[] settingsdata = File.ReadAllLines(hostsfile);
                bool changedsmth = false;
                for (int i = 0, j = settingsdata.Length; i < j; i++)
                {
                    string settingdata = settingsdata[i];
                    if (!settingdata.StartsWith("#"))
                    {
                        if (settingdata.EndsWith("hicloud.com"))
                        {
                            changedsmth = true;
                            string templine = settingsdata[i].Substring(0);
                            settingsdata[i] = "# " + templine;
                        }
                    }
                }
                if (changedsmth)
                {
                    AppendMessage("Hosts File Was Successfully Fixed, Proceeding...", GreenColor);
                    File.WriteAllLines(hostsfile, settingsdata);
                }
                else
                {
                    AppendMessage("Hosts File Is Already Neat, Proceeding...", OrangeColor);
                }
            }
            else
            {
                AppendMessage("Couldn't Find Hosts File, skipping....", OrangeColor);
            }
            TryConnectionToProxy();
        }
        private void TryConnectionToProxy()
        {
            AppendMessage("Checking Connection To HISuite Proxy....", GreenColor);
            string response = null;
            try
            {
                WebClient client = new WebClient();
                client.Proxy = new WebProxy("127.0.0.1:7777");
                response = client.DownloadString("http://hisuiteproxy.org/checkHiSuiteConnection");
            }
            catch
            {
                response = null;
            }
            if(response == null)
            {
                AppendMessage("Connection To HISuite Proxy Failed....", RedColor);
                AppendMessage("Check If Your Firewall Is Blocking HISuite's Proxy Server", OrangeColor);
            }
            else
            {
                AppendMessage("HISuite Proxy Is On And Working.", GreenColor);
                SayFinished();
            }
        }
        private void SayFinished()
        {
            AppendMessage("", GreenColor);
            AppendMessage("Checks Finished, You can proceed to installation now!", GreenColor);

            if (HISuiteVersion.Contains("version=11.0.0.510"))
            {
                AppendMessage("Use HISuite 11.exe instead of your older file...", OrangeColor);
                AppendMessage("If your phone doesn't connect, enable 'USB Debugging'.", OrangeColor);
            }
        }
    }
}
