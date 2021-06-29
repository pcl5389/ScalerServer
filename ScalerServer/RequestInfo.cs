using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Net;
using System.Runtime.Remoting.Lifetime;
using System.Web;

namespace ScalerServer
{
    public class RequestInfo : MarshalByRefObject
    {
        public string RawUrl { get; set; }

        public string Protocol { get; private set; }

        public string FilePath { get; set; }

        public string QueryString { get; private set; }

        public string HttpMethod { get; set; }

        public Dictionary<string, string> Headers { get; private set; }

        public IPEndPoint RemoteEndPoint { get; set; }

        public IPEndPoint LocalEndPoint { get; set; }

        public byte[] EntityBody { get; set; }

        private string _rawRequestHeaders;

        private bool _parsed;

        public bool KeepAlive { get; private set; } = false;
        public bool GZiped = false;
        public bool GZip = false;
        public bool Chunked = false;
        public bool bResponseInit { get; set; } = false;

        public RequestInfo(string requestHeaders)
        {
            _rawRequestHeaders = requestHeaders;
        }

        public void ParseHeaders()
        {
            if (!_parsed)
            {
                DoParse();
                _parsed = true;
            }
        }

        private void DoParse()
        {
            string[] lines = _rawRequestHeaders.Split(new[] { "\r\n" }, StringSplitOptions.None);
            string[] actions = lines[0].Split(' ');
            HttpMethod = actions[0];
            RawUrl = actions[1];
            Protocol = actions[2];

            string[] path = RawUrl.Split('?');
            if (path[0].IndexOf('.') == -1 && !path[0].EndsWith("/", StringComparison.Ordinal))
                FilePath = HttpUtility.UrlDecode(path[0] + "/");
            else
                FilePath = HttpUtility.UrlDecode(path[0]);
            if (path.Length == 2)
                QueryString = path[1];
            Headers = new Dictionary<string, string>();
            for (int i = 1; i < lines.Length; i++)
            {
                string line = lines[i];
                if(string.IsNullOrEmpty(line))
                {
                    break;
                }
                int separator = line.IndexOf(":");
                Headers.Add(line.Substring(0, separator),
                            line.Substring(separator + 1).TrimStart());
            }
            KeepAlive = Headers["Connection"] == null ? false : Headers["Connection"] == "keep-alive";
            GZip = Headers["Accept-Encoding"] == null ? false : (Headers["Accept-Encoding"].IndexOf("gzip") != -1);
        }

        public override object InitializeLifetimeService()
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
