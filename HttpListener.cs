using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics.Metrics;
using Microsoft.ML.Data;

namespace BYOWebServer
{
    
    public static class server {
        private static HttpListener listener;
        private static int maxSimultaneousConnections = 20;
        private static Semaphore Sem = new Semaphore(maxSimultaneousConnections, maxSimultaneousConnections);
        private static Router router = new Router();

     // Summary: Gets list of Ipaddresses assigned to localhosts. 
     private static List<IPAddress> GetLocalHostIPs()
        {
            IPHostEntry host;
            host = Dns.GetHostEntry(Dns.GetHostName());
            List<IPAddress> ips = host.AddressList.Where(ip => ip.AddressFamily == AddressFamily.InterNetwork).ToList();
         return ips; 
         }
     
     private static HttpListener InitializeListener(List<IPAddress> localhostIPs)
        {
            HttpListener listener = new HttpListener();
            listener.Prefixes.Add("https://localhost/");
            localhostIPs.ForEach((ip) => {
                Console.WriteLine("Listening on IP" + "http://" + ip.ToString() + "/");
                listener.Prefixes.Add("http://" + ip.ToString() + "/");
            }) ;

            return listener;
        }

        private static void Start(HttpListener listener)
        {
            listener.Start();
            Task.Run(() => RunServer(listener));
        }

        private static void RunServer(HttpListener listener)
        {
            while(true)
            {
                Sem.WaitOne();
                StartConnectionListener(listener);
            }

           
        }

        private static async void StartConnectionListener(HttpListener listener)
        {
            HttpListenerContext context = await listener.GetContextAsync();

            HttpListenerRequest request = context.Request;

            string? path = request.RawUrl?.Substring(0, request.RawUrl.IndexOf("?"));
            string method = request.HttpMethod;
            string? param = request.RawUrl?.Substring(request.RawUrl.IndexOf("?") + 1);

            Dictionary<String, String> kvParams = GetKeyValues(param);

            router.route(path, method, kvParams);

            Sem.Release();
            Log(context.Request);
            string response = "Hello Browser";
            byte[] encoded = Encoding.UTF8.GetBytes(response);
            context.Response.ContentLength64 = encoded.Length;
            context.Response.OutputStream.Write(encoded, 0, encoded.Length);
            context.Response.OutputStream.Close();
            
        }

        public static void Start()
        {
            List<IPAddress> localHostIPs = GetLocalHostIPs();
            HttpListener listener = InitializeListener(localHostIPs);
            Start(listener);
        }

        public static void Log(HttpListenerRequest? request)
        {
            if (request is not null)
            {
                Console.WriteLine(request.RemoteEndPoint + " " + request.HttpMethod + " /" + request.Url?.AbsoluteUri.ToString());
            }
        }
    
    }
}
