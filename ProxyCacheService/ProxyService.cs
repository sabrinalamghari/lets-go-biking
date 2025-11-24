using System;

namespace ProxyCacheService
{
    internal class ProxyService : IProxyService
    {
        private readonly GenericProxyCache<RawStringResource> _cache = new GenericProxyCache<RawStringResource>();
        public string GetRaw(string url)
        {
            var res = _cache.Get(url);
            return res.Value;
        }

        public string GetRawTtl(string url, int ttlSeconds)
        {
            var res = _cache.Get(url, ttlSeconds);
            return res.Value;
        }

        public string GetRawUntil(string url, DateTimeOffset expiresAt)
        {
            var res = _cache.Get(url, expiresAt);
            return res.Value;
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

