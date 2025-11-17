using System;
using System.Configuration;
using System.Net.Http;

namespace ProxyCacheService
{
    public class JCDecauxStationsData
    {
        public string ContractName { get; set; }
        public string RawJson { get; set; }

        public JCDecauxStationsData(string contractName)
        {
            ContractName = contractName;
            RawJson = CallApi(contractName);
        }

        private string CallApi(string contractName)
        {
            var apiKey = ConfigurationManager.AppSettings["JCDecauxApiKey"];
            var baseUrl = ConfigurationManager.AppSettings["JCDecauxBaseUrl"];

            if (string.IsNullOrEmpty(apiKey))
                throw new InvalidOperationException("JCDecauxApiKey n'est pas configurée dans App.config");

            if (string.IsNullOrEmpty(baseUrl))
                baseUrl = "https://api.jcdecaux.com/vls/v3/";

            var url = $"{baseUrl}stations?contract={contractName}&apiKey={apiKey}";

            Console.WriteLine("[Proxy] APPEL HTTP vers JCDecaux : " + url);

            using (var client = new HttpClient())
            {
                var response = client.GetAsync(url).Result;
                response.EnsureSuccessStatusCode();
                return response.Content.ReadAsStringAsync().Result;
            }
        }
    }
}
