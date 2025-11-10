using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Web.Script.Serialization;

namespace RoutingServiceLib
{
    internal static class OsrmClient
    {
        static readonly HttpClient http = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };

        public static (double distance, double duration, List<string> steps) RouteFoot(LatLng a, LatLng b) =>
            Route($"{Constants.OSRM_FOOT}", a, b);

        public static (double distance, double duration, List<string> steps) RouteBike(LatLng a, LatLng b) =>
            Route($"{Constants.OSRM_BIKE}", a, b);

        static (double, double, List<string>) Route(string baseUrl, LatLng a, LatLng b)
        {
            var url =
                $"{baseUrl}/route/v1/driving/{a.lng.ToString(CultureInfo.InvariantCulture)},{a.lat.ToString(CultureInfo.InvariantCulture)};" +
                $"{b.lng.ToString(CultureInfo.InvariantCulture)},{b.lat.ToString(CultureInfo.InvariantCulture)}?overview=false&steps=true";

            try
            {
                var json = http.GetStringAsync(url).Result;
                dynamic data = new JavaScriptSerializer().Deserialize<dynamic>(json);
                var route = data["routes"][0];
                double distance = route["distance"];
                double duration = route["duration"];
                var steps = new List<string>();
                foreach (var leg in route["legs"])
                    foreach (var s in leg["steps"])
                        steps.Add((string)s["name"]);
                if (steps.Count == 0) steps.Add("Suivre l'itinéraire.");
                return (distance, duration, steps);
            }
            catch
            {
                var dist = Haversine(a.lat, a.lng, b.lat, b.lng);
                var speed = baseUrl.Contains("bike") ? 4.5 : 1.3; // m/s
                return (dist, dist / speed, new List<string> { "Itinéraire approximatif." });
            }
        }

        static double Haversine(double lat1, double lon1, double lat2, double lon2)
        {
            double R = 6371000;
            double dLat = (lat2 - lat1) * Math.PI / 180.0;
            double dLon = (lon2 - lon1) * Math.PI / 180.0;
            double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                       Math.Cos(lat1 * Math.PI / 180.0) * Math.Cos(lat2 * Math.PI / 180.0) *
                       Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            return 2 * R * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        }
    }
}
