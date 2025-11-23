using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Web;

namespace RoutingServiceLib
{
    [ServiceContract]
    public interface IRoutingService
    {
        [OperationContract]
        [WebGet(UriTemplate = "/route?from={from}&to={to}",
                ResponseFormat = WebMessageFormat.Json)]
        RouteResult GetRoute(string from, string to);

        [OperationContract]
        [WebInvoke(Method = "OPTIONS", UriTemplate = "/route")]
        void OptionsRoute();

    }

}
