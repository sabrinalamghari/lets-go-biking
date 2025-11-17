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
            // géocodage
            var origin = NominatimUtils.ParseOrGeocode(from);
            var destination = NominatimUtils.ParseOrGeocode(to);
            if (origin == null || destination == null) return Error("Impossible de géocoder l'origine ou la destination.");

            // trajet direct à pied
            var walkDirect = OsrmClient.RouteFoot(origin, destination);
            Logger.Info($"Route request: origin=({origin.lat},{origin.lng}) dest=({destination.lat},{destination.lng})");
            
            // récup des stations JCDecaux via Proxy
            Logger.Info("[Route] fetching JCDecaux stations for Lyon…");
            var stations = JcDecauxClient.GetStations("Lyon");

            // trouver meilleure station de départ et d'arrivée
            var startStation = StationSelector.FindNearestWithBikes(stations, origin);
            var endStation = StationSelector.FindNearestWithStands(stations, destination);

            if (startStation == null || endStation == null)
            {
                Logger.Warn("Aucune station valide trouvée.");
                return new RouteResult { note = "Pas de station disponible (vélos/places)" };
            }

            // vérifier distance max à pied
            if (!StationSelector.IsStationCloseEnough(origin, startStation.position) ||
                !StationSelector.IsStationCloseEnough(destination, endStation.position))
            {
                Logger.Warn("Stations trop éloignées, trajet à pied recommandé.");
                return RouteFootOnly(origin, destination);
            }

            // trajets détaillés
            var walkToBike = OsrmClient.RouteFoot(origin, startStation.position);
            var bikeLeg = OsrmClient.RouteBike(startStation.position, endStation.position);
            var walkToEnd = OsrmClient.RouteFoot(endStation.position, destination);

            var totalBike = walkToBike.duration + bikeLeg.duration + walkToEnd.duration;
            
            bool worthIt = 
                totalBike < (0.9 * walkDirect.duration) &&
                walkToBike.distance < 800 && 
                walkToEnd.distance < 800;

            if (!worthIt)
            {
                Logger.Info("Vélo non pertinent -> marche uniquement.");
                return WalkOnly(walkDirect, "Le vélo n'apporte pas de gain significatif.");
            }

            // retourner trajet vélo + marche
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
                
                note = $"Stations: départ '{startStation.name}', arrivée '{endStation.name}'."
            };
        }


        RouteResult WalkOnly((double distance, double duration, List<string> steps) w, string note) =>
            new RouteResult
            {
                mode = "walk_only",
                totalDistanceMeters = w.distance,
                totalDurationSeconds = w.duration,
                legs = new List<RouteLeg> 
                { 
                    new RouteLeg 
                    { 
                        type = "walk", 
                        distanceMeters = w.distance, 
                        durationSeconds = w.duration, 
                        instructions = w.steps 
                    } 
                },
                note = note
            };

        RouteResult RouteFootOnly(LatLng origin, LatLng destination)
        {
            var w = OsrmClient.RouteFoot(origin, destination);
            return WalkOnly(w, "Stations trop éloignées, trajet à pied recommandé.");
        }

        RouteResult Error(string msg) => new RouteResult 
        { 
            mode = "error", 
            note = msg, 
            totalDistanceMeters = 0, 
            totalDurationSeconds = 0, 
            legs = new List<RouteLeg>() 
        };
    }
}
