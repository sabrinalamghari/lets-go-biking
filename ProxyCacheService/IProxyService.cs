using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ServiceModel;

namespace ProxyCacheService
{
    [ServiceContract] // indique qu’on définit un contrat WCF
    public interface IProxyService
    {
        [OperationContract] string GetRaw(string url);                     // dt_default
        [OperationContract] string GetRawTtl(string url, int ttlSeconds);  // now + seconds
        [OperationContract] string GetRawUntil(string url, DateTimeOffset expiresAt); // fixed date
    }


}
