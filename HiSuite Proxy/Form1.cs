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

namespace HiSuite_Proxy
{
    public partial class Form1 : Form
    {
        ProxyServer proxyserver = new ProxyServer();
        ExplicitProxyEndPoint endpoint = new ExplicitProxyEndPoint(IPAddress.Any, 7777);
        bool xdaremoveads = false;
        NotifyIcon mytoolbar = new NotifyIcon();
        public Form1()
        {
            InitializeComponent();
            mytoolbar.Visible = false;
            mytoolbar.Text = "HISuite Proxy";
            mytoolbar.Icon = this.Icon;
            MenuItem item = new MenuItem();
            item.Text = "Exit";
            item.Click += delegate
            {
                mytoolbar.Visible = false;
                this.Close();
            };
            MenuItem[] menuItem = { item };

            mytoolbar.ContextMenu = new ContextMenu(menuItem);
            mytoolbar.DoubleClick += delegate
            {
                mytoolbar.Visible = false;
                this.Show();
                WindowState = FormWindowState.Normal;
                this.BringToFront();
                this.Focus();
            };
            this.FormClosing += delegate
            {
                try
                {
                    mytoolbar.Visible = false;
                    mytoolbar.Dispose();
                    proxyserver.DisableSystemProxy(ProxyProtocolType.AllHttp);
                    proxyserver.Stop();
                }
                catch
                {

                }
            };
            this.Resize += delegate
            {
                if(WindowState == FormWindowState.Minimized)
                {
                    mytoolbar.Visible = true;

                    this.Hide();
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
            catch(Exception ex)
            {
                textBox3.AppendText(ex.StackTrace);
                MessageBox.Show(ex.Message, "ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
            checkBox4.CheckedChanged += delegate
            {
                if(checkBox4.Checked)
                {
                    File.WriteAllText("xdanoads", "1");
                    xdaremoveads = true;
                    proxyserver.SetAsSystemProxy(endpoint, ProxyProtocolType.AllHttp);
                }
                else
                {
                    if(File.Exists("xdanoads"))
                    {
                        File.Delete("xdanoads");
                    }
                    xdaremoveads = false;
                    proxyserver.DisableSystemProxy(ProxyProtocolType.AllHttp);
                }
            };
            if(File.Exists("xdanoads"))
            {
                proxyserver.SetAsSystemProxy(endpoint, ProxyProtocolType.AllHttp);
                xdaremoveads = true;
                checkBox4.Checked = true;
            }
        }

        private async Task Proxyserver_BeforeResponse(object sender, Titanium.Web.Proxy.EventArguments.SessionEventArgs e)
        {
            if(xdaremoveads)
            {
                string reqeustURL = e.HttpClient.Request.Url;
                if (reqeustURL.Contains("xda-developers.com"))
                {
                    string response = await e.GetResponseBodyAsString();
                    if(response.Contains("var googletag"))
                    {
                        e.SetResponseBodyString(response + "\r\n" + "<script>googletag.display = function(arguments) { return true; };\r\ngoogletag.enableServices = function() { return true; };</script>");
                    }
                }
                else if(reqeustURL.Contains("gpt/pubads_impl"))
                {
                    e.SetResponseBodyString("");
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
                        Headers.Add("Content-Type", new HttpHeader("Content-Type", "text/plain;charset=UTF-8"));
                        Headers.Add("X-Content-Type-Options", new HttpHeader("X-Content-Type-Options", "nosniff"));
                        Headers.Add("Server", new HttpHeader("Server", "elb"));
                        Headers.Add("X-XSS-Protection", new HttpHeader("X-XSS-Protection", "1; mode=block"));
                        e.Ok(respons, Headers);
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
                                if(GetURLVersion(textBox1.Text) != "Unknown")
                                {
                                    responsedata = responsedata.Replace("hasfullpackage", "0");
                                    responsedata = responsedata.Replace("WriteVerionID", GetURLVersion(textBox1.Text));
                                    responsedata = responsedata.Replace("VersionURL", textBox1.Text);
                                    responsedata = responsedata.Replace("Unknown1", textBox2.Text);
                                }
                                else
                                {
                                    responsedata = responsedata.Replace("hasfullpackage", "1");
                                }

                                if (checkBox1.Checked)
                                {
                                    responsedata = responsedata.Replace("WiteVerionID", GetURLVersion(textBox4.Text));
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
                                    responsedata = responsedata.Replace("WteVerionID", GetURLVersion(textBox7.Text));
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
            }
        }
        private Task Proxyserver_ServerCertificateValidationCallback(object sender, Titanium.Web.Proxy.EventArguments.CertificateValidationEventArgs e)
        {
            e.IsValid = true;
            return Task.CompletedTask;
        }

        private void button3_Click(object sender, EventArgs e)
        {
            this.Size = new System.Drawing.Size(430, 450);
            textBox3.Location = new System.Drawing.Point(10, 168);
            textBox3.Size = new System.Drawing.Size(400, 237);
            textBox3.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
            textBox3.Visible = true;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Process.Start("https://consumer.huawei.com/en/support/hisuite/");
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
    }
}
