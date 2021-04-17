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

using System.Security.Cryptography;

using System.IO.Compression;

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

        private class PackageData
        {
            public string 
                PackageFile,
                PackageSize,
                PackageName,
                PackageMD5,
                PackageSha256;
            public PackageData(string PackageFile, string PackageName, string PackageSize, string PackageMD5, string PackageSha256)
            {
                this.PackageFile = PackageFile;
                this.PackageName = PackageName;
                this.PackageSize = PackageSize;
                this.PackageMD5 = PackageMD5;
                this.PackageSha256 = PackageSha256;
            }
        }
        PackageData basePKGData = null, custPKGData = null, preloadPGKData = null;
        public Form1(string[] arguments)
        {
            if (arguments.Length > 0 && arguments.Length == 2)
            {
                ReplaceComponent(arguments[0], arguments[1]);
                Environment.Exit(Environment.ExitCode);
                return;
            }
            else if(arguments.Length == 1 && arguments[0] == "SETUP")
            {
                SetUP setUP = new SetUP(this);
                setUP.ShowDialog();
                this.Load += delegate
                {
                    this.Close();
                };
                return;
            }
            InitializeComponent();

            textBox8.TextAlign = HorizontalAlignment.Center;

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
            textBox1.TextChanged += delegate
            {
                string text = textBox1.Text;
                int where = text.IndexOf("/full");
                if (where != -1)
                {
                    textBox1.Text = text.Substring(0, ++where);
                }
                if(!text.Contains("/TDS/data/files"))
                {
                    textBox9.Enabled = true;
                }
                else
                {
                    textBox9.Enabled = false;
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

                if (!text.Contains("/TDS/data/files"))
                {
                    textBox11.Enabled = true;
                }
                else
                {
                    textBox11.Enabled = false;
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
                if (!text.Contains("/TDS/data/files"))
                {
                    textBox10.Enabled = true;
                }
                else
                {
                    textBox10.Enabled = false;
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
        private string GetURLVersion(string url, int type = 0)
        {
            if(url.Contains("/TDS/data/files"))
            {
                int where = url.IndexOf("/v");
                if (where == -1)
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
            else
            {
                if (type == 0)
                    return (textBox9.Text == "0") ? ("Unknown") : (textBox9.Text);
                else if (type == 1) // CUST
                    return (textBox10.Text == "0") ? ("Unknown") : (textBox10.Text);
                else if (type == 2) // Preload
                    return (textBox11.Text == "0") ? ("Unknown") : (textBox11.Text);
                else
                    return "Unknown";
            }
            
        }
        private async Task Proxyserver_BeforeRequest(object sender, Titanium.Web.Proxy.EventArguments.SessionEventArgs e)
        {
            try
            {
                string reqeustURL = e.HttpClient.Request.Url;
                if(reqeustURL.EndsWith(":7777/addROM.txt"))
                {
                    if(e.HttpClient.Request.HasBody)
                    {
                        string Body = await e.GetRequestBodyAsString();
                        string[] BodyData = Body.Split('|');
                        if(BodyData.Length > 2)
                        {
                            if (BodyData[1].Contains("-PRELOAD "))
                            {
                                textBox4.Text = BodyData[2];
                                textBox5.Text = BodyData[1];
                                textBox11.Text = BodyData[0];
                                checkBox1.Checked = true;
                                //Activate();

                                Dictionary<string, HttpHeader> Headers = new Dictionary<string, HttpHeader>();
                                Headers.Add("Access-Control-Allow-Origin", new HttpHeader("Access-Control-Allow-Origin", e.HttpClient.Request.Headers.Headers["Origin"].Value));
                                Headers.Add("Content-Type", new HttpHeader("Content-Type", "text/plain"));
                                e.Ok("OK", Headers);
                            }
                            else if (BodyData[1].Contains("-CUST "))
                            {
                                textBox7.Text = BodyData[2];
                                textBox6.Text = BodyData[1];
                                textBox10.Text = BodyData[0];
                                checkBox3.Checked = true;
                                //Activate();

                                Dictionary<string, HttpHeader> Headers = new Dictionary<string, HttpHeader>();
                                Headers.Add("Access-Control-Allow-Origin", new HttpHeader("Access-Control-Allow-Origin", e.HttpClient.Request.Headers.Headers["Origin"].Value));
                                Headers.Add("Content-Type", new HttpHeader("Content-Type", "text/plain"));
                                e.Ok("OK", Headers);
                            }
                            else
                            {
                                textBox1.Text = BodyData[2];
                                textBox2.Text = BodyData[1];
                                textBox9.Text = BodyData[0];
                                //Activate();

                                Dictionary<string, HttpHeader> Headers = new Dictionary<string, HttpHeader>();
                                Headers.Add("Access-Control-Allow-Origin", new HttpHeader("Access-Control-Allow-Origin", e.HttpClient.Request.Headers.Headers["Origin"].Value));
                                Headers.Add("Content-Type", new HttpHeader("Content-Type", "text/plain"));
                                e.Ok("OK", Headers);
                            }
                        }
                        else
                        {
                            Dictionary<string, HttpHeader> Headers = new Dictionary<string, HttpHeader>();
                            Headers.Add("Access-Control-Allow-Origin", new HttpHeader("Access-Control-Allow-Origin", e.HttpClient.Request.Headers.Headers["Origin"].Value));
                            Headers.Add("Content-Type", new HttpHeader("Content-Type", "text/plain"));
                            e.Ok("ERROR", Headers);
                        }
                    }
                    else
                    {
                        Dictionary<string, HttpHeader> Headers = new Dictionary<string, HttpHeader>();
                        Headers.Add("Access-Control-Allow-Origin", new HttpHeader("Access-Control-Allow-Origin", e.HttpClient.Request.Headers.Headers["Origin"].Value));
                        Headers.Add("Content-Type", new HttpHeader("Content-Type", "text/plain"));
                        e.Ok("ERROR", Headers);
                    }
                    return;
                }
                if (reqeustURL.Contains("query.hicloud.com") || reqeustURL.Contains("/TDS/data/files") || reqeustURL.Contains("update.dbankcdn.com"))
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
                if (reqeustURL.Contains("checkHiSuiteConnection"))
                {
                    e.Ok("Success");
                }
                else if (reqeustURL.Contains("query.hicloud.com"))
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
                        string respons = "0";
                        bool forceAuthBridge = checkBox7.Checked;
                        if (forceAuthBridge)
                        {
                            int where = updata.IndexOf("\"deviceCertificate");
                            if (where != -1)
                            {
                                int finish = updata.IndexOf("\",", where) + 2;
                                finish = updata.IndexOf('"', finish);
                                updata = updata.Remove(where, finish - where);
                            }

                            where = updata.IndexOf("\"keyAttestation");
                            if (where != -1)
                            {
                                int finish = updata.IndexOf("\",", where) + 2;
                                finish = updata.IndexOf('"', finish);
                                updata = updata.Remove(where, finish - where);
                            }
                        }
                        if (updata.Contains("\"2\"") && !updata.Contains("deviceCertificate"))
                        {
                            if (forceAuthBridge || MessageBox.Show("Apparently your phone is soft re-branded so normal authentication is going to fail!\r\n\r\nDo you want to use authentication bridge?", "Alert", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning) == DialogResult.OK)
                            {
                                AuthBridge authenticate = new AuthBridge(updata);
                                authenticate.ShowDialog();
                                if (authenticate.Success)
                                {
                                    respons = authenticate.responsedata;
                                }
                                else
                                {
                                    respons = "0";
                                }
                            }
                            else
                            {
                                respons = "0";
                            }
                        }
                        if (respons == "0")
                        {
                            try
                            {
                                respons = client.UploadString("https://query.hicloud.com:443/sp_ard_common/v1/authorize.action", updata);
                            }
                            catch (Exception ex)
                            {
                                respons = null;
                                new Thread(() =>
                                {
                                    MessageBox.Show(ex.Message + "\r\n\r\n Please check your internet connection", "Authorization Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                }).Start();
                            }
                        }
                        Dictionary<string, HttpHeader> Headers = new Dictionary<string, HttpHeader>();
                        if (respons == null)
                            respons = "";
                        Headers.Add("Content-Type", new HttpHeader("Content-Type", "text/plain;charset=UTF-8"));
                        Headers.Add("Server", new HttpHeader("Server", "elb"));
                        Headers.Add("X-XSS-Protection", new HttpHeader("X-XSS-Protection", "1; mode=block"));
                        Headers.Add("X-frame-options", new HttpHeader("X-frame-options", "SAMEORIGIN"));
                        Headers.Add("X-Content-Type-Options", new HttpHeader("X-Content-Type-Options", "nosniff"));
                        if (respons.Length > 2)
                        {
                            if (!CheckAuthentication(updata, respons))
                            {
                                respons = "";
                            }
                        }
                        e.Ok(respons, Headers);
                    }
                    else if (e.HttpClient.Request.HasBody)
                    {
                        string bodydata = await e.GetRequestBodyAsString();
                        int whereisit = bodydata.IndexOf("PackageType");
                        if (whereisit != -1)
                        {
                            if (textBox2.Text.Length < 2)
                                textBox2.Text = "Unknown";
                            if (textBox6.Text.Length < 2)
                                textBox6.Text = "Unknown";
                            if (textBox5.Text.Length < 2)
                                textBox5.Text = "Unknown";
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
                            else if (radioButton4.Checked)
                            {
                                opscheck = "full_recovery";
                            }
                            if (pacakgetype == opscheck)
                            {
                                string responsedata = Encoding.UTF8.GetString(Properties.Resources.responsedata).Replace("\r\n", "");
                                if(checkBox8.Checked)
                                {
                                    responsedata = Encoding.UTF8.GetString(Properties.Resources.oldresponse).Replace("\r\n", "");
                                    bool Iveabase = (GetURLVersion(textBox1.Text, 0) != "Unknown");
                                    if (Iveabase)
                                    {
                                        responsedata = responsedata.Replace("hasfullpackage", "0");
                                        if (_customData.CustomBase)
                                        {
                                            responsedata = responsedata.Replace("WriteVerionID", _customData.CustomBaseID);
                                        }
                                        else
                                        {
                                            responsedata = responsedata.Replace("WriteVerionID", GetURLVersion(textBox1.Text, 0));
                                        }
                                        if (checkBox4.Checked)
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
                                }
                                else
                                {
                                    bool Iveabase = (GetURLVersion(textBox1.Text, 0) != "Unknown");
                                    if (Iveabase)
                                    {
                                        responsedata = responsedata.Replace("hasfullpackage", "0");
                                        if (_customData.CustomBase)
                                        {
                                            responsedata = responsedata.Replace("WriteVerionID", _customData.CustomBaseID);
                                        }
                                        else
                                        {
                                            responsedata = responsedata.Replace("WriteVerionID", GetURLVersion(textBox1.Text, 0));
                                        }
                                        if (checkBox4.Checked)
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
                                        if (_customData.CustomPreload)
                                        {
                                            responsedata = responsedata.Replace("WiteVerionID", _customData.CustomPreloadID);
                                        }
                                        else
                                        {
                                            responsedata = responsedata.Replace("WiteVerionID", GetURLVersion(textBox4.Text, 2));
                                        }
                                        if (checkBox6.Checked)
                                        {
                                            if (Iveabase)
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
                                        if (_customData.CustomCust)
                                        {
                                            responsedata = responsedata.Replace("WteVerionID", _customData.CustomCustID);
                                        }
                                        else
                                        {
                                            responsedata = responsedata.Replace("WteVerionID", GetURLVersion(textBox7.Text, 1));
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
                else if (reqeustURL.Contains("update.dbankcdn.com") && reqeustURL.Contains("/TDS/data/files"))
                {
                    if (reqeustURL.EndsWith("filelist.xml"))
                    {
                        string
                            requestVersion = GetURLVersion(reqeustURL),
                            baseVersionn = GetURLVersion(textBox1.Text, 0),
                            custVersionn = GetURLVersion(textBox7.Text, 1),
                            preloadVersionn = GetURLVersion(textBox4.Text, 2);

                        /*if (_customData.CustomBase)
                            baseVersionn = _customData.CustomBaseID;

                        if (_customData.CustomCust)
                            custVersionn = _customData.CustomCustID;

                        if (_customData.CustomPreload)
                            custVersionn = _customData.CustomPreloadID;*/
                        if (requestVersion == baseVersionn && basePKGData != null)
                        {
                            string responsefile = Properties.Resources.filelist.Replace("changelog", "changelog_base");
                            responsefile = responsefile.Replace("firmkind", "base");
                            responsefile = responsefile.Replace("firmfile", basePKGData.PackageFile);

                            responsefile = responsefile.Replace("firmMD5", basePKGData.PackageMD5);
                            responsefile = responsefile.Replace("firmSHA256", basePKGData.PackageSha256);
                            responsefile = responsefile.Replace("firmsize", basePKGData.PackageSize);

                            Dictionary<string, HttpHeader> Headers = new Dictionary<string, HttpHeader>();
                            Headers.Add("Content-Type", new HttpHeader("Content-Type", "text/xml"));

                            e.Ok(responsefile, Headers, true);
                        }
                        else if (requestVersion == custVersionn && custPKGData != null)
                        {
                            string responsefile = Properties.Resources.filelist;
                            responsefile = responsefile.Replace("firmkind", "cust");
                            responsefile = responsefile.Replace("firmfile", custPKGData.PackageFile);

                            responsefile = responsefile.Replace("firmMD5", custPKGData.PackageMD5);
                            responsefile = responsefile.Replace("firmSHA256", custPKGData.PackageSha256);
                            responsefile = responsefile.Replace("firmsize", custPKGData.PackageSize);

                            Dictionary<string, HttpHeader> Headers = new Dictionary<string, HttpHeader>();
                            Headers.Add("Content-Type", new HttpHeader("Content-Type", "text/xml"));

                            e.Ok(responsefile, Headers, true);
                        }
                        else if (requestVersion == preloadVersionn && preloadPGKData != null)
                        {
                            string responsefile = Properties.Resources.filelist;
                            responsefile = responsefile.Replace("firmkind", "preload");
                            responsefile = responsefile.Replace("firmfile", preloadPGKData.PackageFile);

                            responsefile = responsefile.Replace("firmMD5", preloadPGKData.PackageMD5);
                            responsefile = responsefile.Replace("firmSHA256", preloadPGKData.PackageSha256);
                            responsefile = responsefile.Replace("firmsize", preloadPGKData.PackageSize);

                            Dictionary<string, HttpHeader> Headers = new Dictionary<string, HttpHeader>();
                            Headers.Add("Content-Type", new HttpHeader("Content-Type", "text/xml"));

                            e.Ok(responsefile, Headers, true);
                        }
                    }
                    else if (reqeustURL.EndsWith("changelog_base.xml") || reqeustURL.EndsWith("changelog.xml"))
                    {
                        Dictionary<string, HttpHeader> Headers = new Dictionary<string, HttpHeader>();
                        Headers.Add("Content-Type", new HttpHeader("Content-Type", "text/xml"));

                        e.Ok(Properties.Resources.changelog, Headers, true);
                    }
                }
                
            }
            catch (Exception ex)
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

        private void button1_Click(object sender, EventArgs e)
        {
            Process.Start("https://github.com/ProfessorJTJ/HISuite-Proxy/releases/download/1.8.8/HiSuite_10.0.1.100_OVE.zip");
        }

        public bool ReplaceComponent(string rawfile, string patchedfile, bool systemadmin = false, bool ReplaceOriginalFile = true, string NewFileDirectory = null)
        {
            Process[] hisuiteopen = Process.GetProcessesByName("HiSuite");
            if(hisuiteopen.Length > 0)
            {
                hisuiteopen[0].Kill();
                Thread.Sleep(1500);
            }
            try
            {
                if(ReplaceOriginalFile)
                {
                    string targetFile = rawfile.Replace(".dll", ".bak");
                    if (File.Exists(targetFile))
                        File.Delete(targetFile);
                    File.Move(rawfile, targetFile);
                    File.Move(patchedfile, rawfile);
                }
                else
                {
                    if (File.Exists(NewFileDirectory))
                        File.Delete(NewFileDirectory);
                    File.Move(patchedfile, NewFileDirectory);
                }
                if(!systemadmin)
                {
                    MessageBox.Show("Successfully Patched!", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                return true;
            }
            catch(Exception e)
            {
                if (!systemadmin)
                {
                    MessageBox.Show(e.Message);
                }
            }
            return false;
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
        public bool Patch(string filename, bool systemadmin = false)
        {

            byte[] finddata = new byte[] { 0x6A, 0x00, 0x6A, 0x01, 0x51, 0xFF, 0x15, 0xC4, 0x32, 0x01, 0x10 }, replacedata = new byte[] { 0x6A, 0x00, 0x6A, 0x00, 0x51, 0xFF, 0x15, 0xC4, 0x32, 0x01, 0x10 };
            bool newHiSuite = false;
            try
            {
                string settings = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86) + @"\HiSuite\RunInfo.ini";
                if(File.ReadAllText(settings).Contains("version=10.1.0.550"))
                {
                    finddata = new byte[] { 0x6A, 0x00, 0x6A, 0x01, 0xFF, 0x35, 0x08, 0xD6, 0x02, 0x10 };
                    replacedata = new byte[] { 0x6A, 0x00, 0x6A, 0x00, 0xFF, 0x35, 0x08, 0xD6, 0x02, 0x10 };
                }
                else if (File.ReadAllText(settings).Contains("version=11.0.0.510"))
                {
                    //6A 00 6A 01 FF 35 70 68 03 10
                    finddata = new byte[] { 0x6A, 0x00, 0x6A, 0x01, 0xFF, 0x35, 0x70, 0x68, 0x03, 0x10 };
                    replacedata = new byte[] { 0x6A, 0x00, 0x6A, 0x00, 0xFF, 0x35, 0x70, 0x68, 0x03, 0x10 };
                    newHiSuite = true;
                    //return false;
                }
            }
            catch
            {

            }

            byte[] filedata = File.ReadAllBytes(filename);

            if (PatcherReplace(finddata, replacedata, ref filedata))
            {
                if ((newHiSuite && PatcherReplace(new byte[] { 0xE8, 0x3C, 0x00, 0x00, 0x00, 0x83, 0xF8, 0x01 }, new byte[] { 0xE8, 0x3C, 0x00, 0x00, 0x00, 0x83, 0xF8, 0x05 }, ref filedata))
                    || PatcherReplace(new byte[] { 0x71, 0x75, 0x65, 0x72, 0x79, 0x2E, 0x68, 0x69, 0x63, 0x6C, 0x6F, 0x75, 0x64, 0x2E, 0x63, 0x6F, 0x6D }, new byte[] { 0x70, 0x70, 0x70, 0x70, 0x79, 0x2E, 0x68, 0x69, 0x63, 0x6C, 0x6F, 0x75, 0x64, 0x2E, 0x63, 0x6F, 0x6D }, ref filedata))
                {
                    string tempfile = Path.GetTempPath() + "httpcomponent.dll";
                    File.WriteAllBytes(tempfile, filedata);
                    if(!systemadmin)
                    {
                        Process startprogram = new Process();
                        startprogram.StartInfo.FileName = Process.GetCurrentProcess().MainModule.FileName;
                        startprogram.StartInfo.Arguments = "\"" + filename + "\" \"" + tempfile + "\"";
                        startprogram.StartInfo.Verb = "runas";
                        try
                        {
                            startprogram.Start();
                        }
                        catch
                        {

                        }
                    }
                    else
                    {
                        if (newHiSuite)
                        {
                            string HISuiteDir = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86) + @"\HiSuite\";
                            bool succeded1 = ReplaceComponent(null, tempfile, true, false, HISuiteDir + "httpcomponenb.dll");

                            filedata = File.ReadAllBytes(HISuiteDir + "HiSuite.exe");

                            finddata = new byte[] { 0x68, 0x74, 0x74, 0x70, 0x63, 0x6f, 0x6d, 0x70, 0x6f, 0x6e, 0x65, 0x6e, 0x74, 0x2e, 0x64, 0x6c, 0x6c };
                            replacedata = new byte[] { 0x68, 0x74, 0x74, 0x70, 0x63, 0x6f, 0x6d, 0x70, 0x6f, 0x6e, 0x65, 0x6e, 0x62, 0x2e, 0x64, 0x6c, 0x6c };

                            byte[] finddata2 = new byte[] { 0x43, 0x6f, 0x6d, 0x6d, 0x42, 0x61, 0x73, 0x65, 0x2e, 0x64, 0x6c, 0x6c };
                            byte[] replacedata2 = new byte[] { 0x43, 0x6f, 0x6d, 0x6d, 0x42, 0x61, 0x7a, 0x65, 0x2e, 0x64, 0x6c, 0x6c };

                            if (PatcherReplace(finddata, replacedata, ref filedata) && PatcherReplace(finddata2, replacedata2, ref filedata))
                            {
                                tempfile = Path.GetTempPath() + "HiSuite 11.exe";
                                File.WriteAllBytes(tempfile, filedata);
                                bool succeded2 = ReplaceComponent(null, tempfile, true, false, HISuiteDir + "HiSuite 11.exe");

                                filedata = File.ReadAllBytes(HISuiteDir + "CommBase.dll");

                                if (PatcherReplace(finddata, replacedata, ref filedata))
                                {
                                    tempfile = Path.GetTempPath() + "CommBaze.dll";
                                    File.WriteAllBytes(tempfile, filedata);
                                    bool succeded3 = ReplaceComponent(null, tempfile, true, false, HISuiteDir + "CommBaze.dll");
                                    if(succeded1 && succeded2 && succeded3)
                                    {
                                        CreateShortcut(HISuiteDir + @"HiSuite 11.exe");
                                    }
                                    return (succeded1 && succeded2 && succeded3);
                                }
                                else
                                {
                                    return false;
                                }
                            }
                            else
                            {
                                return false;
                            }
                        }
                        else
                        {
                            return ReplaceComponent(filename, tempfile, true);
                        }
                    }
                    return true;
                }
                else
                {
                    if(!systemadmin)
                        MessageBox.Show("Some errors occured in the patching process!", "ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }
            }
            else
            {
                if(FindArray(filedata, replacedata) == -1)
                {
                    if (!systemadmin)
                        MessageBox.Show("Could not patch this version of HiSuite.", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return false;
                }
                else
                {
                    if (!systemadmin)
                        MessageBox.Show("File is already patched.", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return true;
                }
            }
        }
        private bool PatcherReplace(byte[] data, byte[] replacewith, ref byte[] basedata)
        {
            int dataindex = FindArray(basedata, data);
            if(dataindex == -1)
            {
                return false;
            }
            else
            {
                for(int i = 0, j = replacewith.Length; i < j; i++)
                {
                    basedata[dataindex + i] = replacewith[i];
                }
                return true;
            }
        }
        private int FindArray(byte[] basedata, byte[] searchdata)
        {
            int b = searchdata.Length;
            for (int i = 0, j = basedata.Length; i < j; i++)
            {
                if(b > (j - i))
                {
                    break;
                }
                bool succeed = true;
                for(int a = 0; a < b; a++)
                {
                    if(basedata[i + a] != searchdata[a])
                    {
                        succeed = false;
                        break;
                    }
                }
                if(succeed)
                {
                    return i;
                }
            }
            return -1;
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
            Process.Start("https://www.paypal.com/cgi-bin/webscr?cmd=_donations&business=fullclip39@gmail.com&item_name=HISuite+Proxy+Support&no_shipping=1&lc=US");
        }

        private void button6_Click(object sender, EventArgs e)
        {
            Process startprogram = new Process();
            startprogram.StartInfo.FileName = Process.GetCurrentProcess().MainModule.FileName;
            startprogram.StartInfo.Arguments = "\"SETUP\"";
            startprogram.StartInfo.Verb = "runas";
            try
            {
                startprogram.Start();
            }
            catch
            {

            }
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
            Progress progress = new Progress("Copying File For " + romname);
            new Thread(() =>
            {
                
                bool finished = false, formLoaded = false;
                progress.Load += delegate
                {
                    formLoaded = true;
                };
                Thread CopyThread = new Thread(() =>
                {
                    try
                    {
                        while (!formLoaded)
                        {
                            Thread.Sleep(400);
                        }

                        CopyItPlease(filekind, progress, romname, filename, packagename);
                        finished = true;
                    }
                    catch
                    {

                    }
                    Thread.Sleep(400);
                    progress.Close();
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
                Invoke(new Action(() =>
                {
                    progress.Show();
                }));
            }).Start();
        }

        private string GetFileMD5CheckSum(Stream fileStream)
        {
            string MD5Result = null;
            fileStream.Position = 0;
            using (MD5 MD5 = MD5.Create())
            {
                MD5Result = BitConverter.ToString(MD5.ComputeHash(fileStream)).Replace("-", "").ToUpper();
            }
            return MD5Result;
        }

        private string GetFileSHA256CheckSum(Stream fileStream)
        {
            string SHA256Result = null;
            fileStream.Position = 0;
            using (SHA256 sha256 = SHA256.Create())
            {
                SHA256Result = BitConverter.ToString(sha256.ComputeHash(fileStream)).Replace("-", "").ToUpper();
            }
            return SHA256Result;
        }

        private void custpkgSet_Click(object sender, EventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Filter = "ZIP Files (*.zip)|*.zip|All Files (*.*)|*.*";
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                custPKGData = LoadPackage(dialog.FileName);
                if (custPKGData != null)
                {
                    custpkgName.Text = custPKGData.PackageName;
                    textBox6.Enabled = false;
                    textBox6.Text = custpkgName.Text;

                    CopyFile(custpkgName.Text, dialog.FileName, Path.GetFileName(dialog.FileName), 1);
                }
                else
                {
                    textBox6.Enabled = true;
                }
            }
            else
            {
                custpkgName.Text = "Not Set";
                textBox6.Enabled = true;
                custPKGData = null;
            }
        }

        private void preloadpkgSet_Click(object sender, EventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Filter = "ZIP Files (*.zip)|*.zip|All Files (*.*)|*.*";
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                preloadPGKData = LoadPackage(dialog.FileName);
                if (preloadPGKData != null)
                {
                    preloadpkgName.Text = preloadPGKData.PackageName;
                    textBox5.Enabled = false;
                    textBox5.Text = preloadpkgName.Text;

                    CopyFile(preloadpkgName.Text, dialog.FileName, Path.GetFileName(dialog.FileName), 2);
                }
                else
                {
                    textBox5.Enabled = true;
                }
            }
            else
            {
                preloadpkgName.Text = "Not Set";
                textBox5.Enabled = true;
                preloadPGKData = null;
            }
        }

        private void CreateShortcut(string Source)
        {
            object shDesktop = (object)"Desktop";
            IWshRuntimeLibrary.WshShell shell = new IWshRuntimeLibrary.WshShell();
            string shortcutAddress = (string)shell.SpecialFolders.Item(ref shDesktop) + @"\HISuite 11.lnk";
            IWshRuntimeLibrary.IWshShortcut shortcut = (IWshRuntimeLibrary.IWshShortcut)shell.CreateShortcut(shortcutAddress);
            shortcut.IconLocation = AppDomain.CurrentDomain.BaseDirectory + "icon.ico";
            shortcut.Description = "Patched HISuite 11";
            shortcut.TargetPath = Source;
            shortcut.Save();
        }
        private PackageData LoadPackage(string filename)
        {
            PackageData packageData = new PackageData(null, null, null, null, null);
            try
            {
                using (Stream fileStream = File.OpenRead(filename))
                {
                    using (ZipArchive zipInputStream = new ZipArchive(fileStream))
                    {
                        foreach (ZipArchiveEntry entry in zipInputStream.Entries)
                        {
                            if (entry.Name.Contains("VERSION.mbn"))
                            {
                                int ReadSize = (int)entry.Length;
                                if (ReadSize == -1)
                                    ReadSize = 1024;
                                byte[] readSize = new byte[ReadSize];
                                Stream entryStream = entry.Open();
                                int ReadBytes = entryStream.Read(readSize, 0, readSize.Length);
                                entryStream.Close();
                                entryStream.Dispose();
                                if (ReadBytes > 0)
                                {
                                    string PackageName = Encoding.UTF8.GetString(readSize, 0, ReadBytes);
                                    PackageName = PackageName.Trim();
                                    int where = PackageName.IndexOf('\r'), where2 = PackageName.IndexOf('\n');
                                    if (where != -1 && (where < where2 || where2 == -1))
                                    {
                                        PackageName = PackageName.Substring(0, where);
                                    }
                                    else if (where2 != -1)
                                    {
                                        PackageName = PackageName.Substring(0, where2);
                                    }
                                    packageData.PackageName = PackageName;
                                }
                                break;
                            }
                        }

                        if (packageData.PackageName != null)
                        {
                            packageData.PackageFile = Path.GetFileName(filename);

                            packageData.PackageSize = fileStream.Length.ToString();

                            packageData.PackageSha256 = GetFileSHA256CheckSum(fileStream);

                            packageData.PackageMD5 = GetFileMD5CheckSum(fileStream);
                            return packageData;
                        }
                    }
                }
            }
            catch(Exception e)
            {
                MessageBox.Show(e.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            return null;
        }
        private void basepkgSet_Click(object sender, EventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Filter = "ZIP Files (*.zip)|*.zip|All Files (*.*)|*.*";
            if(dialog.ShowDialog() == DialogResult.OK)
            {
                basePKGData = LoadPackage(dialog.FileName);
                if (basePKGData != null)
                {
                    basepkgName.Text = basePKGData.PackageName;
                    textBox2.Enabled = false;
                    textBox2.Text = basepkgName.Text;

                    CopyFile(basepkgName.Text, dialog.FileName, Path.GetFileName(dialog.FileName), 0);
                }
                else
                {
                    textBox2.Enabled = true;
                }
            }
            else
            {
                basepkgName.Text = "Not Set";
                basePKGData = null;
                textBox2.Enabled = true;
            }
        }

        private void CopyItPlease(int FileKind, Progress progress, string romname, string filename, string packagename)
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
                        progress.SetProgress(100);
                        return;
                    }
                }
                string Directoryname = Path.GetDirectoryName(dir);
                if(!Directory.Exists(Directoryname))
                {
                    Directory.CreateDirectory(Directoryname);
                }
                if(FileKind > 0)
                {
                    if(FileKind == 1)
                    {
                        while(CopyingBase || CopyingPreload)
                        {
                            Thread.Sleep(1500);
                        }
                    }
                    else
                    {
                        while (CopyingBase || CopyingCust)
                        {
                            Thread.Sleep(1500);
                        }
                    }
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

                                progress.SetProgress(percentage);

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
        private bool CheckAuthentication(string phonedata, string response)
        {
            try
            {
                int where = response.IndexOf("data=");
                if (where != -1)
                {
                    where += 5;
                    int finish = response.IndexOf('&', where);
                    string base64 = Encoding.UTF8.GetString(Convert.FromBase64String(response.Substring(where, finish - where)));
                    if (base64.Contains("\"status\":\"1\"") || base64.Contains("\"status\":\"2\""))
                    {
                        new Thread(() =>
                        {
                            MessageBox.Show("Apparently Firm(s) is/are not approved for installation and proccess will most likely fail in phone.\r\n\r\nYou might want to consider cancelling it to avoid time waste.", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }).Start();
                        return true;
                    }
                    if(base64.Contains("\"status\":\"0\""))
                    {
                        where = phonedata.IndexOf("vendor");
                        if (where != -1)
                        {
                            where += 7;
                            where = phonedata.IndexOf('"', where);
                            if (where != -1)
                            {
                                where++;
                                finish = phonedata.IndexOf('-', where) + 1;
                                string phonemodel = phonedata.Substring(where, finish - where).ToUpper();
                                where = 0;
                                while ((where = base64.IndexOf("versionNumber", where)) != -1)
                                {
                                    where += 14;
                                    where = base64.IndexOf('"', where);
                                    where++;
                                    finish = base64.IndexOf('"', where);
                                    string responsemodel = base64.Substring(where, finish - where).ToUpper();
                                    if(!responsemodel.Contains("PATCH") && !responsemodel.Contains(phonemodel))
                                    {
                                        string showstr = "Your phone model: " + phonemodel + "\r\n";
                                        showstr += "Installing Firm Of: " + responsemodel + "\r\n\r\n";
                                        showstr += "Do you want to proceed? ( Might soft-brick your phone )";
                                        if(MessageBox.Show(showstr, "Firm Mis-Match", MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation) == DialogResult.No)
                                        {
                                            return false;
                                        }
                                    }
                                }
                                return true;
                            }
                            else
                            {
                                return true;
                            }
                        }
                        else
                        {
                            return true;
                        }
                    }
                    else
                    {
                        return true;
                    }
                }
                else
                {
                    return false;
                }
            }
            catch
            {
                return true;
            }
        }
    }
}
