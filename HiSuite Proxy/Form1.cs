using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Titanium.Web.Proxy;
using Titanium.Web.Proxy.Models;
using System.Threading;
using System;
using System.Diagnostics;
using System.IO;
using System.Net.Security;

namespace HiSuite_Proxy
{
    public partial class Form1 : Form
    {
        ProxyServer proxyserver = new ProxyServer();
        ExplicitProxyEndPoint endpoint = new ExplicitProxyEndPoint(IPAddress.Any, 7777);
        public class CustomData
        {
            public bool CustomBase = false, CustomPreload = false, CustomCust = false, LocalBase = false, LocalPreload = false, LocalCust = false;
            public string CustomBaseID, CustomPreloadID, CustomCustID, LocalBaseDir, LocalPreloadDir, LocalCustDir;
        }
        public CustomData _customData = new CustomData();
        FirmFinder firmFinder = null;
        public Form1()
        {
            InitializeComponent();
            firmFinder = new FirmFinder(this);

            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
            ServicePointManager.ServerCertificateValidationCallback += new RemoteCertificateValidationCallback(delegate { return true; });

            this.FormClosing += delegate
            {
                try
                {
                    proxyserver.Stop();
                    Environment.Exit(Environment.ExitCode);
                }
                catch
                {

                }
            };
            try
            {
                proxyserver.CertificateManager.CreateRootCertificate(true);
                proxyserver.CertificateManager.TrustRootCertificate(true);

                proxyserver.ServerCertificateValidationCallback += Proxyserver_ServerCertificateValidationCallback;
                proxyserver.BeforeRequest += Proxyserver_BeforeRequest;
                proxyserver.BeforeResponse += Proxyserver_BeforeResponse;

                proxyserver.AddEndPoint(endpoint);

                proxyserver.Start();
            }
            catch (Exception ex)
            {
                textBox3.AppendText(ex.StackTrace);
                //MessageBox.Show(ex.Message, "ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            textBox3.ScrollBars = ScrollBars.Both;
            textBox3.Visible = false;
            textBox1.TextChanged += delegate
            {
                string text = textBox1.Text;
                int where = text.IndexOf("/full");
                if (where != -1)
                {
                    textBox1.Text = text.Substring(0, ++where);
                }
            };
            textBox4.TextChanged += delegate
            {
                string text = textBox4.Text;
                int where = text.IndexOf("/full");
                if (where != -1)
                {
                    textBox4.Text = text.Substring(0, ++where);
                }
            };

            textBox7.TextChanged += delegate
            {
                string text = textBox7.Text;
                int where = text.IndexOf("/full");
                if (where != -1)
                {
                    textBox7.Text = text.Substring(0, ++where);
                }
            };
        }

        private async Task Proxyserver_BeforeResponse(object sender, Titanium.Web.Proxy.EventArguments.SessionEventArgs e)
        {
            string reqeustURL = e.HttpClient.Request.Url;
            if (reqeustURL.Contains("filelist.xml"))
            {

                string basefirm = textBox1.Text, custfirm = textBox7.Text, preloadfirm = textBox4.Text;
                basefirm = basefirm.Substring(basefirm.IndexOf("TDS"));
                custfirm = custfirm.Substring(custfirm.IndexOf("TDS"));
                preloadfirm = preloadfirm.Substring(preloadfirm.IndexOf("TDS"));

                string tempurl = reqeustURL.Substring(reqeustURL.IndexOf("TDS"));
                if (_customData.LocalBase && tempurl.Contains(basefirm))
                {
                    string response = await e.GetResponseBodyAsString();
                    int where = response.IndexOf("package=");
                    if (where == -1)
                    {
                        MessageBox.Show("Couldn't load base package name, please contact developer if error continues", "ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    else
                    {
                        where += 9;
                        int finish = response.IndexOf('"', where);
                        CopyFile(textBox2.Text, _customData.LocalBaseDir, response.Substring(where, finish - where), 0);
                    }
                }
                else if (_customData.LocalCust && tempurl.Contains(custfirm))
                {
                    string response = await e.GetResponseBodyAsString();
                    int where = response.IndexOf("package=");
                    if (where == -1)
                    {
                        MessageBox.Show("Couldn't load cust package name, please contact developer if error continues", "ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    else
                    {
                        where += 9;
                        int finish = response.IndexOf('"', where);
                        CopyFile(textBox6.Text, _customData.LocalCustDir, response.Substring(where, finish - where), 1);
                    }
                }
                else if (_customData.LocalPreload && tempurl.Contains(preloadfirm))
                {
                    string response = await e.GetResponseBodyAsString();
                    int where = response.IndexOf("package=");
                    if (where == -1)
                    {
                        MessageBox.Show("Couldn't load preload package name, please contact developer if error continues", "ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    else
                    {
                        where += 9;
                        int finish = response.IndexOf('"', where);
                        CopyFile(textBox5.Text, _customData.LocalPreloadDir, response.Substring(where, finish - where), 2);
                    }
                }
            }
        }

        private string GetURLVersion(string url)
        {
            int where = url.IndexOf("/v");
            if(where == -1)
            {
                return "Unknown";
            }
            else
            {
                where += 2;
                int finish = url.IndexOf('/', where);
                return url.Substring(where, finish - where);
            }
        }
        private async Task Proxyserver_BeforeRequest(object sender, Titanium.Web.Proxy.EventArguments.SessionEventArgs e)
        {
            try
            {
                string reqeustURL = e.HttpClient.Request.Url;
                if(reqeustURL.Contains("query.hicloud.com") || reqeustURL.Contains("/TDS/data/files"))
                {
                    this.Invoke(new Action(() =>
                    {
                        textBox3.AppendText(e.HttpClient.Request.Url + Environment.NewLine);
                    }));
                    if (checkBox2.Checked)
                    {
                        string debug = e.HttpClient.Request.Url + " : " + Environment.NewLine;
                        List<HttpHeader> clientheaders = e.HttpClient.Request.Headers.GetAllHeaders();
                        for (int i = 0, j = clientheaders.Count; i < j; i++)
                        {
                            debug += clientheaders[i].Name + ": " + clientheaders[i].Value + Environment.NewLine;
                        }
                        if (e.HttpClient.Request.HasBody)
                        {
                            debug += Environment.NewLine + await e.GetRequestBodyAsString();
                        }
                        debug += Environment.NewLine + Environment.NewLine;
                        File.AppendAllText("logs.txt", debug);
                    }
                }
                if (reqeustURL.Contains("query.hicloud.com"))
                {
                    if (reqeustURL.Contains("CheckNewVersion.aspx"))
                    {
                        Dictionary<string, HttpHeader> Headers = new Dictionary<string, HttpHeader>();
                        Headers.Add("Content-Type", new HttpHeader("Content-Type", "application/xml;charset=UTF-8"));
                        e.Ok("<?xml version=\"1.0\" encoding=\"UTF-8\"?><root><status>1</status></root>", Headers, true);
                    }
                    else if (reqeustURL.Contains("CouplingReport.action"))
                    {
                        string resbody = await e.GetRequestBodyAsString();
                        int where = resbody.IndexOf("descinfo");
                        if (where != -1)
                        {
                            where += 13;
                            int finish = resbody.IndexOf("\",", where);
                            new Thread(() =>
                            {
                                MessageBox.Show(resbody.Substring(where, finish - where), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            }).Start();
                        }
                    }
                    else if (reqeustURL.Contains("authorize.action"))
                    {
                        WebClient client = new WebClient();
                        client.Headers.Set(HttpRequestHeader.Accept, "*/*");
                        client.Headers.Set(HttpRequestHeader.ContentType, "application/json;charset=UTF-8");
                        string updata = await e.GetRequestBodyAsString();
                        string respons = client.UploadString("https://query.hicloud.com:443/sp_ard_common/v1/authorize.action", updata);
                        Dictionary<string, HttpHeader> Headers = new Dictionary<string, HttpHeader>();
                        Headers.Add("Date", new HttpHeader("Date", client.ResponseHeaders[HttpResponseHeader.Date]));
                        Headers.Add("Content-Type", new HttpHeader("Content-Type", "text/plain;charset=UTF-8"));
                        Headers.Add("Server", new HttpHeader("Server", "elb"));
                        Headers.Add("X-XSS-Protection", new HttpHeader("X-XSS-Protection", "1; mode=block"));
                        Headers.Add("X-frame-options", new HttpHeader("X-frame-options", "SAMEORIGIN"));
                        Headers.Add("X-Content-Type-Options", new HttpHeader("X-Content-Type-Options", "nosniff"));
                        string manipulatedresponse = ManipulateData(respons);
                        e.Ok(manipulatedresponse, Headers);
                    }
                    else if (e.HttpClient.Request.HasBody)
                    {
                        string bodydata = await e.GetRequestBodyAsString();
                        int whereisit = bodydata.IndexOf("PackageType");
                        if (whereisit != -1)
                        {
                            whereisit += 16;
                            int finish = bodydata.IndexOf('"', whereisit);
                            string pacakgetype = bodydata.Substring(whereisit, finish - whereisit);
                            string opscheck = "full";
                            if (radioButton2.Checked)
                            {
                                opscheck = "hfull_switch";
                            }
                            else if (radioButton3.Checked)
                            {
                                opscheck = "full_back";
                            }
                            else if(radioButton4.Checked)
                            {
                                opscheck = "full_recovery";
                            }
                            if (pacakgetype == opscheck)
                            {
                                string responsedata = Encoding.UTF8.GetString(Properties.Resources.responsedata).Replace("\r\n", "");
                                bool Iveabase = (GetURLVersion(textBox1.Text) != "Unknown");
                                if (Iveabase)
                                {
                                    responsedata = responsedata.Replace("hasfullpackage", "0");
                                    if(_customData.CustomBase)
                                    {
                                        responsedata = responsedata.Replace("WriteVerionID", _customData.CustomBaseID);
                                    }
                                    else
                                    {
                                        responsedata = responsedata.Replace("WriteVerionID", GetURLVersion(textBox1.Text));
                                    }
                                    if(checkBox4.Checked)
                                        responsedata = responsedata.Replace("pointbase", "1");
                                    else
                                        responsedata = responsedata.Replace("pointbase", "0");

                                    responsedata = responsedata.Replace("basetype", textBox8.Text);
                                    responsedata = responsedata.Replace("VersionURL", textBox1.Text);
                                    responsedata = responsedata.Replace("Unknown1", textBox2.Text);
                                }
                                else
                                {
                                    responsedata = responsedata.Replace("hasfullpackage", "1");
                                }

                                if (checkBox1.Checked)
                                {
                                    if(_customData.CustomPreload)
                                    {
                                        responsedata = responsedata.Replace("WiteVerionID", _customData.CustomPreloadID);
                                    }
                                    else
                                    {
                                        responsedata = responsedata.Replace("WiteVerionID", GetURLVersion(textBox4.Text));
                                    }
                                    if (checkBox6.Checked)
                                    {
                                        if(Iveabase)
                                        {
                                            responsedata = responsedata.Replace("pointpreload", "2");
                                        }
                                        else
                                        {
                                            responsedata = responsedata.Replace("pointpreload", "1");
                                        }
                                    }
                                    else
                                        responsedata = responsedata.Replace("pointpreload", "0");
                                    responsedata = responsedata.Replace("VrsionURL", textBox4.Text);
                                    responsedata = responsedata.Replace("Unknown2", textBox5.Text);
                                    responsedata = responsedata.Replace("hasreloadedpackage", "0");
                                }
                                else
                                {
                                    responsedata = responsedata.Replace("hasreloadedpackage", "1");
                                }

                                if (checkBox3.Checked)
                                {
                                    if(_customData.CustomCust)
                                    {
                                        responsedata = responsedata.Replace("WteVerionID", _customData.CustomCustID);
                                    }
                                    else
                                    {
                                        responsedata = responsedata.Replace("WteVerionID", GetURLVersion(textBox7.Text));
                                    }
                                    if (checkBox5.Checked)
                                    {
                                        if (Iveabase)
                                        {
                                            responsedata = responsedata.Replace("pointcust", "2");
                                        }
                                        else
                                        {
                                            responsedata = responsedata.Replace("pointcust", "1");
                                        }
                                    }  
                                    else
                                        responsedata = responsedata.Replace("pointcust", "0");
                                    responsedata = responsedata.Replace("VrionURL", textBox7.Text);
                                    responsedata = responsedata.Replace("Unknown3", textBox6.Text);
                                    responsedata = responsedata.Replace("hascustpackage", "0");
                                }
                                else
                                {
                                    responsedata = responsedata.Replace("hascustpackage", "1");
                                }
                                Dictionary<string, HttpHeader> Headers = new Dictionary<string, HttpHeader>();
                                Headers.Add("Content-Type", new HttpHeader("Content-Type", "application/json;charset=utf8"));
                                e.Ok(responsedata, Headers, true);
                            }
                            else
                            {
                                string responsedata = Encoding.UTF8.GetString(Properties.Resources.emptyresponse).Replace("\r\n", "");
                                Dictionary<string, HttpHeader> Headers = new Dictionary<string, HttpHeader>();
                                Headers.Add("Content-Type", new HttpHeader("Content-Type", "application/json;charset=utf8"));
                                e.Ok(responsedata, Headers, true);
                            }
                        }
                    }
                }
                
            }
            catch(Exception ex)
            {
                this.Invoke(new Action(() =>
                {
                    textBox3.AppendText(ex.StackTrace + Environment.NewLine);
                }));
                MessageBox.Show(ex.Message, "ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private Task Proxyserver_ServerCertificateValidationCallback(object sender, Titanium.Web.Proxy.EventArguments.CertificateValidationEventArgs e)
        {
            e.IsValid = true;
            return Task.CompletedTask;
        }

        private void button3_Click(object sender, EventArgs e)
        {
            this.Size = new System.Drawing.Size(Width, Width - 50);
            textBox3.Location = new System.Drawing.Point(10, 168);
            textBox3.Size = new System.Drawing.Size(Width - 30, Width - 263);
            textBox3.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
            textBox3.Visible = true;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Filter = "Executable File|*.exe";
            dialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86) + @"\HiSuite";
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                PatchHISuite(dialog.FileName);
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Filter = "Dynamic-link library|*.dll";
            dialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86) + @"\HiSuite";
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                Patch(dialog.FileName);
            }
        }
        private void PatchHISuite(string filename)
        {
            string filedata = File.ReadAllText(filename, Encoding.Default);

            if (PatcherReplace(new byte[] { 0xE8, 0xC9, 0x4D, 0xD8, 0xFF, 0x85, 0xC0, 0x0F, 0x84, 0xB1, 0x00, 0x00, 0x00 }, new byte[] { 0xE8, 0xC9, 0x4D, 0xD8, 0xFF, 0x85, 0xC0, 0x0F, 0x85, 0xB1, 0x00, 0x00, 0x00 }, ref filedata))
            {
                SaveFileDialog dialog = new SaveFileDialog();
                dialog.Filter = "Executable File|*.exe";
                dialog.FileName = "HISuite2.exe";
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        File.WriteAllText(dialog.FileName, filedata, Encoding.Default);
                        MessageBox.Show("Successfully Patched!", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception e)
                    {
                        MessageBox.Show(e.Message, "ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
            else
            {
                MessageBox.Show("This file is already patched.", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
        private void Patch(string filename)
        {
            string filedata = File.ReadAllText(filename, Encoding.Default);

            if (PatcherReplace(new byte[] { 0x6A, 0x00, 0x6A, 0x01, 0x51, 0xFF, 0x15, 0xC4, 0x32, 0x01, 0x10 }, new byte[] { 0x6A, 0x00, 0x6A, 0x00, 0x51, 0xFF, 0x15, 0xC4, 0x32, 0x01, 0x10 }, ref filedata))
            {
                    if (PatcherReplace(new byte[] { 0x71, 0x75, 0x65, 0x72, 0x79, 0x2E, 0x68, 0x69, 0x63, 0x6C, 0x6F, 0x75, 0x64, 0x2E, 0x63, 0x6F, 0x6D }, new byte[] { 0x70, 0x70, 0x70, 0x70, 0x79, 0x2E, 0x68, 0x69, 0x63, 0x6C, 0x6F, 0x75, 0x64, 0x2E, 0x63, 0x6F, 0x6D }, ref filedata))
                    {
                        SaveFileDialog dialog = new SaveFileDialog();
                        dialog.Filter = "Dynamic-link library|*.dll";
                        dialog.FileName = "httpcomponent.dll";
                        if (dialog.ShowDialog() == DialogResult.OK)
                        {
                            try
                            {
                                File.WriteAllText(dialog.FileName, filedata, Encoding.Default);
                                MessageBox.Show("Successfully Patched!", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            }
                            catch(Exception e)
                            {
                                MessageBox.Show(e.Message, "ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            }
                        }
                    }
                    else
                    {
                        MessageBox.Show("Some errors occured in the patching process!", "ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
            }
            else
            {
                MessageBox.Show("This file is already patched.", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
        private bool PatcherReplace(byte[] data, byte[] replacewith, ref string basedata)
        {
            string finddata = Encoding.Default.GetString(data);
            if (basedata.Contains(finddata))
            {
                basedata = basedata.Replace(finddata, Encoding.Default.GetString(replacewith));
                return true;
            }
            else
            {
                return false;
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            if(!firmFinder.Visible)
            {
                firmFinder.Show(this);
                firmFinder.BringToFront();
                firmFinder.Focus();
            }          
        }
        private bool CopyingBase = false, CopyingCust = false, CopyingPreload = false;

        private void button5_Click(object sender, EventArgs e)
        {
            Process.Start("https://consumer.huawei.com/en/support/hisuite/");
        }

        private void CopyFile(string romname, string filename, string packagename, int filekind)
        {
            if (filekind == 0)
            {
                if (CopyingBase)
                    return;
                CopyingBase = true;
            }
            else if (filekind == 1)
            {
                if (CopyingCust)
                    return;
                CopyingCust = true;
            }
            else if (filekind == 2)
            {
                if (CopyingPreload)
                    return;
                CopyingPreload = true;
            }
            new Thread(() =>
            {
                Progress progress = new Progress("Copying File For " + romname);
                bool finished = false;
                Thread CopyThread = new Thread(() =>
                {
                    CopyItPlease(progress, romname, filename, packagename);
                    finished = true;
                    if(progress.Visible)
                    {
                        progress.Close();
                    }
                    else
                    {
                        progress.Load += delegate
                        {
                            progress.Close();
                        };
                    }
                });
                progress.FormClosing += delegate
                {
                    if (filekind == 0)
                    {
                        CopyingBase = false;
                    }
                    else if (filekind == 1)
                    {
                        CopyingCust = false;
                    }
                    else if (filekind == 2)
                    {
                        CopyingPreload = false;
                    }
                    if (!finished)
                    {
                        CopyThread.Abort();
                    }
                };
                CopyThread.Start();
                progress.ShowDialog(this);
            }).Start();
        }
        private void CopyItPlease(Progress progress, string romname, string filename, string packagename)
        {
            try
            {
                string dir = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + @"\HiSuite\ROM\" + romname + @"\full\" + packagename;
                
                if(File.Exists(dir))
                {
                    long victimlen = new FileInfo(dir).Length, mainfilelen = new FileInfo(filename).Length;
                    if(victimlen != mainfilelen)
                    {
                        File.Delete(dir);
                    }
                    else
                    {
                        return;
                    }
                }
                string Directoryname = Path.GetDirectoryName(dir);
                if(!Directory.Exists(Directoryname))
                {
                    Directory.CreateDirectory(Directoryname);
                }
                using (FileStream stream = File.OpenRead(filename))
                {
                    long totallen = stream.Length;
                    using(FileStream write = File.OpenWrite(dir))
                    {
                        long writtenlen = 0;
                        int readdata = 4096;
                        byte[] data = new byte[readdata];
                        while((readdata = stream.Read(data, 0, data.Length)) > 0)
                        {
                            write.Write(data, 0, readdata);
                            writtenlen += readdata;
                            int percentage = (int)((writtenlen * 100) / totallen);
                            if((percentage % 5) == 0)
                            {
                                this.Invoke(new Action(() =>
                                {
                                    progress.SetProgress(percentage);
                                }));
                            }
                        }
                    }
                }
            }
            catch(Exception e)
            {
                if(!e.Message.StartsWith("Thread was being"))
                MessageBox.Show(e.Message, "ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public string ManipulateData(string response)
        {
            int where = response.IndexOf("data=");
            where += 5;
            int finish = response.IndexOf("&", where);
            string find = response.Substring(where, finish - where);

            string data = Encoding.UTF8.GetString(Convert.FromBase64String(find));

            int whereisit = 0;
            while((whereisit = data.IndexOf("versionId", whereisit)) != -1)
            {
                whereisit += 12;
                int finishez = data.IndexOf('"', whereisit);
                string versionID = data.Substring(whereisit, finishez - whereisit);
                whereisit = finishez + 12;
                if(data[whereisit] != '0')
                {
                    whereisit += 2;

                    string baseversion = GetURLVersion(textBox1.Text),
                        preloadversion = GetURLVersion(textBox4.Text)
                    ;
                    if(baseversion == versionID)
                    {
                        data = data.Insert(whereisit, ",\"versionNumber\":\"" + textBox2.Text + "\"");
                    }
                    else if(preloadversion == versionID)
                    {
                        data = data.Insert(whereisit, ",\"versionNumber\":\"" + textBox5.Text + "\"");
                    }
                    else
                    {
                        data = data.Insert(whereisit, ",\"versionNumber\":\"" + textBox6.Text + "\"");
                    }
                }
            }
            data = data.Replace("status\":\"1", "status\":\"0").Replace("status\":\"2", "status\":\"0");

            response = response.Remove(where, finish - where);
            response = response.Insert(where, Convert.ToBase64String(Encoding.UTF8.GetBytes(data)));
            return response;
        }
    }
}
