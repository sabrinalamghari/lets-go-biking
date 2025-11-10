using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Caching;

namespace ProxyCacheService
{
    internal class ProxyService : IProxyService
    {
        private readonly MemoryCache _cache = MemoryCache.Default;

        public string Get(string url)
        {
            if (_cache.Contains(url))
            {
                Console.WriteLine($"[Cache HIT] {url}");
                return (string)_cache[url];
            }

            Console.WriteLine($"[Cache MISS] Fetching {url}");
            using (HttpClient client = new HttpClient())
            {
                try
                {
                    string result = client.GetStringAsync(url).Result;
                    _cache.Add(url, result, DateTimeOffset.Now.AddSeconds(30));
                    return result;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ERROR] {ex.Message}");
                    return $"Error fetching {url}: {ex.Message}";
                }
            }
        }
    }
}

