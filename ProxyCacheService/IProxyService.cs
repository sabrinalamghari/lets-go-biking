using System;
using System.ServiceModel;

namespace ProxyCacheService
{
    [ServiceContract]
    public interface IProxyService
    {
        [OperationContract] string GetRaw(string url);                    
        [OperationContract] string GetRawTtl(string url, int ttlSeconds);
        [OperationContract] string GetRawUntil(string url, DateTimeOffset expiresAt);
        [OperationContract] string GetStationsJson(string contractName);
        [OperationContract] string GetContractsJson();

    }


}
