using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.ServiceModel.Web;

namespace RoutingServiceLib
{
    public class RoutingServiceImpl : IRoutingService, IRoutingServiceSoap
    {
       public RouteResult GetRoute(string from, string to)
        {
            // CORS
            WebOperationContext.Current.OutgoingResponse.Headers.Add("Access-Control-Allow-Origin", "*");
            WebOperationContext.Current.OutgoingResponse.Headers.Add("Access-Control-Allow-Methods", "GET, OPTIONS");
            WebOperationContext.Current.OutgoingResponse.Headers.Add("Access-Control-Allow-Headers", "Content-Type");

            var o = NominatimUtils.ParseOrGeocode(from);
            var d = NominatimUtils.ParseOrGeocode(to);
            if (o == null || d == null)
                return Error("Impossible de géocoder l'origine ou la destination.");

            var walkDirect = OsrmClient.RouteFoot(o, d);

            Console.WriteLine($"[Route] origin=({o.lat},{o.lng}) dest=({d.lat},{d.lng})");
            Console.WriteLine("[Route] fetching JCDecaux contracts…");

            var contracts = JcDecauxClient.GetContracts();

            var contractFrom = JcDecauxClient.FindContractForOneAddress(from, contracts);
            var contractTo = JcDecauxClient.FindContractForOneAddress(to, contracts);
            bool interCity = contractFrom != contractTo;

            if (interCity)
            {
                Console.WriteLine("[Route] Trajet inter-ville détecté.");

                var stationsA = JcDecauxClient.GetStations(contractFrom);
                var stationsB = JcDecauxClient.GetStations(contractTo);

                var startA = stationsA.Where(s => s.available_bikes > 0)
                                      .OrderBy(s => DistMeters(o, s.position))
                                      .FirstOrDefault();

                var dropA = stationsA.Where(s => s.available_bike_stands > 0)
                                     .OrderBy(s => DistMeters(d, s.position)) // direction vers destination
                                     .FirstOrDefault();

                var pickupB = stationsB.Where(s => s.available_bikes > 0)
                                       .OrderBy(s => DistMeters(o, s.position)) // direction venant de l'origine
                                       .FirstOrDefault();

                var endB = stationsB.Where(s => s.available_bike_stands > 0)
                                    .OrderBy(s => DistMeters(d, s.position))
                                    .FirstOrDefault();

                if (startA == null || dropA == null || pickupB == null || endB == null)
                    return WalkOnly(walkDirect, "Pas de stations dispo pour un trajet inter-ville.");

                var w0 = OsrmClient.RouteFoot(o, startA.position);
                var bA = OsrmClient.RouteBike(startA.position, dropA.position);
                var wMid = OsrmClient.RouteFoot(dropA.position, pickupB.position);
                var bB = OsrmClient.RouteBike(pickupB.position, endB.position);
                var wEnd = OsrmClient.RouteFoot(endB.position, d);

                // critère simple : on garde si les accès à pied sont raisonnables
                if (w0.distance > 2000 || wEnd.distance > 2000)
                    return WalkOnly(walkDirect, "Stations trop loin de l'origine ou de la destination.");

                var total = w0.duration + bA.duration + wMid.duration + bB.duration + wEnd.duration;

                return new RouteResult
                {
                    mode = "bike+walk+bike",
                    totalDistanceMeters = w0.distance + bA.distance + wMid.distance + bB.distance + wEnd.distance,
                    totalDurationSeconds = total,
                    legs = new List<RouteLeg> {
                        MakeLeg("walk", w0),
                        MakeLeg("bike", bA),
                        MakeLeg("walk", wMid),
                        MakeLeg("bike", bB),
                        MakeLeg("walk", wEnd),
                    },
                    note = $"Inter-ville: départ {contractFrom} ({startA.name} → {dropA.name}), arrivée {contractTo} ({pickupB.name} → {endB.name})."
                };
            }


            Console.WriteLine($"[Route] contractFrom = {contractFrom ?? "null"}");
            Console.WriteLine($"[Route] contractTo   = {contractTo ?? "null"}");

            if (string.IsNullOrEmpty(contractFrom) || string.IsNullOrEmpty(contractTo))
                return WalkOnly(walkDirect, "Aucun contrat JCDecaux trouvé pour l'origine ou la destination.");

            // ============================================================
            // CAS 1 : CONTRATS DIFFÉRENTS  => bike + walk(inter-ville) + bike
            // ============================================================
            if (!contractFrom.Equals(contractTo, StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine("[Route] Trajet inter-ville détecté.");

                var stationsO = JcDecauxClient.GetStations(contractFrom);
                var stationsD = JcDecauxClient.GetStations(contractTo);

                // candidates origine : stations avec vélos / stations avec places
                var startCandidatesA = stationsO.Where(s => s.available_bikes > 0)
                    .OrderBy(s => DistMeters(o, s.position))
                    .Take(8)
                    .ToList();

                var exitCandidatesA = stationsO.Where(s => s.available_bike_stands > 0)
                    .OrderBy(s => DistMeters(d, s.position)) // station "sortie" proche de la destination
                    .Take(8)
                    .ToList();

                if (startCandidatesA.Count == 0 || exitCandidatesA.Count == 0)
                    return WalkOnly(walkDirect, $"Pas de stations utilisables dans la ville d’origine ({contractFrom}).");

                // on choisit la meilleure paire startA -> exitA selon OSRM
                JcStation bestStartA = null, bestExitA = null;
                double bestA = double.MaxValue;

                foreach (var sA in startCandidatesA)
                {
                    foreach (var eA in exitCandidatesA)
                    {
                        var wA = OsrmClient.RouteFoot(o, sA.position);
                        var bA = OsrmClient.RouteBike(sA.position, eA.position);
                        var totalA = wA.duration + bA.duration;

                        if (totalA < bestA)
                        {
                            bestA = totalA;
                            bestStartA = sA;
                            bestExitA = eA;
                        }
                    }
                }

                var startA = bestStartA;
                var exitA = bestExitA;

                if (startA == null || exitA == null)
                    return WalkOnly(walkDirect, $"Pas de stations utilisables dans la ville d’origine ({contractFrom}).");

                // candidates arrivée : stations avec vélos proches de exitA / stations avec places proches de destination
                var entryCandidatesB = stationsD.Where(s => s.available_bikes > 0)
                    .OrderBy(s => DistMeters(exitA.position, s.position)) // station "entrée" proche de exitA
                    .Take(8)
                    .ToList();

                var endCandidatesB = stationsD.Where(s => s.available_bike_stands > 0)
                    .OrderBy(s => DistMeters(d, s.position))
                    .Take(8)
                    .ToList();

                if (entryCandidatesB.Count == 0 || endCandidatesB.Count == 0)
                    return WalkOnly(walkDirect, $"Pas de stations utilisables dans la ville d’arrivée ({contractTo}).");

                // meilleure paire entryB -> endB
                JcStation bestEntryB = null, bestEndB = null;
                double bestB = double.MaxValue;

                foreach (var sB in entryCandidatesB)
                {
                    foreach (var eB in endCandidatesB)
                    {
                        var bB = OsrmClient.RouteBike(sB.position, eB.position);
                        var wB = OsrmClient.RouteFoot(eB.position, d);
                        var totalB = bB.duration + wB.duration;

                        if (totalB < bestB)
                        {
                            bestB = totalB;
                            bestEntryB = sB;
                            bestEndB = eB;
                        }
                    }
                }

                var entryB = bestEntryB;
                var endB = bestEndB;

                if (entryB == null || endB == null)
                    return WalkOnly(walkDirect, $"Pas de stations utilisables dans la ville d’arrivée ({contractTo}).");

                // legs inter-ville
                var walk1 = OsrmClient.RouteFoot(o, startA.position);
                var bike1 = OsrmClient.RouteBike(startA.position, exitA.position);

                var walkMid = OsrmClient.RouteFoot(exitA.position, entryB.position);

                var bike2 = OsrmClient.RouteBike(entryB.position, endB.position);
                var walk2 = OsrmClient.RouteFoot(endB.position, d);

                // garde-fou pour éviter les trajets absurdes
                if (walkMid.distance > 50000) // 50km à pied entre villes -> on abandonne le mix
                    return WalkOnly(walkDirect, "Trajet inter-ville trop long pour un mix vélo/marche réaliste.");

                var totalInter =
                    walk1.duration + bike1.duration +
                    walkMid.duration +
                    bike2.duration + walk2.duration;

                return new RouteResult
                {
                    mode = "bike+walk+bike",
                    totalDistanceMeters =
                        walk1.distance + bike1.distance +
                        walkMid.distance +
                        bike2.distance + walk2.distance,
                    totalDurationSeconds = totalInter,
                    legs = new List<RouteLeg>
            {
                MakeLeg("walk", walk1),
                MakeLeg("bike", bike1),
                MakeLeg("walk", walkMid),
                MakeLeg("bike", bike2),
                MakeLeg("walk", walk2),
            },
                    note = $"Trajet inter-ville ({contractFrom} → {contractTo}). " +
                           $"Stations : {startA.name} → {exitA.name} → {entryB.name} → {endB.name}."
                };
            }

            // ============================================================
            // CAS 2 : MÊME CONTRAT  => ton code actuel bike+walk
            // ============================================================
            var contractName = contractFrom;
            Console.WriteLine("[Route] contrat choisi = " + contractName);
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

            if (!worthIt)
                return WalkOnly(walkDirect, "Le vélo n'apporte pas de gain significatif.");

            return new RouteResult
            {
                mode = "bike+walk",
                totalDistanceMeters = walkToBike.distance + bikeLeg.distance + walkToEnd.distance,
                totalDurationSeconds = totalBike,
                legs = new List<RouteLeg>
        {
            MakeLeg("walk", walkToBike),
            MakeLeg("bike", bikeLeg),
            MakeLeg("walk", walkToEnd),
        },
                note = $"Stations: départ '{start.name}', arrivée '{end.name}'."
            };
        }

        RouteResult WalkOnly(
            (double distance, double duration, List<string> steps, List<double[]> geometry) w,
            string note
            ) =>
            new RouteResult
        {
            mode = "walk_only",
            totalDistanceMeters = w.distance,
            totalDurationSeconds = w.duration,
            legs = new List<RouteLeg> { MakeLeg("walk", w) },
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


        RouteLeg MakeLeg(string type, (double distance, double duration, List<string> steps, List<double[]> geometry) r)
            => new RouteLeg
        {
            type = type,
            distanceMeters = r.distance,
            durationSeconds = r.duration,
            instructions = r.steps,
            geometry = r.geometry
        };

    }
}
