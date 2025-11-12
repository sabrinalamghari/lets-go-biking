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
    }
}

