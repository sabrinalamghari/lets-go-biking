using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace RoutingServiceLib
{
    public class RoutingServiceImpl : IRoutingService
    {
        public RouteResult GetRoute(string from, string to)
        {
            var o = NominatimUtils.ParseOrGeocode(from);
            var d = NominatimUtils.ParseOrGeocode(to);
            if (o == null || d == null) return Error("Impossible de géocoder l'origine ou la destination.");

            var walkDirect = OsrmClient.RouteFoot(o, d);
            Console.WriteLine($"[Route] origin=({o.lat},{o.lng}) dest=({d.lat},{d.lng})");
            Console.WriteLine("[Route] fetching JCDecaux stations for Lyon…");
            var stations = JcDecauxClient.GetStations("Lyon");
            Console.WriteLine($"[Route] stations fetched = {stations?.Count ?? 0}");
            var start = stations.Where(s => s.available_bikes > 0)
                                .OrderBy(s => Dist(o, s.position)).FirstOrDefault();
            var end = stations.Where(s => s.available_bike_stands > 0)
                                .OrderBy(s => Dist(d, s.position)).FirstOrDefault();

            if (start == null || end == null)
                return WalkOnly(walkDirect, "Pas de station disponible (vélos/places).");

            var walkToBike = OsrmClient.RouteFoot(o, start.position);
            var bikeLeg = OsrmClient.RouteBike(start.position, end.position);
            var walkToEnd = OsrmClient.RouteFoot(end.position, d);

            var totalBike = walkToBike.duration + bikeLeg.duration + walkToEnd.duration;
            bool worthIt = totalBike < (0.9 * walkDirect.duration) &&
                            walkToBike.distance < 800 && walkToEnd.distance < 800; 

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
        double Dist(LatLng a, LatLng b) => Math.Sqrt(Math.Pow(a.lat - b.lat, 2) + Math.Pow(a.lng - b.lng, 2)); 
    }
}
