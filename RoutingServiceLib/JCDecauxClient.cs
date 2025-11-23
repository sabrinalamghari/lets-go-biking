using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Web.Script.Serialization;
using RoutingServiceLib.Clients;

namespace RoutingServiceLib
{
    public static class JcDecauxClient
    {
        private static readonly ProxyClient _proxy = new ProxyClient("http://localhost:9001/ProxyService");
        public static List<JcStation> GetStations(string contract = "Lyon")
        {
            var list = new List<JcStation>();
            try
            {
                var json = _proxy.GetStationsJson(contract);
                var rows = new JavaScriptSerializer().Deserialize<object[]>(json);
                Console.WriteLine($"[JCDecaux] raw stations = {rows?.Length ?? 0}");

                if (rows == null)
                {
                    return list;
                }

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



        public static List<JcContract> GetContracts()
        {
            var list = new List<JcContract>();
            try
            {
                var json = _proxy.GetContractsJson();
                var rows = new JavaScriptSerializer().Deserialize<object[]>(json);
                if (rows == null) return list;

                foreach (var row in rows)
                {
                    var d = row as Dictionary<string, object>;
                    if (d == null) continue;

                    // name
                    string name = d.TryGetValue("name", out var nameObj) && nameObj != null
                        ? nameObj.ToString()
                        : null;

                    // commercial_name
                    string commercial = d.TryGetValue("commercial_name", out var commObj) && commObj != null
                        ? commObj.ToString()
                        : null;

                    // country_code
                    string country = d.TryGetValue("country_code", out var countryObj) && countryObj != null
                        ? countryObj.ToString()
                        : null;

                    // cities (array)
                    var cities = new List<string>();
                    if (d.TryGetValue("cities", out var citiesObj) && citiesObj is object[] arr)
                    {
                        foreach (var c in arr)
                            if (c != null) cities.Add(c.ToString());
                    }

                    list.Add(new JcContract
                    {
                        name = name,
                        commercial_name = commercial,
                        country_code = country,
                        cities = cities
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("[JCDecaux][contracts] " + ex.Message);
            }

            return list;
        }
        public static string FindBestContract(string from, string to, List<JcContract> contracts)
        {
            if (contracts == null || contracts.Count == 0) return null;

            string hay = (from + " " + to).ToLowerInvariant();
            var byCity = contracts.FirstOrDefault(c =>
                c.cities != null && c.cities.Any(city => hay.Contains(city.ToLowerInvariant()))
            );
            if (byCity != null) return byCity.name;


            var byCommercial = contracts.FirstOrDefault(c =>
                !string.IsNullOrEmpty(c.commercial_name) &&
                hay.Contains(c.commercial_name.ToLowerInvariant())
            );
            if (byCommercial != null) return byCommercial.name;

            var fr = contracts.FirstOrDefault(c => c.country_code == "FR");
            return null;
        }


        public static string FindContractForOneAddress(string addr, List<JcContract> contracts)
        {
            if (contracts == null || contracts.Count == 0) return null;

            string hay = (addr ?? "").ToLowerInvariant();

            var byCity = contracts.FirstOrDefault(c =>
                c.cities != null && c.cities.Any(city => hay.Contains(city.ToLowerInvariant()))
            );
            if (byCity != null) return byCity.name;

            var byCommercial = contracts.FirstOrDefault(c =>
                !string.IsNullOrEmpty(c.commercial_name) &&
                hay.Contains(c.commercial_name.ToLowerInvariant())
            );
            if (byCommercial != null) return byCommercial.name;

            return null; 
        }


    }


}
