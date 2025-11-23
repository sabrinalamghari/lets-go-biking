using System;
using System.Net;
using System.ServiceModel;
using System.ServiceModel.Description;
using System.ServiceModel.Web;
using RoutingServiceLib;

namespace RoutingHost
{
    class Program
    {
        static void Main(string[] args)
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            var baseAddress = new Uri("http://localhost:9002/");

            using (var host = new WebServiceHost(typeof(RoutingServiceImpl), baseAddress))
            {
                var binding = new WebHttpBinding
                {
                    CrossDomainScriptAccessEnabled = true
                };

                var ep = host.AddServiceEndpoint(typeof(IRoutingService), binding, "");
                ep.EndpointBehaviors.Add(new WebHttpBehavior
                {
                    HelpEnabled = true,
                    DefaultOutgoingResponseFormat = WebMessageFormat.Json
                });

                host.Open();
                Console.WriteLine("RoutingService REST démarré !");
                Console.WriteLine("Test : http://localhost:9002/route?from=Paris&to=Lyon");
                Console.WriteLine("Appuyez sur Entrée pour arrêter...");
                Console.ReadLine();
                host.Close();
            }
        }
    }
}
