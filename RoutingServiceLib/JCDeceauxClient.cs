using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Web.Script.Serialization;
using RoutingServiceLib.Clients;

namespace RoutingServiceLib
{
    internal static class JcDecauxClient
    {
        static readonly HttpClient http = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
        private static readonly ProxyClient _proxy = new ProxyClient("http://localhost:9001/ProxyService");
        public static List<JcStation> GetStations(string contract = "Lyon")
        {
            var list = new List<JcStation>();
            try
            {
                var url = $"{Constants.JCDECAUX}/stations?contract={Uri.EscapeDataString(contract)}&apiKey={Constants.JCDECAUX_KEY}";
                Console.WriteLine($"[JCDecaux] Fetching via Proxy: {url}");
                //r resp = http.GetAsync(url).Result;
                //Console.WriteLine($"[JCDecaux] {url} -> {(int)resp.StatusCode} {resp.ReasonPhrase}");
                //if (!resp.IsSuccessStatusCode) return list;
                //var json = resp.Content.ReadAsStringAsync().Result;

                var json = _proxy.Get(url);
                var rows = new JavaScriptSerializer().Deserialize<object[]>(json);
                Console.WriteLine($"[JCDecaux] raw stations = {rows?.Length ?? 0}");

                foreach (var row in rows)
                {
                    var s = row as Dictionary<string, object>;
                    if (s == null) continue;

                    // position
                    var pos = (Dictionary<string, object>)s["position"];
                    double lat = Convert.ToDouble(pos["latitude"]);
                    double lng = Convert.ToDouble(pos["longitude"]);

                    // name
                    string name = (string)s["name"];

                    int bikes = 0, stands = 0;

                    if (s.ContainsKey("mainStands"))
                    {
                        var ms = (Dictionary<string, object>)s["mainStands"];
                        var av = (Dictionary<string, object>)ms["availabilities"];
                        bikes = SafeInt(av, "bikes");
                        stands = SafeInt(av, "stands");
                    }
                    else if (s.ContainsKey("totalStands"))
                    {
                        var ts = (Dictionary<string, object>)s["totalStands"];
                        var av = (Dictionary<string, object>)ts["availabilities"];
                        bikes = SafeInt(av, "bikes");
                        stands = SafeInt(av, "stands");
                    }

                    list.Add(new JcStation
                    {
                        name = name,
                        position = new LatLng { lat = lat, lng = lng },
                        available_bikes = bikes,
                        available_bike_stands = stands
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("[JCDecaux] " + ex.Message);
            }

            return list;
        }

        static int SafeInt(Dictionary<string, object> d, string key)
        {
            if (d == null || !d.ContainsKey(key) || d[key] == null) return 0;
            try { return Convert.ToInt32(d[key]); } catch { return 0; }
        }
    }
}
