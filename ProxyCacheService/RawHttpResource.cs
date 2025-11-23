using System;
using System.Net.Http;

namespace ProxyCacheService
{
    public class RawHttpResource
    {
        private static readonly HttpClient client;

        static RawHttpResource()
        {
            client = new HttpClient();

            client.DefaultRequestHeaders.UserAgent.ParseAdd(
                "LetsGoBiking/1.0 (contact: sabrinalmghari@gmail.com)");
            client.DefaultRequestHeaders.From = "sabrinalmghari@gmail.com";
        }

        public string Url { get; }
        public string Content { get; }

        public RawHttpResource(string url)
        {
            Url = url;

            Console.WriteLine("[Proxy] HTTP GET " + url);

            var response = client.GetAsync(url).Result;
            response.EnsureSuccessStatusCode();

            Content = response.Content.ReadAsStringAsync().Result;
        }
    }
}
