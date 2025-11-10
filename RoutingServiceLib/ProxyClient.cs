using System;
using System.ServiceModel;
using System.ServiceModel.Channels;

namespace RoutingServiceLib.Clients
{
    [ServiceContract]
    public interface IProxyService
    {
        [OperationContract]
        string Get(string url);
    }

    public class ProxyClient
    {
        private readonly string _endpointUrl;
        private readonly Binding _binding;
        private readonly EndpointAddress _endpoint;

        public ProxyClient(string endpointUrl = "http://localhost:9001/ProxyService")
        {
            _endpointUrl = endpointUrl;
            _binding = new BasicHttpBinding
            {
                MaxReceivedMessageSize = 10_000_000, // 10 Mo
                MaxBufferSize = 10_000_000,
                MaxBufferPoolSize = 10_000_000
            };
            _endpoint = new EndpointAddress(_endpointUrl);
        }

        public string Get(string url)
        {
            var factory = new ChannelFactory<IProxyService>(_binding, _endpoint);
            var ch = factory.CreateChannel();
            try
            {
                string res = ch.Get(url);
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
    }
}
