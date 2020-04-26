using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using ICSharpCode.SharpZipLib.Zip;
using System.Windows.Forms;
using System.Threading;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Net.Security;

namespace HiSuite_Proxy
{
    static class RomDetails
    {
        public class RomDetailsClass
        {
            public bool ApprovedForInstall = false;
            public string[] SupportedVersions = { };
        }
        private static string GetVersionID(string url)
        {
            int where = url.IndexOf("/v");
            if (where != -1)
            {
                where += 2;
                int finish = url.IndexOf('/', where);
                string versionid = url.Substring(where, finish - where);
                return versionid;
            }
            else
            {
                throw new Exception("Invalid URL Passed");
            }
        }

        private static string GetPackageURL(string url)
        {
            int where = url.IndexOf("/full/");
            if (where != -1)
            {
                where += 6;
                gZipWebClient client = new gZipWebClient();
                client.Proxy = null;
                client.SetTimeout(10000);
                string baseurl = url.Substring(0, where);
                string response = client.DownloadString(baseurl + "/filelist.xml");
                where = response.IndexOf("package=");
                if (where != -1)
                {
                    where += 9;
                    int finish = response.IndexOf('"', where);
                    string packagename = response.Substring(where, finish - where);
                    return baseurl + packagename;
                }
                else
                {
                    throw new Exception("Error in processing 'filelist.xml'");
                }
            }
            else
            {
                throw new Exception("Invalid URL Passed, Couldn't find '/full/'");
            }
        }
        private static void SetProgress(FirmFinder owner, Progress form, int value, string text = "")
        {
            owner.Invoke(new Action(() =>
            {
                form.SetProgress(value, text);
            }));
        }
        public static void GetFirmwareDetails(FirmFinder form, string url, ref RomDetails.RomDetailsClass romDetails, int DetailsKind)
        {
            string VersionID = GetVersionID(url);
            if(DetailsKind == 0 || DetailsKind == 2)
                romDetails.ApprovedForInstall = ApprovedForInstallation(VersionID);

            if (DetailsKind == 0)
                return;

            Progress progress = null;
            bool Finished = false;
            Thread thisthread = Thread.CurrentThread;
            form.Invoke(new Action(() =>
            {
                progress = new Progress("Getting Package URL");
                progress.FormClosing += delegate
                {
                    if(!Finished)
                    {
                        thisthread.Abort();
                    }
                };
                progress.Show(form);
            }));

            SetProgress(form, progress, 5);
            string Package = GetPackageURL(url);

            SetProgress(form, progress, 30, "Initial Request To ZIP");         
            HTTPStream httpstream = new HTTPStream(Package);

            ZipInputStream stream = new ZipInputStream(httpstream);
            SetProgress(form, progress, 40, "Exploring ZIP File...");
            int count = 0;
            while (true)
            {
                ZipEntry entry = stream.GetNextEntry();
                if (entry != null)
                {
                    count += 2;
                    if(count > 60)
                    {
                        count = 30;
                    }
                    SetProgress(form, progress, 40 + count, entry.Name);
                    if (entry.Name.Contains("SOFTWARE_VER_LIST.mbn"))
                    {                       
                        int read = 4096;
                        byte[] buffer = new byte[read];
                        string content = "";
                        while ((read = stream.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            content += Encoding.UTF8.GetString(buffer, 0, read);
                        }
                        romDetails.SupportedVersions = content.Split('\n');
                        break;
                    }
                    else
                    {
                        stream.CloseEntry();
                    }
                }
                else
                {
                    MessageBox.Show("Seems like I couldn't load the data :(", "ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    break;
                }
            }
            Finished = true;
            progress.Close();
        }
        private static bool ApprovedForInstallation(string versionID)
        {
            TcpClient client = new TcpClient();
            IPAddress[] address = Dns.GetHostAddresses("query.hicloud.com");
            if (address.Length == 0)
            {
                throw new Exception("Couldn't get query.hicloud.com's ip address");
            }
            if (!client.BeginConnect(address[0], 443, null, null).AsyncWaitHandle.WaitOne(6000))
            {
                throw new Exception("Couldn't connect to huawei servers");
            }
            SslStream stream = new SslStream(client.GetStream());
            if(!stream.BeginAuthenticateAsClient("query.hicloud.com", null, SslProtocols.Tls | SslProtocols.Tls11 | SslProtocols.Tls12, false, null, null).AsyncWaitHandle.WaitOne(5000))
            {
                try
                {
                    stream.Close();
                    client.Close();
                }
                catch
                {

                }
                throw new Exception("Couldn't establish secure connection with huawei servers");
            }

            string updata = Properties.Resources.romapprovedjson.Replace("VersionID", versionID);
            string headers = "POST /sp_ard_common/v1/authorize.action HTTP/1.1" + "\r\n";
            headers += "Host: query.hicloud.com" + "\r\n";
            headers += "Connection: close" + "\r\n";
            headers += "Content-Type: application/json" + "\r\n";
            headers += "Content-Length: " + Encoding.ASCII.GetByteCount(updata) + "\r\n\r\n";
            headers += updata;

            byte[] senddata = Encoding.ASCII.GetBytes(headers);
            if(!stream.BeginWrite(senddata, 0, senddata.Length, null, null).AsyncWaitHandle.WaitOne(5000))
            {
                try
                {
                    stream.Close();
                    client.Close();
                }
                catch
                {

                }
                throw new Exception("Couldn't send data to huawei");
            }

            stream.ReadTimeout = 7000;

            string response = "";
            byte[] recieve = new byte[4096];
            int readdata = 0;
            while((readdata = stream.Read(recieve, 0, recieve.Length)) > 0)
            {
                response += Encoding.UTF8.GetString(recieve, 0, readdata);
            }

            try
            {
                stream.Close();
                client.Close();
            }
            catch
            {

            } 
            /*
            gZipWebClient client = new gZipWebClient();
            client.SetTimeout(10000);
            client.Proxy = null;
            
            client.Headers.Set(HttpRequestHeader.ContentType, "application/json;charset=UTF-8");
            string response = string.Empty;
            try
            {
                response = client.UploadString("https://query.hicloud.com:443/sp_ard_common/v1/authorize.action", updata);
            }
            catch
            {
                throw new Exception("Couldn't receive information from huawei servers");
            }*/
            int where = response.IndexOf("data=");
            if (where != -1)
            {
                where += 5;
                int finish = response.IndexOf('&', where);
                string base64response = response.Substring(where, finish - where);
                int mod4 = base64response.Length % 4;
                if (mod4 > 0)
                {
                    base64response += new string('=', 4 - mod4);
                }
                base64response = Encoding.UTF8.GetString(Convert.FromBase64String(base64response));
                if (base64response.Contains("\"status\":\"0\""))
                {
                    return true;
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
    }
}
