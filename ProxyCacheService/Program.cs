using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Description;
using System.Text;
using System.Threading.Tasks;

namespace ProxyCacheService
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Uri baseAddress = new Uri("http://localhost:9001/ProxyService");
            using (ServiceHost host = new ServiceHost(typeof(ProxyService), baseAddress))
            {
                var binding = new BasicHttpBinding
                {
                    MaxReceivedMessageSize = 10_000_000, // 5 Mo
                    MaxBufferSize = 10_000_000,
                    MaxBufferPoolSize = 10_000_000
                };

                host.AddServiceEndpoint(typeof(IProxyService), binding, "");

                // exposer le WSDL/MEX pour les tests
                var smb = new ServiceMetadataBehavior { HttpGetEnabled = true, HttpGetUrl = baseAddress };
                host.Description.Behaviors.Add(smb);
                host.AddServiceEndpoint(typeof(IMetadataExchange), MetadataExchangeBindings.CreateMexHttpBinding(), "mex");
                host.Open();
                Console.WriteLine("ProxyCacheService started at " + baseAddress);
                Console.WriteLine("Press ENTER to stop...");
                Console.ReadLine();
            }
        }
    }
}
