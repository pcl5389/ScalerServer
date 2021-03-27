using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;

namespace ScalerServer
{
    public class MyHttpRequestHeader
    {
        public Uri uri { set; get; }
        public HttpCookieCollection Cookies { set; get; } = new HttpCookieCollection();
        public bool Gzip { set; get; }
        public string Method { set; get; }
        public int ContentLength { set; get; }
        public bool KeepAlive { set; get; }
        public bool bEnd { set; get; }
        public string Data { set; get; }
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("URI:" + uri.AbsoluteUri);
            if (Cookies != null)
            {
                foreach (HttpCookie cookie in Cookies)
                {
                    sb.AppendLine("Cookie:" + cookie.Name + ":" + cookie.Value);
                }
            }
            sb.AppendLine("Gzip:" + Gzip.ToString());
            sb.AppendLine("Method:" + Method);
            sb.AppendLine("ContentLength:" + ContentLength.ToString());
            sb.AppendLine("KeepAlive:" + KeepAlive.ToString());
            sb.AppendLine();
            sb.AppendLine(Data);
            return sb.ToString();
        }
    }
    class MyIIS
    {
        public static object objWriter = new object();
        public static void WriteLog(string strs)
        {
            lock (objWriter)
            {
                using (var sw = new StreamWriter(AppDomain.CurrentDomain.BaseDirectory + "Log.txt", true))
                {
                    sw.WriteLine(string.Format("{0},{1}", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), strs));
                }
            }
        }
        static readonly string BasicPath = AppDomain.CurrentDomain.BaseDirectory + "wwwroot/";
        static readonly string Exts = (".htm|.html|.php|.gif|.jpg|.jpeg|.png|.bmp|.ico|.rar|.css|.js|.zip|.java|.jar|.txt|.flv|.swf|.mid|.doc|.ppt|.xls|.pdf|.txt|.mp3|.wma|");
        //private static Regex line0 = new Regex("^(GET|POST) .+? HTTP/(\\.|\\d)+$", RegexOptions.Compiled);
        static Regex regLen = new Regex("Content-Length: \\d*", RegexOptions.IgnoreCase);
        public static RequestInfo Analyze(MemoryStream ms, ref int total)
        {
            ms.Position = 0;
            int iChar, iLoc=0;
            while ((iChar = ms.ReadByte()) > -1)
            {
                if (iChar.Equals(13))
                {
                    if (ms.ReadByte().Equals(10))
                    {
                        if (ms.ReadByte().Equals(13))
                        {
                            if (ms.ReadByte().Equals(10))
                            {
                                iLoc = (int)(ms.Position - 4);
                                break;
                            }
                        }
                    }
                }
            }
            if (iLoc == 0)
                return null;

            byte[] _header_buffer = new byte[iLoc];
            ms.Position = 0;
            ms.Read(_header_buffer, 0, iLoc);
            string _header = Encoding.ASCII.GetString(_header_buffer);
            string Method = _header.Substring(0, _header.IndexOf(' '));
            RequestInfo request = null;
            if (Method == "GET")
            {
                request = new RequestInfo(_header);
                request.ParseHeaders();
                return request;
            }
            else if (Method == "POST")
            {
                Match m = regLen.Match(_header);
                if (!m.Success || !int.TryParse(m.Value.Substring(15), out int content_length))
                {
                    throw new MyException(0, "无请求长度！");
                }
                total = content_length + iLoc + 4;
                if ((iLoc + 4 + content_length) == ms.Length)
                {
                    request = new RequestInfo(_header);
                    byte[] buffer = new byte[content_length];
                    ms.Position = iLoc + 4;
                    ms.Read(buffer, 0, content_length);
                    request.EntityBody = buffer;
                    request.ParseHeaders();
                }
                return request;
            }
            throw new MyException(0, "错误的HTTP请求！");
        }
        public static byte[] Response(MyHttpRequestHeader header)
        {
#if DEBUG
            DateTime dt0 = DateTime.Now;
#endif
            if (!header.uri.AbsolutePath.EndsWith("/", StringComparison.Ordinal)) //文件
            {
                FileInfo file = new FileInfo(BasicPath + header.uri.AbsolutePath.Substring(1));
                if (file.Name.IndexOf(".") > -1)
                {
                    if (Exts.IndexOf(file.Extension.ToLower() + "|") > -1) //静态文件
                    {
                        return Response(header, new FileInfo(BasicPath + System.Web.HttpUtility.UrlDecode(header.uri.AbsolutePath)));
                    }
                }
            }
            string path = BasicPath + header.uri.AbsolutePath;
            if (!header.uri.AbsolutePath.EndsWith("/", StringComparison.Ordinal))
            {
                path = path + "/";
            }
            if (File.Exists(path + "default.html"))
            {
                return Response(header, new FileInfo(path + "default.html"));
            }
            else if (File.Exists(path + "index.html"))
            {
                return Response(header, new FileInfo(path + "index.html"));
            }
            else if (File.Exists(path + "index.htm"))
            {
                return Response(header, new FileInfo(path + "index.htm"));
            }
            else if (File.Exists(path + "default.aspx"))
            {
                return Response(header, "200", "aspx");
            }
            return Response(header, "200", path);
        }
        public static byte[] Response(MyHttpRequestHeader header, int status)
        {
            return Response(header, status.ToString(), "");
        }
        public static byte[] Response(MyHttpRequestHeader header, string status, string content)
        {
            StringBuilder sb = new StringBuilder();
            byte[] buffer1 = Encoding.UTF8.GetBytes(content);
            switch (status)
            {
                case "404":
                    sb.AppendLine("HTTP/1.1 200 OK");
                    sb.AppendLine("Server: ScalerServer");
                    sb.AppendLine(string.Format("Date: {0}", DateTime.Now.ToString("r")));
                    sb.AppendLine("Content-Type: text/html");
                    sb.AppendLine(string.Format("Connection: {0}", (header.KeepAlive ? "keep-alive" : "close")));
                    sb.AppendLine(string.Format("Content-Length: {0}", buffer1.Length));
                    sb.AppendLine();
                    break;
                default:
                    sb.AppendLine(string.Format("HTTP/1.1 {0} OK", status));
                    sb.AppendLine("Server: ScalerServer");
                    sb.AppendLine(string.Format("Date: {0}", DateTime.Now.ToString("r")));
                    if (content.StartsWith("<", StringComparison.Ordinal))
                        sb.AppendLine("Content-Type: text/xml");
                    else if (content.StartsWith("{", StringComparison.Ordinal) && content.EndsWith("}", StringComparison.Ordinal))
                        sb.AppendLine("Content-Type: application/json");
                    else
                        sb.AppendLine("Content-Type: text/html");
                    sb.AppendLine(string.Format("Connection: {0}", (header.KeepAlive ? "keep-alive" : "close")));
                    sb.AppendLine(string.Format("Content-Length: {0}", buffer1.Length));
                    sb.AppendLine();
                    break;
            }
            byte[] buffer2 = Encoding.UTF8.GetBytes(sb.ToString());
            byte[] buffer = new byte[buffer1.Length + buffer2.Length];
            buffer2.CopyTo(buffer, 0);
            buffer1.CopyTo(buffer, buffer2.Length);
            return buffer;
        }

        public static byte[] Response(MyHttpRequestHeader header, FileInfo file)
        {

            StringBuilder sb = new StringBuilder();
            if (file.Exists)
            {
                sb.AppendLine("HTTP/1.1 200 OK");
                sb.AppendLine("Server: ScalerServer");
                sb.AppendLine(string.Format("Date: {0}", DateTime.Now.ToString("r")));
                switch (file.Extension.ToLower())
                {
                    case ".css":
                        sb.AppendLine("Content-Type: text/css; charset=utf-8");
                        break;
                    case ".png":
                        sb.AppendLine("Content-Type: image/png; charset=utf-8");
                        break;
                    case ".jpg":
                        sb.AppendLine("Content-Type: image/jpeg; charset=utf-8");
                        break;
                    case ".jpge":
                        sb.AppendLine("Content-Type: image/jpeg; charset=utf-8");
                        break;
                    case ".html":
                        sb.AppendLine("Content-Type: text/html; charset=utf-8");
                        break;
                    case ".htm":
                        sb.AppendLine("Content-Type: text/html; charset=utf-8");
                        break;
                    case ".txt":
                        sb.AppendLine("Content-Type: text/html; charset=utf-8");
                        break;
                    case ".js":
                        sb.AppendLine("Content-Type: application/javascript; charset=utf-8");
                        break;
                    case ".gif":
                        sb.AppendLine("Content-Type: image/gif; charset=utf-8");
                        break;
                    case ".config":
                        sb.AppendLine("Content-Type: image/gif; charset=utf-8");
                        break;
                    case ".cs":
                        sb.AppendLine("Content-Type: image/gif; charset=utf-8");
                        break;
                    case ".mdb":
                        sb.AppendLine("Content-Type: image/gif; charset=utf-8");
                        break;
                    default:
                        sb.AppendLine(string.Format("Content-Type: application/{0}", file.Extension));
                        break;
                }
                sb.AppendLine(string.Format("Connection: {0}", (header.KeepAlive ? "keep-alive" : "close")));
                sb.AppendLine(string.Format("Content-Length: {0}", file.Length));
                sb.AppendLine();
            }
            else
            {
                string echo = string.Format("文件{0}不存在！", file.FullName);
                sb.AppendLine("HTTP/1.1 200 OK");
                sb.AppendLine("Server: ScalerServer");
                sb.AppendLine(string.Format("Date: {0}", DateTime.Now.ToString("r")));
                sb.AppendLine("Content-Type: text/html; charset=utf-8");
                sb.AppendLine("Connection: keep-alive");
                sb.AppendLine(string.Format("Content-Length: {0}", Encoding.UTF8.GetBytes(echo).Length));
                sb.AppendLine();
                sb.Append(echo);

                return Encoding.UTF8.GetBytes(sb.ToString());
            }
            FileStream fs = file.Open(FileMode.Open, FileAccess.Read, FileShare.Read);
            MemoryStream ms = new MemoryStream();
            ms.Write(Encoding.UTF8.GetBytes(sb.ToString()), 0, sb.Length);
            fs.CopyTo(ms);
            fs.Close();
            fs.Dispose();
            return ms.ToArray();
        }
    }
    public class MyException : ApplicationException
    {
        public int ErrorCode { get; set; }
        /// <summary>
        /// 自定义登录异常
        /// </summary>
        /// <param name="ErrCode">错误码 必须数字</param>
        /// <param name="ErrDescription">错误描述</param>
        public MyException(int ErrCode, string ErrDescription) : base(ErrDescription)
        {
            ErrorCode = ErrCode;
        }
        public override string ToString()
        {
            return string.Format("ErrorCode:{0}, Message:{1}", ErrorCode, Message);
        }
    }
}
