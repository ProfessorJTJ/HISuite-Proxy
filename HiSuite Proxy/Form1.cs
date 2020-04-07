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
using System.Net.Sockets;
using System.Net.Security;

namespace HiSuite_Proxy
{
    public partial class Form1 : Form
    {
        ProxyServer proxyserver = new ProxyServer();
        public Form1()
        {
            InitializeComponent();
            this.FormClosing += delegate
            {
                proxyserver.Stop();
            };

            try
            {
                proxyserver.CertificateManager.CreateRootCertificate(true);
                proxyserver.CertificateManager.TrustRootCertificate(true);

                proxyserver.ServerCertificateValidationCallback += Proxyserver_ServerCertificateValidationCallback;
                proxyserver.BeforeRequest += Proxyserver_BeforeRequest;
                ExplicitProxyEndPoint endpoint = new ExplicitProxyEndPoint(IPAddress.Any, 7777);

                proxyserver.AddEndPoint(endpoint);

                proxyserver.Start();
            }
            catch(Exception ex)
            {
                textBox3.AppendText(ex.StackTrace);
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
        }

        private string GetURLVersion(string url)
        {
            int where = url.IndexOf("/v");
            if(where == -1)
            {
                return "";
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
                string reqeustURL = e.HttpClient.Request.Url;
                if (reqeustURL.Contains("query.hicloud.com"))
                {
                    if (reqeustURL.Contains("CouplingReport.action"))
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
                        e.Ok(respons, Headers, true);
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
                            if (pacakgetype == opscheck)
                            {
                                string responsedata = Encoding.UTF8.GetString(Properties.Resources.responsedata).Replace("\r\n", "").Replace("WriteVerionID", GetURLVersion(textBox1.Text)).Replace("VersionURL", textBox1.Text).Replace("WiteVerionID", GetURLVersion(textBox4.Text)).Replace("VrsionURL", textBox4.Text);
                                if (checkBox1.Checked)
                                {
                                    responsedata = responsedata.Replace("hasreloadedpackage", "0");
                                }
                                else
                                {
                                    responsedata = responsedata.Replace("hasreloadedpackage", "1");
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
            this.Size = new System.Drawing.Size(410, 430);
            textBox3.Location = new System.Drawing.Point(12, 153);
            textBox3.Size = new System.Drawing.Size(374, 232);
            textBox3.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
            textBox3.Visible = true;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Process.Start("https://consumer.huawei.com/en/support/hisuite/");
        }

        private void button2_Click(object sender, EventArgs e)
        {
            SaveFileDialog dialog = new SaveFileDialog();
            dialog.Filter = "Dynamic-link library|*.dll";
            dialog.FileName = "httpcomponent.dll";
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                File.WriteAllBytes(dialog.FileName, Properties.Resources.httpcomponent);
            }
        }
    }
}
