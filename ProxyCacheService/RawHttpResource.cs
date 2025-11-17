using System;
using System.Net.Http;

namespace ProxyCacheService
{
    public class RawHttpResource
    {
        public string Url { get; }
        public string Content { get; }

        public RawHttpResource(string url)
        {
            Url = url;

            using (var client = new HttpClient())
            {
                Console.WriteLine("[Proxy] HTTP GET " + url);

                var response = client.GetAsync(url).Result;
                response.EnsureSuccessStatusCode();

                Content = response.Content.ReadAsStringAsync().Result;
            }
        }
    }
}
