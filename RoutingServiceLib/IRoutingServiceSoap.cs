using System.ServiceModel;

namespace RoutingServiceLib
{
    [ServiceContract]
    public interface IRoutingServiceSoap
    {
        [OperationContract]
        RouteResult GetRoute(string from, string to);
    }
}
