using System;
using System.IO;
using System.Reflection;
using System.Web.Hosting;
namespace ScalerServer
{
    class Program
    {
        static WebServer server;
        static string dir = Directory.GetCurrentDirectory();
        static int port = 9527;
        
        static void Main(string[] args)
        {
            _ = args.Length > 0 && int.TryParse(args[0], out port);
            //InitHostFile(dir);

            //Server_OnServiceStop(); //永远不停
            while(true)
            {
                var host = InitHost(dir);
                //host.OnStopService += Server_OnServiceStop;
                server = new WebServer(host, port);
                server.StartService();
            }
        }
        private static void Server_OnServiceStop()
        {
            var host = InitHost(dir);
            host.OnStopService += Server_OnServiceStop;
            server = new WebServer(host, port);
            server.StartService();
        }

        private static SimpleHost InitHost(string dir)
        {
            SimpleHost host = (SimpleHost)CreateWorkerAppDomainWithHost("/", dir, typeof(SimpleHost));
            //SimpleHost host = (SimpleHost)ApplicationHost.CreateApplicationHost(typeof(SimpleHost), "/", dir);
            host.Config("/", dir);
            return host;
        }
        static object CreateWorkerAppDomainWithHost(string virtualPath, string physicalPath, Type hostType)
        {
            string _appId = "ScalerApp";
            Type buildManagerHostType = typeof(System.Web.HttpRuntime).Assembly.GetType("System.Web.Compilation.BuildManagerHost");
            ApplicationManager appManager = ApplicationManager.GetApplicationManager();
            IRegisteredObject buildManagerHost = appManager.CreateObject(_appId, buildManagerHostType, virtualPath, physicalPath, false);

            buildManagerHostType.InvokeMember("RegisterAssembly",
                                              BindingFlags.Instance | BindingFlags.InvokeMethod | BindingFlags.NonPublic,
                                              null,
                                              buildManagerHost,
                                              new object[] { hostType.Assembly.FullName, hostType.Assembly.Location });
            return appManager.CreateObject(_appId, hostType, virtualPath, physicalPath, false);
        }
        /*
        //需要拷贝执行文件 才能创建ASP.NET应用程序域
        private static void InitHostFile(string dir)
        {
            string path = Path.Combine(dir, "bin");
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
            string source = Assembly.GetExecutingAssembly().Location;
            string target = path + "/" + Assembly.GetExecutingAssembly().GetName().Name + ".exe";
            if(File.Exists(target))
                File.Delete(target);
            File.Copy(source, target);
        }*/
    }
}
