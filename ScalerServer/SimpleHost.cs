using System;
using System.Web;

namespace ScalerServer
{
    public class SimpleHost : MarshalByRefObject
    {
        public string PhysicalDir { get; private set; }
        public string VituralDir { get; private set; }
        public void Config(string vitrualDir, string physicalDir)
        {
            VituralDir = vitrualDir;
            PhysicalDir = physicalDir;
        }

        public void ProcessRequest(ref HttpProcessor processor, ref RequestInfo requestInfo)
        {
            WorkerRequest workerRequest = new WorkerRequest(this, processor, requestInfo);
            HttpRuntime.ProcessRequest(workerRequest);
        }
        public override Object InitializeLifetimeService()
        {
            return null;
        }
        public AppDomain GetAppDomain()
        {
            return System.Threading.Thread.GetDomain();
        }
    }
}
