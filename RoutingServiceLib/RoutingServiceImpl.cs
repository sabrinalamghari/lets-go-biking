using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.ServiceModel.Web;

namespace RoutingServiceLib
{
    public class RoutingServiceImpl : IRoutingService
    {
        public RouteResult GetRoute(string from, string to)
        {
            WebOperationContext.Current.OutgoingResponse.Headers.Add("Access-Control-Allow-Origin", "*");
            WebOperationContext.Current.OutgoingResponse.Headers.Add("Access-Control-Allow-Methods", "GET, OPTIONS");
            WebOperationContext.Current.OutgoingResponse.Headers.Add("Access-Control-Allow-Headers", "Content-Type");

            var o = NominatimUtils.ParseOrGeocode(from);
            var d = NominatimUtils.ParseOrGeocode(to);
            if (o == null || d == null) return Error("Impossible de géocoder l'origine ou la destination.");

            var walkDirect = OsrmClient.RouteFoot(o, d);
            Console.WriteLine($"[Route] origin=({o.lat},{o.lng}) dest=({d.lat},{d.lng})");
            Console.WriteLine("[Route] fetching JCDecaux contracts…");
            var contracts = JcDecauxClient.GetContracts();
            Console.WriteLine("=== [DEBUG Contracts] ===");
            foreach (var c in contracts)
            {
                Console.WriteLine($"Name={c.name}, Commercial={c.commercial_name}, Country={c.country_code}, Cities=[{string.Join(",", c.cities ?? new List<string>())}]");
            }
            Console.WriteLine("=========================");
            var contractFrom = JcDecauxClient.FindContractForOneAddress(from, contracts);
            var contractTo = JcDecauxClient.FindContractForOneAddress(to, contracts);

            Console.WriteLine($"[Route] contractFrom = {contractFrom ?? "null"}");
            Console.WriteLine($"[Route] contractTo   = {contractTo ?? "null"}");

            if (string.IsNullOrEmpty(contractFrom) || string.IsNullOrEmpty(contractTo))
                return WalkOnly(walkDirect, "Aucun contrat JCDecaux trouvé pour l'origine ou la destination.");

            if (contractFrom != contractTo)
                return WalkOnly(walkDirect,
                    $"Origine et destination dans 2 contrats différents ({contractFrom} → {contractTo}). Trajet vélo JCDecaux impossible.");

            var contractName = contractFrom; 

            Console.WriteLine("[Route] contract choisi = " + (contractName ?? "null"));

            if (string.IsNullOrEmpty(contractName))
                return WalkOnly(walkDirect, "Aucun contrat JCDecaux trouvé pour cette zone.");

            Console.WriteLine($"[Route] fetching JCDecaux stations for {contractName}…");

            var stations = JcDecauxClient.GetStations(contractName);
            var startCandidates = stations.Where(s => s.available_bikes > 0)
                .OrderBy(s => DistMeters(o, s.position))
                .Take(8)
                .ToList();

            var endCandidates = stations.Where(s => s.available_bike_stands > 0)
                .OrderBy(s => DistMeters(d, s.position))
                .Take(8)
                .ToList();

            if (startCandidates.Count == 0 || endCandidates.Count == 0)
                return WalkOnly(walkDirect, "Pas de station disponible (vélos/places).");

            // on teste les combinaisons et on garde la meilleure
            JcStation bestStart = null, bestEnd = null;
            double bestTotal = double.MaxValue;

            foreach (var s in startCandidates)
            {
                foreach (var e2 in endCandidates)
                {
                    var w1 = OsrmClient.RouteFoot(o, s.position);
                    var b1 = OsrmClient.RouteBike(s.position, e2.position);
                    var w2 = OsrmClient.RouteFoot(e2.position, d);

                    var total = w1.duration + b1.duration + w2.duration;

                    if (total < bestTotal)
                    {
                        bestTotal = total;
                        bestStart = s;
                        bestEnd = e2;
                    }
                }
            }

            var start = bestStart;
            var end = bestEnd;

            Console.WriteLine($"[DEBUG] stations total = {stations.Count}");
            Console.WriteLine($"[DEBUG] start = {(start == null ? "null" : start.name)} bikes={start?.available_bikes}");
            Console.WriteLine($"[DEBUG] end   = {(end == null ? "null" : end.name)} stands={end?.available_bike_stands}");

            if (start == null || end == null)
                return WalkOnly(walkDirect, "Pas de station disponible (vélos/places).");

            var walkToBike = OsrmClient.RouteFoot(o, start.position);
            var bikeLeg = OsrmClient.RouteBike(start.position, end.position);
            var walkToEnd = OsrmClient.RouteFoot(end.position, d);

            var totalBike = walkToBike.duration + bikeLeg.duration + walkToEnd.duration;
            bool worthIt =
                totalBike < (0.95 * walkDirect.duration) &&
                walkToBike.distance < 2000 &&
                walkToEnd.distance < 2000;


            Console.WriteLine($"[DEBUG] walkDirect: {walkDirect.duration}s / {walkDirect.distance}m");
            Console.WriteLine($"[DEBUG] walkToBike: {walkToBike.duration}s / {walkToBike.distance}m");
            Console.WriteLine($"[DEBUG] bikeLeg:    {bikeLeg.duration}s / {bikeLeg.distance}m");
            Console.WriteLine($"[DEBUG] walkToEnd:  {walkToEnd.duration}s / {walkToEnd.distance}m");
            Console.WriteLine($"[DEBUG] totalBike = {totalBike}s");
            Console.WriteLine($"[DEBUG] worthIt = {worthIt}");


            if (!worthIt) return WalkOnly(walkDirect, "Le vélo n'apporte pas de gain significatif.");

            return new RouteResult
            {
                mode = "bike+walk",
                totalDistanceMeters = walkToBike.distance + bikeLeg.distance + walkToEnd.distance,
                totalDurationSeconds = totalBike,
                legs = new List<RouteLeg> {
                    new RouteLeg{ type="walk", distanceMeters=walkToBike.distance, durationSeconds=walkToBike.duration, instructions=walkToBike.steps },
                    new RouteLeg{ type="bike", distanceMeters=bikeLeg.distance,   durationSeconds=bikeLeg.duration,     instructions=bikeLeg.steps },
                    new RouteLeg{ type="walk", distanceMeters=walkToEnd.distance, durationSeconds=walkToEnd.duration,   instructions=walkToEnd.steps },
                },
                note = $"Stations: départ '{start.name}', arrivée '{end.name}'."
            };
        }
        RouteResult WalkOnly((double distance, double duration, List<string> steps) w, string note) =>
            new RouteResult
            {
                mode = "walk_only",
                totalDistanceMeters = w.distance,
                totalDurationSeconds = w.duration,
                legs = new List<RouteLeg> { new RouteLeg { type = "walk", distanceMeters = w.distance, durationSeconds = w.duration, instructions = w.steps } },
                note = note
            };
        RouteResult Error(string msg) => new RouteResult { mode = "error", note = msg, totalDistanceMeters = 0, totalDurationSeconds = 0, legs = new List<RouteLeg>() };

        // on a choisi de calculer localement la distance géographique (Haversine) 
        // entre l'utilisateur et les stations pour éviter de faire des centaines
        // d'appels OSRM 
        double DistMeters(LatLng a, LatLng b)
        {
            double R = 6371000;
            double lat1 = a.lat * Math.PI / 180.0;
            double lat2 = b.lat * Math.PI / 180.0;
            double dLat = (b.lat - a.lat) * Math.PI / 180.0;
            double dLon = (b.lng - a.lng) * Math.PI / 180.0;

            double h =
                Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(lat1) * Math.Cos(lat2) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

            return 2 * R * Math.Atan2(Math.Sqrt(h), Math.Sqrt(1 - h));
        }


        public void OptionsRoute()
        {
            WebOperationContext.Current.OutgoingResponse.Headers.Add("Access-Control-Allow-Origin", "*");
            WebOperationContext.Current.OutgoingResponse.Headers.Add("Access-Control-Allow-Methods", "GET, OPTIONS");
            WebOperationContext.Current.OutgoingResponse.Headers.Add("Access-Control-Allow-Headers", "Content-Type");
        }


    }
}
