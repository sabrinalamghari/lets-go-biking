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
        [OperationContract] // indique que la méthode est exposée au client
        string Get(string url);
    }
}
