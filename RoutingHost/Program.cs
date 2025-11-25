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

            var restBase = new Uri("http://localhost:9002/");
            var soapBase = new Uri("http://localhost:9002/soap");

            using (var restHost = new WebServiceHost(typeof(RoutingServiceImpl), restBase))
            using (var soapHost = new ServiceHost(typeof(RoutingServiceImpl), soapBase))
            {
                // ===== REST JSON (pour le front) =====
                var restBinding = new WebHttpBinding
                {
                    CrossDomainScriptAccessEnabled = true
                };

                var restEndpoint = restHost.AddServiceEndpoint(typeof(IRoutingService), restBinding, "");
                restEndpoint.EndpointBehaviors.Add(new WebHttpBehavior
                {
                    HelpEnabled = true,
                    DefaultOutgoingResponseFormat = WebMessageFormat.Json
                });

                // ===== SOAP (pour ton heavy client C#) =====
                var soapBinding = new BasicHttpBinding
                {
                    MaxReceivedMessageSize = 10_000_000
                };

                // endpoint SOAP -> http://localhost:9002/soap
                soapHost.AddServiceEndpoint(typeof(IRoutingServiceSoap), soapBinding, "");

                // MEX pour exposer le WSDL -> http://localhost:9002/soap/mex
                var smb = new ServiceMetadataBehavior
                {
                    HttpGetEnabled = true,
                    HttpGetUrl = soapBase
                };
                soapHost.Description.Behaviors.Add(smb);
                soapHost.AddServiceEndpoint(
                    typeof(IMetadataExchange),
                    MetadataExchangeBindings.CreateMexHttpBinding(),
                    "mex"
                );

                // ===== Démarrage =====
                restHost.Open();
                soapHost.Open();

                Console.WriteLine("RoutingService REST démarré sur " + restBase);
                Console.WriteLine("RoutingService SOAP démarré sur " + soapBase);
                Console.WriteLine("WSDL : " + soapBase + "?wsdl");
                Console.WriteLine("Test REST : http://localhost:9002/route?from=Paris&to=Lyon");
                Console.WriteLine("Appuyez sur Entrée pour arrêter...");
                Console.ReadLine();

                soapHost.Close();
                restHost.Close();
            }
        }
    }
}
