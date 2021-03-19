using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Web;
using System.Runtime.Remoting.Lifetime;

namespace ScalerServer
{
    public class HttpProcessor : MarshalByRefObject
    {
        const int FILE_BLOCK_SIZE = 1024;
        public Socket _socket;
        private SimpleHost _host;
        private static readonly Dictionary<string, string> staticFileContentType = new Dictionary<string, string>()
        {
            {"htm", "text/html"},
            {"html", "text/html"},
            {"xml", "text/xml"},
            {"txt", "text/plain"},
            {"css", "text/css"},
            {"js", "application/x-javascript"},
            {"png", "image/png"},
            {"gif", "image/gif"},
            {"jpg", "image/jpg"},
            {"jpeg", "image/jpeg"},
            {"zip", "application/zip"},
            {"ico", "image/x-icon"},
            {"ttf", "application/octet-stream"},
            {"woff", "font/x-woff"},
            {"woff2", "application/x-font-woff"},
            {"eot", "application/vnd.ms-fontobject"},
            {"svg", "image/svg+xml"},
            {"swf", "application/x-shockwave-flash"}
        };
        public HttpProcessor(SimpleHost host, Socket socket)
        {
            _host = host;
            _socket = socket;
        }
        public static string GetStaticContentType(RequestInfo requestInfo)
        {
            int dotStart = requestInfo.FilePath.LastIndexOf('.') + 1;
            if (dotStart > 0)
            {
                string extension = requestInfo.FilePath.Substring(dotStart);
                if (staticFileContentType.TryGetValue(extension.ToLower(), out string ext))
                    return ext;
            }
            return string.Empty;
        }
        public void WriteFileResponse(string filePath, string contentType, bool gZip, bool keepalive)
        {
            string fullPath = Path.Combine(_host.PhysicalDir, filePath.TrimStart('/'));
            if (!File.Exists(fullPath))
                SendResponse(200, Encoding.UTF8.GetBytes("404"), null, keepalive); //SendErrorResponse(404, keepalive);
            else
            {
                Dictionary<string, string> header = new Dictionary<string, string>();
                header["Content-Type"] = contentType;
                header["Expires"] = DateTime.Now.AddDays(3).GetDateTimeFormats('r')[0].ToString();
                header["Cache-Control"] = "max-age=259200";

                using (FileStream fs = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    if (fs.Length > int.MaxValue)
                    {
                        Close();
                        return;
                    }
                    int length = (int)fs.Length;
                    if (gZip && fs.Length > 512 && fs.Length < 2097152)
                    {
                        if (gZip)
                            header["Content-Encoding"] = "gzip";
                        using (MemoryStream ms = new MemoryStream())
                        {
                            using (System.IO.Compression.GZipStream gZipStream = new System.IO.Compression.GZipStream(ms, System.IO.Compression.CompressionMode.Compress, true))
                            {
                                byte[] buffer = new byte[FILE_BLOCK_SIZE];
                                int contentLength = fs.Read(buffer, 0, FILE_BLOCK_SIZE);

                                while (contentLength > 0)
                                {
                                    gZipStream.Write(buffer, 0, contentLength);
                                    contentLength = fs.Read(buffer, 0, FILE_BLOCK_SIZE);
                                }
                            }
                            string headers = BuildHeader(200, header, (int)ms.Length, keepalive);
                            SendResponse(Encoding.UTF8.GetBytes(headers));
                            SendResponse(ms.ToArray());
                            ms.SetLength(0);
                            ms.Capacity = 0;
                        }
                    }
                    else
                    {
                        string headers = BuildHeader(200, header, length, keepalive);
                        SendResponse(Encoding.UTF8.GetBytes(headers));
                        byte[] buffer = new byte[FILE_BLOCK_SIZE];
                        int contentLength = fs.Read(buffer, 0, FILE_BLOCK_SIZE);
                        while (contentLength > 0 && _socket.Connected)
                        {
                            SendResponse(buffer, 0, contentLength);
                            contentLength = fs.Read(buffer, 0, FILE_BLOCK_SIZE);
                        }
                    }
                }
            }
        }
        public void WriteDirResponse(string filePath, bool keepalive)
        {
            string dir = Path.Combine(_host.PhysicalDir, filePath.TrimStart('/'));
            if (!Directory.Exists(dir))
                SendResponse(404, Encoding.UTF8.GetBytes("404"), new Dictionary<string, string>() { { "Content-Type", "text/html" } }, keepalive);
            else
            {
                string[] files = Directory.GetFileSystemEntries(dir);
                StringBuilder builder = new StringBuilder(files.Length + 2);
                builder.Append("<ol>");
                foreach (string file in files)
                {
                    string filename = Path.GetFileName(file);
                    bool isDir = Directory.Exists(file);
                    builder.AppendFormat("<li><a href=\"{0}\">{1}</a>{2}</li>", isDir ? filename + "/" : filename,
                                         filename, isDir ? "文件夹" : "");
                }
                builder.Append("</ol>");
                SendResponse(200, Encoding.UTF8.GetBytes(builder.ToString()), new Dictionary<string, string>() { { "Content-Type", "text/html" } }, keepalive);
            }
        }
        public void SendResponse(int statusCode, byte[] responseBodyBytes, Dictionary<string, string> headers, bool keepAlive)
        {
            SendHeaders(statusCode, headers, responseBodyBytes.Length, keepAlive);
            SendResponse(responseBodyBytes);
        }

        public void SendResponse(byte[] data, int offset = 0, int length = 0)
        {
            length = length == 0 ? data.Length : length;
            try
            {
                if (_socket.Connected && length > 0)
                    _socket.Send(data, offset, length, SocketFlags.None);
            }
            catch(Exception err)
            {
                Console.WriteLine("发送数据失败！" + err.Message);
                Close();
            }
        }
        public void SendHeaders(int statusCode, Dictionary<string, string> headers, int contentLength, bool keepAlive)
        {
            string header = BuildHeader(statusCode, headers, contentLength, keepAlive);
            SendResponse(Encoding.UTF8.GetBytes(header));
        }
        public static string BuildHeader(int statusCode, Dictionary<string, string> headers, int contentLength, bool keepAlive)
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendFormat("HTTP/1.1 {0} {1}\r\n", statusCode, HttpWorkerRequest.GetStatusDescription(statusCode));
            builder.AppendFormat("Server: Scaler Server/1.0\r\n");
            builder.AppendFormat("Date: {0}\r\n", DateTime.Now.ToUniversalTime().ToString("R", DateTimeFormatInfo.InvariantInfo));
            if (contentLength >= 0)
                builder.AppendFormat("Content-Length: {0}\r\n", contentLength);
            if (keepAlive)
                builder.Append("Connection: keep-alive\r\n");
            if (headers != null)
            {
                foreach (KeyValuePair<string, string> pair in headers)
                {
                    builder.AppendFormat("{0}: {1}\r\n", pair.Key.IndexOf("$") > -1 ? pair.Key.Substring(pair.Key.IndexOf("$") + 1) : pair.Key, pair.Value);
                }
            }
            builder.Append("\r\n");
            return builder.ToString();
        }

        public void Close()
        {
            if (_socket != null && _socket.Connected)
            {
                _socket.Shutdown(SocketShutdown.Both);
                _socket.Close();
            }
            GC.Collect(0, GCCollectionMode.Optimized);
        }
        public override Object InitializeLifetimeService()
        {
            ILease lease = (ILease)base.InitializeLifetimeService();
            if (lease.CurrentState == LeaseState.Initial)
            {
                lease.InitialLeaseTime = TimeSpan.FromMinutes(1);
                lease.SponsorshipTimeout = TimeSpan.FromMinutes(2);
                lease.RenewOnCallTime = TimeSpan.FromSeconds(2);
            }
            return lease;
        }
    }
}