using System.ServiceModel;
using System.ServiceModel.Web;

namespace RoutingServiceLib
{
    [ServiceContract]
    public interface IRoutingServiceRest
    {
        [OperationContract]
        [WebGet(
            UriTemplate = "/route?from={from}&to={to}",
            ResponseFormat = WebMessageFormat.Json,
            BodyStyle = WebMessageBodyStyle.Wrapped
        )]
        RouteResult GetRouteRest(string from, string to);

        [OperationContract]
        [WebInvoke(Method = "OPTIONS", UriTemplate = "/route")]
        void OptionsRoute();
    }
}
