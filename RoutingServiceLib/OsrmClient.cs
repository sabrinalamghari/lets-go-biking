using System;
using System.Collections.Generic;
using System.Globalization;
using System.Web.Script.Serialization;
using System.Xml.Linq;
using RoutingServiceLib.Clients;
using Newtonsoft.Json.Linq;
using System.Linq;


namespace RoutingServiceLib
{
    internal static class OsrmClient
    {
        static readonly ProxyClient _proxy = new ProxyClient("http://localhost:9001/ProxyService");

        public static (double distance, double duration, List<string> steps, List<double[]> geometry) RouteFoot(LatLng a, LatLng b) =>
            Route(Constants.OSRM_FOOT, "foot", a, b);

        public static (double distance, double duration, List<string> steps, List<double[]> geometry) RouteBike(LatLng a, LatLng b) =>
            Route(Constants.OSRM_BIKE, "bike", a, b);

        public static (double, double, List<string>, List<double[]>) Route(string host, string profile, LatLng a, LatLng b)
        {
            var url =
                $"{host}/route/v1/{profile}/" +
                $"{a.lng.ToString(CultureInfo.InvariantCulture)},{a.lat.ToString(CultureInfo.InvariantCulture)};" +
                $"{b.lng.ToString(CultureInfo.InvariantCulture)},{b.lat.ToString(CultureInfo.InvariantCulture)}" +
                $"?overview=full&geometries=geojson&steps=true";

            try
            {
                var json = _proxy.GetRaw(url);
                var root = JObject.Parse(json);

                if (root["code"]?.ToString() != "Ok")
                    throw new Exception("OSRM error: " + root["code"]);

                var route = root["routes"][0];
                var geomCoords = route["geometry"]?["coordinates"]
                    ?.Select(c => new double[] { (double)c[1], (double)c[0] }) // lat, lon
                    .ToList()
                    ?? new List<double[]>();

                double distance = (double)route["distance"];
                double duration = (double)route["duration"];

                var stepsList = new List<string>();
                foreach (var leg in route["legs"])
                    foreach (var step in leg["steps"])
                    {
                        string name = (string)step["name"];
                        if (!string.IsNullOrWhiteSpace(name)) stepsList.Add(name);
                        else stepsList.Add((string)step["maneuver"]?["type"] ?? "continuer");
                    }

                if (stepsList.Count == 0) stepsList.Add("Suivre l'itinéraire.");
                return (distance, duration, stepsList, geomCoords);
            }
            catch (Exception ex)
            {
                Console.WriteLine("[OSRM] FAIL " + ex.Message);
                Console.WriteLine("[OSRM] URL = " + url);

                var dist = Haversine(a.lat, a.lng, b.lat, b.lng);

                var speed = profile == "bike" ? 4.5 : 1.3;

                return (dist, dist / speed, new List<string> { "Itinéraire approximatif." }, new List<double[]>());
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
