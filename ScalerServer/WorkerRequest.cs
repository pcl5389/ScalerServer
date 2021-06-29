using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.IO.Compression;
using System.Web;

namespace ScalerServer
{
    class WorkerRequest : HttpWorkerRequest
    {
        private SimpleHost _host;
        private HttpProcessor _processor;
        private RequestInfo _requestInfo;

        //输出相关
        private int _statusCode;
        private Dictionary<string, string> _responseHeaders;
        private IList<byte[]> _responseBodyBytes;

        //请求相关
        private string[] _knownRequestHeaders;
        private string[][] _unknownRequestHeaders;

        private bool _isHeaderSent;
        private int iHeaderIndex = 0;

        public WorkerRequest(SimpleHost host, HttpProcessor processor, RequestInfo requestInfo)
        {
            _host = host;
            _processor = processor;
            _requestInfo = requestInfo;

            _responseHeaders = new Dictionary<string, string>();
            _responseBodyBytes = new List<byte[]>();

            ParseRequestHeaders();
        }

        private void ParseRequestHeaders()
        {
            _knownRequestHeaders = new string[40];
            Dictionary<string, string> unknownHeaders = new Dictionary<string, string>();
            foreach (var item in _requestInfo.Headers)
            {
                int index = GetKnownRequestHeaderIndex(item.Key);
                if (index >= 0)
                    _knownRequestHeaders[index] = item.Value;
                else
                    unknownHeaders.Add(item.Key, item.Value);
            }
            _unknownRequestHeaders = new string[unknownHeaders.Count][];
            int i = 0;
            foreach (var item in unknownHeaders)
            {
                _unknownRequestHeaders[i++] = new string[] {item.Key, item.Value };
            }
        }

        #region vitural method

        public override string GetAppPath()
        {
            return _host.VituralDir;
        }

        public override string GetAppPathTranslated()
        {
            return _host.PhysicalDir;
        }

        public override string GetFilePath()
        {
            return _requestInfo.FilePath;
        }

        public override string GetFilePathTranslated()
        {
            string path = GetFilePath();
            path = path.Substring(_host.VituralDir.Length);
            path = path.Replace('/', '\\');
            return _host.PhysicalDir + path;
        }

        public override byte[] GetPreloadedEntityBody()
        {
            return _requestInfo.EntityBody;
        }

        public override bool IsEntireEntityBodyIsPreloaded()
        {
            return true;
        }

        public override int ReadEntityBody(byte[] buffer, int size)
        {
            return buffer.Length;
        }

        public override string GetKnownRequestHeader(int index)
        {
            return _knownRequestHeaders[index];
        }

        public override string GetUnknownRequestHeader(string name)
        {
            foreach (var item in _requestInfo.Headers)
            {
                if (item.Key.Equals(name, StringComparison.OrdinalIgnoreCase))
                    return item.Value;
            }
            return null;
        }

        public override string[][] GetUnknownRequestHeaders()
        {
            return _unknownRequestHeaders;
        }
        #endregion
        public override string GetUriPath()
        {
            return _requestInfo.FilePath;
        }

        public override string GetQueryString()
        {
            return _requestInfo.QueryString;
        }

        public override string GetRawUrl()
        {
            return _requestInfo.RawUrl;
        }

        public override string GetHttpVerbName()
        {
            return _requestInfo.HttpMethod;
        }

        public override string GetHttpVersion()
        {
            return _requestInfo.Protocol;
        }

        public override string GetRemoteAddress()
        {
            return _requestInfo.RemoteEndPoint.Address.ToString();
        }

        public override int GetRemotePort()
        {
            return _requestInfo.RemoteEndPoint.Port;
        }

        public override string GetLocalAddress()
        {
            return _requestInfo.LocalEndPoint.Address.ToString();
        }

        public override int GetLocalPort()
        {
            return _requestInfo.LocalEndPoint.Port;
        }

        public override void SendStatus(int statusCode, string statusDescription)
        {
            _statusCode = statusCode;
        }

        public override void SendKnownResponseHeader(int index, string value)
        {
            if (index == 27)
                _responseHeaders[(iHeaderIndex++).ToString() + "$" + GetKnownResponseHeaderName(index)] = value;
            else
                _responseHeaders[GetKnownResponseHeaderName(index)] = value;
        }

        public override void SendUnknownResponseHeader(string name, string value)
        {
            _responseHeaders[(iHeaderIndex++).ToString() + "$" + name] = value;
        }

        public override void SendResponseFromMemory(byte[] data, int length)
        {
            if (length > 0)
            {
                byte[] dst = new byte[length];
                Buffer.BlockCopy(data, 0, dst, 0, length);
                _responseBodyBytes.Add(dst);
            }
        }

        public override void SendResponseFromFile(string filename, long offset, long length)
        {
        }

        public override void SendResponseFromFile(IntPtr handle, long offset, long length)
        {
        }

        public override void FlushResponse(bool finalFlush)
        {
            if (!_requestInfo.bResponseInit)
            {
                if (_responseHeaders.TryGetValue("Transfer-Encoding", out string chunked) && chunked.Equals("chunked", StringComparison.OrdinalIgnoreCase))
                {
                    _requestInfo.Chunked = true;
                }
                if (_responseHeaders.TryGetValue("Content-Encoding", out string gzip) && gzip.IndexOf("gzip", StringComparison.OrdinalIgnoreCase) > -1)
                {
                    _requestInfo.GZiped = true;
                }
                if (_requestInfo.Chunked)
                {
                    _processor.SendHeaders(_statusCode, _responseHeaders, 0, _requestInfo.KeepAlive);
                }
                _requestInfo.bResponseInit = true;
            }
            if (_requestInfo.Chunked)
            {
                for (int i = 0; i < _responseBodyBytes.Count; i++)
                {
                    byte[] data = _responseBodyBytes[i];
                    _processor.SendResponse(data);
                }
            }
            else
            {
                MemoryStream ms = new MemoryStream();
                for (int i = 0; i < _responseBodyBytes.Count; i++)
                {
                    byte[] data = _responseBodyBytes[i];
                    ms.Write(data, 0, data.Length);
                }
                if (_requestInfo.GZip && !_requestInfo.GZiped && ms.Length > 512 && ms.Length < 2097152)
                {
                    if (!_isHeaderSent)
                    {
                        _responseHeaders.Add("Content-Encoding", "gzip");
                    }
                    //开始压缩发送
                    using (MemoryStream gms = new MemoryStream())
                    {
                        using (GZipStream gZipStream = new GZipStream(gms, CompressionMode.Compress, true))
                        {
                            ms.Position = 0;
                            ms.CopyTo(gZipStream);
                        }
                        if (!_isHeaderSent)
                        {
                            _processor.SendHeaders(_statusCode, _responseHeaders, (int)gms.Length, _requestInfo.KeepAlive);
                            _isHeaderSent = true;
                        }
                        if (gms.Length > 0)
                        {
                            gms.Position = 0;
                            _processor.SendResponse(gms.ToArray());
                        }
                    }
                }
                else
                {
                    if (!_isHeaderSent)
                    {
                        _processor.SendHeaders(_statusCode, _responseHeaders, (int)ms.Length, _requestInfo.KeepAlive);
                        _isHeaderSent = true;
                    }
                    if (ms.Length > 0)
                    {
                        ms.Position = 0;
                        _processor.SendResponse(ms.ToArray());
                    }
                }
            }
            _responseBodyBytes.Clear();
            if (finalFlush)
            {
                _isHeaderSent = false;
                if (!_requestInfo.KeepAlive)
                    _processor.Close();
            }
        }

        public override void EndOfRequest()
        {
        }
    }
}
