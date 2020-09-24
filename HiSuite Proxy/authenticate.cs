using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Threading;
using System.Net;

namespace HiSuite_Proxy
{
    public partial class authenticate : Form
    {
        public string responsedata = "";
        public bool Success = false;
        private string UploadData = null;
        private Color GreenColor = Color.FromArgb(0, 204, 0), RedColor = Color.FromArgb(230, 46, 0), OrangeColor = Color.FromArgb(255, 175, 26);
        private Thread thethread;
        public authenticate(string senddata)
        {
            InitializeComponent();
            richTextBox1.ReadOnly = true;
            richTextBox1.ScrollBars = RichTextBoxScrollBars.Both;
            CheckForIllegalCrossThreadCalls = false;
            richTextBox1.SelectionFont = new Font(richTextBox1.SelectionFont.Name, 8, FontStyle.Bold);
            UploadData = senddata;
            thethread = new Thread(BypassRobot);
            thethread.Start();
            this.FormClosed += delegate
            {
                try
                {
                    thethread.Abort();
                }
                catch
                {

                }
            };
        }
        string cookie = "";
        private void BypassRobot()
        {
            for(int i = 0; i < 3; i++)
            {
                AppendMessage("Connecting to Authentication Bridge", GreenColor);
                try
                {
                    gZipWebClient client = new gZipWebClient();
                    string response = client.DownloadString("http://bt3zgcp.byethost18.com/auth/index.php");
                    int where = response.LastIndexOf("toNumbers");
                    if(where == -1)
                    {
                        AppendMessage("Unexpected Response Received, Retrying...", OrangeColor);
                    }
                    else
                    {
                        where += 11;
                        int finish = response.IndexOf('"', where);
                        string iphex = response.Substring(where, finish - where);
                        cookie = "__test=" + DecryptIP(iphex);
                        Authenticate();
                        return;
                    }
                }
                catch(Exception e)
                {
                    if(i != 2)
                        AppendMessage("Connection to Authentication Bridge Failed, Retrying...", OrangeColor);
                    else
                    {
                        AppendMessage("Connection to Authentication Bridge Failed, Process Aborted.", OrangeColor);
                        AppendMessage(e.Message, OrangeColor);
                    }
                }
            }
            AppendMessage("Connection to Authentication Bridge Failed, Process Aborted.", RedColor);
        }
        private void Authenticate()
        {
            for(int i = 0; i < 3; i++)
            {
                AppendMessage("Sending Data to Authentication Bridge", GreenColor);
                try
                {
                    gZipWebClient client = new gZipWebClient();
                    client.Headers.Set(HttpRequestHeader.Cookie, cookie);
                    string response = client.UploadString("http://bt3zgcp.byethost18.com/auth/index.php", UploadData);
                    int where = response.LastIndexOf("data=");
                    if (where == -1)
                    {
                        AppendMessage("Unexpected Authentication Response Received, Retrying...", OrangeColor);
                    }
                    else
                    {
                        responsedata = response;
                        Success = true;
                        Close();
                        return;
                    }
                }
                catch(Exception e)
                {
                    if (i != 2)
                        AppendMessage("Authentication Through Bridge Failed, Retrying...", OrangeColor);
                    else
                    {
                        AppendMessage("Authentication Through Bridge Failed, Process Aborted.", OrangeColor);
                        AppendMessage(e.Message, OrangeColor);
                    }
                }
            }
            AppendMessage("Authentication Through Bridge Failed, Process Aborted.", OrangeColor);
            responsedata = null;
            Success = false;
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
        }
        private byte[] toNumbers(string str)
        {
            List<byte> data = new List<byte>();
            string[] regex = Regex.Split(str, @"(?<=\G.{2})");
            for(int i = 0, j = regex.Length - 1; i < j; i++)
            {
                data.Add(Convert.ToByte(regex[i], 16));
            }
            return data.ToArray();
        }

        public class gZipWebClient : WebClient
        {
            private int Timeout = 25000;
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
        private string DecryptIP(string hash)
        {
            byte[] key = { 246, 85, 186, 157, 9, 161, 18, 212, 150, 140, 99, 87, 157, 181, 144, 180 };
            byte[] iv = { 152, 52, 76, 46, 238, 134, 195, 153, 72, 144, 89, 37, 133, 180, 159, 128 };
            var bytesIn = toNumbers(hash);

            AesManaged aesmanage = new AesManaged();
            aesmanage.KeySize = key.Length * 8;
            aesmanage.Padding = PaddingMode.None;
            aesmanage.Key = key;
            aesmanage.IV = iv;
            aesmanage.Mode = CipherMode.CBC;

            ICryptoTransform decrypter = aesmanage.CreateDecryptor();
            byte[] decrypted = decrypter.TransformFinalBlock(bytesIn, 0, bytesIn.Length);

            string decrypteddata = BitConverter.ToString(decrypted);
            decrypteddata = decrypteddata.Replace("-", "").ToLower();
            decrypter.Dispose();
            aesmanage.Dispose();
            return decrypteddata;
        }
    }
}
