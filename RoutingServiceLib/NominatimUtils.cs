using System;
using System.Globalization;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Web;
using System.Web.Script.Serialization;
using RoutingServiceLib.Clients;

namespace RoutingServiceLib
{
    internal static class NominatimUtils
    {
        static readonly HttpClient http;
        static readonly ProxyClient _proxy = new ProxyClient("http://localhost:9001/ProxyService");

        static NominatimUtils()
        {
            http = new HttpClient();

            http.DefaultRequestHeaders.UserAgent.ParseAdd(
                "LetsGoBiking/1.0 (sabrinalmghari@gmail.com)");

            http.DefaultRequestHeaders.From = "sabrinalmgharo@gmail.com";
        }

        public static LatLng ParseOrGeocode(string q)
        {
            if (string.IsNullOrWhiteSpace(q)) return null;

            q = Uri.UnescapeDataString(q.Trim()).Replace('+', ' ');

            var parts = q.Split(',');
            if (parts.Length == 2 &&
                double.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var la) &&
                double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var lg))
                return new LatLng { lat = la, lng = lg };

            var candidates = new[]
            {
                q,
                q.IndexOf("France", StringComparison.OrdinalIgnoreCase) >= 0 ? null : q + ", France",
                q.Equals("Gare de Lyon", StringComparison.OrdinalIgnoreCase)
                    ? "Gare de Lyon, Paris, France"
                    : null
            };

            foreach (var c in candidates)
            {
                if (string.IsNullOrWhiteSpace(c)) continue;

                var r = GeocodeNominatim(c, country: "fr");
                if (r != null) return r;
            }

            return null;
        }

        static LatLng GeocodeNominatim(string address, string country = null)
        {
            try
            {
                var qp = HttpUtility.UrlEncode(address);
                var cc = string.IsNullOrEmpty(country) ? "" : $"&countrycodes={country}";
                var url =
                    $"https://nominatim.openstreetmap.org/search?q={qp}&format=jsonv2&limit=1&addressdetails=0{cc}";
                Console.WriteLine("[Nominatim] " + url);

                string json = null;

                try
                {
                    json = _proxy.GetRawTtl(url, 2);
                }
                catch
                {
                    var resp = http.GetAsync(url).Result;
                    Console.WriteLine($"[Nominatim] HTTP {(int)resp.StatusCode} {resp.ReasonPhrase}");

                    if ((int)resp.StatusCode == 429)
                    {
                        Thread.Sleep(1200);
                        return null;
                    }

                    if (!resp.IsSuccessStatusCode) return null;

                    json = resp.Content.ReadAsStringAsync().Result;
                }


                object[] rows = new JavaScriptSerializer().Deserialize<object[]>(json);
                if (rows != null && rows.Length > 0)
                {
                    var first = rows[0] as Dictionary<string, object>;
                    if (first != null && first.ContainsKey("lat") && first.ContainsKey("lon"))
                    {
                        double lat = double.Parse(first["lat"].ToString(), CultureInfo.InvariantCulture);
                        double lon = double.Parse(first["lon"].ToString(), CultureInfo.InvariantCulture);
                        return new LatLng { lat = lat, lng = lon };
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("[Nominatim] " + ex.Message);
            }

            return null;
        }
    }
}
