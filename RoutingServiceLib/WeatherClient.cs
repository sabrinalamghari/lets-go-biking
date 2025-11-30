using System;
using System.Globalization;
using System.Net;
using System.Web.Script.Serialization;

namespace RoutingServiceLib
{
    public class WeatherInfo
    {
        public string Description { get; set; }
        public double Temperature { get; set; }
        public int Humidity { get; set; }
    }

    public static class WeatherClient
    {
        // ⚠️ mets bien ta clé SANS \r\n ni espace
        private const string ApiKey = "c1fe54300c468bcd687662703e118868";

        public static WeatherInfo GetWeather(double lat, double lon)
        {
            // coords avec point comme séparateur décimal
            string latStr = lat.ToString(CultureInfo.InvariantCulture);
            string lonStr = lon.ToString(CultureInfo.InvariantCulture);

            string url =
                $"https://api.openweathermap.org/data/2.5/weather?lat={latStr}&lon={lonStr}&units=metric&lang=fr&appid={ApiKey}";

            using (var wc = new WebClient())
            {
                string json = wc.DownloadString(url);

                var serializer = new JavaScriptSerializer();
                dynamic data = serializer.DeserializeObject(json);

                // Convert.ToDouble / ToInt32 gèrent decimal, double, int, etc.
                double temp = Convert.ToDouble(data["main"]["temp"], CultureInfo.InvariantCulture);
                int humidity = Convert.ToInt32(data["main"]["humidity"], CultureInfo.InvariantCulture);
                string description = data["weather"][0]["description"];

                Console.WriteLine($"[Weather] description={description}, temp={temp}, humidity={humidity}");

                return new WeatherInfo
                {
                    Description = description,
                    Temperature = temp,
                    Humidity = humidity
                };
            }
        }
    }
}
