using System;
using System.ServiceModel;
using System.ServiceModel.Channels;

namespace RoutingServiceLib.Clients
{
    [ServiceContract]
    public interface IProxyService
    {
        [OperationContract]
        string GetRaw(string url);

        [OperationContract]
        string GetRawTtl(string url, int ttlSeconds);

        [OperationContract]
        string GetRawUntil(string url, DateTimeOffset expiresAt);

        [OperationContract]
        string GetStationsJson(string contractName);

        [OperationContract]
        string GetContractsJson();

    }

    public class ProxyClient
    {
        private readonly Binding _binding;
        private readonly EndpointAddress _endpoint;

        public ProxyClient(string endpointUrl = "http://localhost:9001/ProxyService")
        {
            _binding = new BasicHttpBinding
            {
                MaxReceivedMessageSize = 10_000_000,
                MaxBufferSize = 10_000_000,
                MaxBufferPoolSize = 10_000_000
            };

            _endpoint = new EndpointAddress(endpointUrl);
        }

        private T CallProxy<T>(Func<IProxyService, T> action)
        {
            var factory = new ChannelFactory<IProxyService>(_binding, _endpoint);
            var ch = factory.CreateChannel();
            try
            {
                var res = action(ch);
                ((IClientChannel)ch).Close();
                factory.Close();
                return res;
            }
            catch
            {
                ((IClientChannel)ch).Abort();
                factory.Abort();
                throw;
            }
        }

        public string GetRaw(string url)
        {
            return CallProxy(p => p.GetRaw(url));
        }

        public string GetRawTtl(string url, int ttlSeconds)
        {
            return CallProxy(p => p.GetRawTtl(url, ttlSeconds));
        }

        public string GetRawUntil(string url, DateTimeOffset expiresAt)
        {
            return CallProxy(p => p.GetRawUntil(url, expiresAt));
        }

        public string GetStationsJson(string contractName)
        {
            return CallProxy(p => p.GetStationsJson(contractName));
        }

        public string GetContractsJson()
        {
            return CallProxy(p => p.GetContractsJson());
        }


    }
}