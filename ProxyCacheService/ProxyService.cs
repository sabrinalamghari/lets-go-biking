using System;
using System.Configuration;

namespace ProxyCacheService
{
    public class ProxyService : IProxyService
    {
        private readonly GenericProxyCache<RawHttpResource> _cache =
            new GenericProxyCache<RawHttpResource>();

        public string GetRaw(string url)
        {
            var res = _cache.Get(url);      // dt_default
            return res.Content;
        }

        public string GetRawTtl(string url, int ttlSeconds)
        {
            var res = _cache.Get(url, ttlSeconds);
            return res.Content;
        }

        public string GetRawUntil(string url, DateTimeOffset expiresAt)
        {
            var res = _cache.Get(url, expiresAt);
            return res.Content;
        }

        public string GetStationsJson(string contractName)
        {
            var apiKey = ConfigurationManager.AppSettings["JCDecauxApiKey"];
            var baseUrl = ConfigurationManager.AppSettings["JCDecauxBaseUrl"]
                          ?? "https://api.jcdecaux.com/vls/v3/";

            double ttlSeconds = 5;
            var ttlSetting = ConfigurationManager.AppSettings["JCDecauxStationsTtlSeconds"];
            if (!string.IsNullOrEmpty(ttlSetting))
                double.TryParse(ttlSetting, out ttlSeconds);

            var url = $"{baseUrl}stations?contract={contractName}&apiKey={apiKey}";

            // On passe par GetRawTtl pour profiter du cache générique
            return GetRawTtl(url, (int)ttlSeconds);
        }

        public string GetContractsJson()
        {
            var apiKey = ConfigurationManager.AppSettings["JCDecauxApiKey"];
            var baseUrl = ConfigurationManager.AppSettings["JCDecauxBaseUrl"]
                          ?? "https://api.jcdecaux.com/vls/v3/";
            double ttlSeconds = 60;
            var ttlSetting = ConfigurationManager.AppSettings["JCDecauxContractsTtlSeconds"];
            if (!string.IsNullOrEmpty(ttlSetting))
                double.TryParse(ttlSetting, out ttlSeconds);

            var url = $"{baseUrl}contracts?apiKey={apiKey}";

            return GetRawTtl(url, (int)ttlSeconds);
        }

    }
}