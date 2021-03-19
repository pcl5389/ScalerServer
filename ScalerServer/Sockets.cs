using System;
using System.IO;
using System.Net;
using System.Net.Sockets;

namespace ScalerServer
{
    public class Sockets : IDisposable
    {
        const int BUFFER_SIZE = 1024;
        /// <summary>
        /// 接收缓冲区
        /// </summary>
        public byte[] RecBuffer = new byte[BUFFER_SIZE];

        public MemoryStream ms = new MemoryStream();
        public int Total = 0;
        /// <summary>
        /// 异步接收后包的大小
        /// </summary>
        public int Offset { get; set; }
        /// <summary>
        /// 空构造
        /// </summary>
        public Sockets() { }
        /// <summary>
        /// 创建Sockets对象
        /// </summary>
        /// <param name="ip">Ip地址</param>
        /// <param name="client">TcpClient</param>
        /// <param name="ns">承载客户端Socket的网络流</param>
        public Sockets(IPEndPoint ip, IPEndPoint lp, TcpClient client, NetworkStream ns)
        {
            Ip = ip;
            Lp = lp;
            Client = client;
            nStream = ns;
        }
        /// <summary>
        /// 当前IP地址,端口号
        /// </summary>
        public IPEndPoint Ip { get; set; }
        public IPEndPoint Lp { get; set; }
        
        /// <summary>
        /// 客户端主通信程序
        /// </summary>
        public TcpClient Client { get; set; }
        /// <summary>
        /// 承载客户端Socket的网络流
        /// </summary>
        public NetworkStream nStream { get; set; }
        /// <summary>
        /// 异常枚举
        /// </summary>

        /// <summary>
        /// 新客户端标识.如果推送器发现此标识为true,那么认为是客户端上线
        /// 仅服务端有效
        /// </summary>
        public bool NewClientFlag { get; set; }
        public void Dispose()
        {
            if (ms != null && ms.Length > 0)
            {
                ms.SetLength(0);
                ms.Dispose();
            }
            Total = 0;
            Lp = Ip = null;
            if (Client != null && Client.Connected)
            {
                Client.Client.Shutdown(SocketShutdown.Both);
                Client.Client.Close();
            }
            GC.Collect(0, GCCollectionMode.Optimized);
        }
    }
}
