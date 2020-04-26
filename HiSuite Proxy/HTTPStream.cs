using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Net;
using ICSharpCode.SharpZipLib.Zip;
namespace HiSuite_Proxy
{
    class HTTPStream : Stream
    {
        private string _serverUri;
        private MemoryStream StoringStream = null;

        private HttpWebRequest _request;
        private HttpWebResponse _response;
        private Stream _responseStream = null;

        private long TotalLength = 0, BytePlace = 0, LoadRange = 0, TotalReadBytes = 0;
        public HTTPStream(string serverUri)
        {
            _serverUri = serverUri;
            StartHTTPDOwnload();
        }

        private void StartHTTPDOwnload(long from = 0)
        {
            _request = (HttpWebRequest)WebRequest.Create(_serverUri);
            _request.Proxy = null;

            _request.AddRange(from, from + 300000);
            LoadRange = from;
            _response = (HttpWebResponse)_request.GetResponse();
            _responseStream = _response.GetResponseStream();
            if (TotalLength == 0)
            {
                TotalLength = _response.ContentLength;
            }
            if (StoringStream != null)
            {
                StoringStream.Close();
            }
            StoringStream = new MemoryStream();
            int readbytes = 0;
            while (true)
            {
                byte[] readdata = new byte[4096];
                int len = _responseStream.Read(readdata, 0, readdata.Length);
                readbytes += len;
                if (len > 0 && readbytes < 250000)
                {
                    StoringStream.Write(readdata, 0, len);
                }
                else
                {
                    break;
                }
            }
            TotalReadBytes += readbytes;
            if(TotalReadBytes > (5000000))
            {
                throw new Exception("More than 5MBs were downloaded, process failed");
            }
            StoringStream.Position = 0;

            _responseStream.Close();
            _response.Close();
            _responseStream = null;
            _response = null;
            _request = null;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int attempts = 0;
            while (attempts++ < 5)
            {
                if (StoringStream == null)
                {
                    StartHTTPDOwnload();
                }

                try
                {
                    int done = StoringStream.Read(buffer, offset, count);
                    BytePlace += done;
                    long placereached = (BytePlace - LoadRange);
                    if (placereached >= StoringStream.Length)
                    {
                        Seek(0, SeekOrigin.Current);
                        int ndone = StoringStream.Read(buffer, done, count - done);
                        BytePlace += ndone;
                        done += ndone;
                    }
                    return done;
                }
                catch (Exception ex)
                {
                    Close();
                    throw ex;
                }
            }
            return 0;
        }

        public override void Close()
        {
            if (StoringStream != null)
            {
                try
                {
                    StoringStream.Close();
                }
                catch
                {

                }
            }
            StoringStream = null;
        }

        public override void Flush()
        {
            StoringStream.Flush();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            if (origin == SeekOrigin.Begin)
            {
                BytePlace = offset;
            }
            else
            {
                BytePlace += offset;
            }

            long newplace = BytePlace - LoadRange;
            if (newplace < 0 || newplace >= StoringStream.Length)
            {
                StartHTTPDOwnload((BytePlace));
            }
            else
            {
                StoringStream.Seek(newplace, SeekOrigin.Begin);
            }
            return BytePlace;
        }

        public override void SetLength(long value)
        {
            StoringStream.SetLength(value);
        }
        public override void Write(byte[] buffer, int offset, int count)
        {
            StoringStream.Write(buffer, offset, count);
        }

        public override bool CanRead
        {
            get { return StoringStream.CanRead; }
        }
        public override bool CanSeek
        {
            get { return StoringStream.CanSeek; }
        }
        public override bool CanWrite
        {
            get { return StoringStream.CanWrite; }
        }
        public override long Length
        {
            get { return TotalLength; }
        }
        public override long Position
        {
            get { return BytePlace; }
            set { BytePlace = value; } // never gets called, no worries
        }
    }
}
