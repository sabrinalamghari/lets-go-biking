using System.Net.Http;

namespace ProxyCacheService
{
    public class RawStringResource
    {
        private static readonly HttpClient http = new HttpClient();
        public string Value { get; }
        public RawStringResource(string url)
        {
            Value = http.GetStringAsync(url).Result;
        }
    }
}
