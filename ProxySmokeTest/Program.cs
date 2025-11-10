using System;
using System.ServiceModel;
using System.ServiceModel.Channels;

[ServiceContract]
public interface IProxyService
{
    [OperationContract]
    string Get(string url);
}

class Program
{
    static void Main()
    {
        var binding = new BasicHttpBinding();
        var endpoint = new EndpointAddress("http://localhost:9001/ProxyService");
        var factory = new ChannelFactory<IProxyService>(binding, endpoint);
        var ch = factory.CreateChannel();

        try
        {
            var url = "https://api.ipify.org?format=json";
            Console.WriteLine("Requête 1...");
            var res1 = ch.Get(url);
            Console.WriteLine(res1);

            Console.WriteLine("\nRequête 2 (cache)...");
            var res2 = ch.Get(url);
            Console.WriteLine(res2);

            ((IClientChannel)ch).Close();
            factory.Close();
        }
        catch
        {
            ((IClientChannel)ch).Abort();
            factory.Abort();
            throw;
        }

        Console.WriteLine("\nTest terminé. Appuyez sur Entrée pour quitter.");
        Console.ReadLine();
    }
}
