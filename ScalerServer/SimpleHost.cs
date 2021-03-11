using System;
using System.Web;

namespace ScalerServer
{
    public class SimpleHost : MarshalByRefObject
    {
        public string PhysicalDir { get; private set; }
        public string VituralDir { get; private set; }
        public bool bStoping = false, bStoped = false;

        public delegate void StartService();
        public event StartService OnStartService;
        public event StartService OnStopService;
        public SimpleHost()
        {
            
        }

        private void SimpleHost_DomainUnload(object sender, EventArgs e)
        {
            bStoping = true;
            DateTime dtExpired = DateTime.Now.AddSeconds(15);
            while (!bStoped)
            {
                System.Threading.Thread.Sleep(1);
                if (DateTime.Now > dtExpired)
                {
                    Console.WriteLine("停止超时！正在强制停止服务...");
                    break;
                }
            }
            Console.WriteLine("Host 服务已停止！");
            if (OnStopService != null)
                OnStopService.Invoke();
        }
        public void Start()
        {
            if (OnStartService != null)
                OnStartService.Invoke();
        }
        public void Config(string vitrualDir, string physicalDir)
        {
            VituralDir = vitrualDir;
            PhysicalDir = physicalDir;
            System.Threading.Thread.GetDomain().DomainUnload += SimpleHost_DomainUnload;
        }
        
        public void ProcessRequest(ref HttpProcessor processor, ref RequestInfo requestInfo)
        {
            WorkerRequest workerRequest = new WorkerRequest(this, processor, requestInfo);
            HttpRuntime.ProcessRequest(workerRequest);
        }
        public override object InitializeLifetimeService()
        {
            return null;
        }
        public AppDomain GetAppDomain()
        {
            return System.Threading.Thread.GetDomain();
        }
    }
}
