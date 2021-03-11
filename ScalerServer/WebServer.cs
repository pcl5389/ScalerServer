using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace ScalerServer
{
    [Serializable]
    class WebServer
    {
        private static TcpListener listener;
        private SimpleHost _host;
        public SimpleHost Host { set { _host = value; } }
        public int Port { get; private set; }
        public WebServer(SimpleHost host, int port)
        {
            _host = host;
            Port = port;
            _host.OnStartService += Start;
        }
        public void StartService()
        {
            try
            {
                _host.Start();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }
        public void Start()
        {
            //Console.WriteLine("listener domain:" + System.Threading.Thread.GetDomainID());
            _host.bStoping = _host.bStoped = false;
            listener = new TcpListener(IPAddress.Any, Port);
            try
            {
                listener.Start(10000);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message.ToString());
                Console.ReadLine();
                return;
            }
            Console.WriteLine("Serving HTTP on 0.0.0.0 port " + Port + " ...");
            OnStart();
            //new Thread(OnStart).Start();
        }
        private Semaphore semap = new Semaphore(5, 5000);
        private void OnStart() //object state
        {
            //Console.WriteLine("while domain:" + System.Threading.Thread.GetDomainID());
            Console.WriteLine("开始监听");
            while (listener != null)
            {
                if (listener.Pending())
                {
                    listener.BeginAcceptSocket(new AsyncCallback(NewConnect), listener);
                }
                else
                {
                    if (_host.bStoping)
                    {
                        listener.Stop();
                        //Console.WriteLine("listen stop domain:" + System.Threading.Thread.GetDomainID());
                        Console.WriteLine("监听已停止");
                        _host.bStoped = true;
                        break;
                    }
                    Thread.Sleep(1);
                }
            }
            //Console.WriteLine("跳出循环");
        }
        public delegate void AsyncClientHandler(TcpClient client);
       
        private void NewConnect(IAsyncResult ar)
        {
            //初始化一个SOCKET，用于其它客户端的连接
            TcpListener server = (TcpListener)ar.AsyncState;
            try
            {
                TcpClient tclient = server.EndAcceptTcpClient(ar);
                AsyncClientHandler handler = new AsyncClientHandler(AcceptClient);
                handler.BeginInvoke(tclient, null, handler);
            }
            catch (Exception err)
            {
                Console.WriteLine(err.Message);
                /*
                 if (tclient != null && tclient.Connected)
                {
                    tclient.Close();
                }
                if (tclient.Client != null)
                    tclient.Client.Dispose();
                 */
            }
        }
        public void AcceptClient(TcpClient tclient)
        {
            Socket socket = tclient.Client;
            NetworkStream stream = new NetworkStream(socket, true);
            stream.ReadTimeout = 90000;
            Sockets sks = new Sockets(tclient.Client.RemoteEndPoint as IPEndPoint, tclient.Client.LocalEndPoint as IPEndPoint, tclient, stream);
            sks.NewClientFlag = true;
            //客户端异步接收
            try
            {
                sks.nStream.BeginRead(sks.RecBuffer, 0, sks.RecBuffer.Length, new AsyncCallback(EndReader), sks);
            }
            catch
            {
                sks.Dispose();
            }
        }

        public string defaultDocument(string path, string[] files)
        {
            foreach (string file in files)
            {
                if (File.Exists(path + "/" + file))
                    return file;
            }
            return string.Empty;
        }

        private void EndReader(IAsyncResult ir)
        {
            //DateTime dt0 = DateTime.Now;
            Sockets sks = ir.AsyncState as Sockets;
            RequestInfo req = null;
            if (sks != null)
            {
                if (sks.NewClientFlag || sks.Offset != 0)
                {
                    sks.NewClientFlag = false;
                    try
                    {
                        sks.Offset = sks.nStream.EndRead(ir);
                    }
                    catch
                    {
                        sks.Dispose();
                        sks.Offset = 0;
                    }
                    if (sks.Offset > 0)
                    {
                        string response = string.Empty;
                        int status = 200;
                        sks.ms.Position = sks.ms.Length;
                        sks.ms.Write(sks.RecBuffer, 0, sks.Offset);
                        if(sks.Total==0 || sks.Total==sks.ms.Length)
                        {
                            try
                            {
                                req = MyIIS.Analyze(sks.ms, ref sks.Total);
                            }
                            catch (MyException me)
                            {
                                response = me.Message;
                                status = 504;
                            }
                        }
                        if (req != null)
                        {
                            HttpProcessor processor = new HttpProcessor(_host, sks.Client.Client);
                            if (status != 200)
                            {
                                processor.SendResponse(status, Encoding.UTF8.GetBytes(response), null, req.KeepAlive);
                            }
                            else
                            {
                                string staticContentType = HttpProcessor.GetStaticContentType(req);
                                if (!string.IsNullOrEmpty(staticContentType)) //静态内容
                                {
                                    processor.WriteFileResponse(req.FilePath, staticContentType, req.GZip, req.KeepAlive);
                                }
                                else if (req.FilePath.EndsWith("/"))  //目录
                                {
                                    string file = defaultDocument(_host.PhysicalDir, new string[] { "default.htm", "default.html", "index.htm", "index.html", "default.aspx", "index.aspx" });
                                    if (string.IsNullOrEmpty(file))
                                    {
                                        processor.WriteDirResponse(req.FilePath, req.KeepAlive);
                                    }
                                    else if(file.EndsWith(".aspx"))
                                    {
                                        req.RawUrl = req.RawUrl + file;
                                        req.FilePath = req.FilePath + file;
                                        req.RemoteEndPoint = sks.Ip;
                                        req.LocalEndPoint = sks.Lp;
                                        _host.ProcessRequest(ref processor, ref req);
                                    }
                                    else
                                    {
                                        processor.WriteFileResponse(req.FilePath + file, "text/html", req.GZip, req.KeepAlive);
                                    }
                                }
                                else
                                {
                                    req.RemoteEndPoint = sks.Ip;
                                    req.LocalEndPoint = sks.Lp;
                                    //DateTime dt00 = DateTime.Now;
                                    _host.ProcessRequest(ref processor, ref req);
                                    //Console.WriteLine((DateTime.Now - dt00).TotalMilliseconds.ToString());
                                }
                            }
                            if (req.KeepAlive)
                            {
                                if (sks.Client != null && sks.Client.Client != null && sks.Client.Connected)
                                {
                                    if (sks.ms.CanWrite && sks.ms.Length > 0)
                                    {
                                        sks.ms.SetLength(0);
                                        sks.ms.Capacity = 0;
                                        sks.Total = 0;
                                        GC.Collect(0, GCCollectionMode.Optimized);
                                    }
                                    try
                                    {
                                        sks.nStream.BeginRead(sks.RecBuffer, 0, sks.RecBuffer.Length, new AsyncCallback(EndReader), sks);
                                    }
                                    catch
                                    {
                                        sks.Dispose();
                                    }
                                }
                                else
                                {
                                    sks.Dispose();
                                }
                            }
                            else
                            {
                                sks.Dispose();
                            }
                        }
                        else 
                        {
                            if (sks.Client.Connected)
                            {
                                try
                                {
                                    sks.nStream.BeginRead(sks.RecBuffer, 0, sks.RecBuffer.Length, new AsyncCallback(EndReader), sks);
                                }
                                catch
                                {
                                    sks.Dispose();
                                }
                            }
                            else
                            {
                                sks.Dispose();
                            }
                        }
                    }
                }
            }
        }
#if CLOSE
        public void Stop()
        {
            IsRuning = false;
            if (listener != null)
            {
                listener.Stop();
            }
        }
#endif
    }
}
