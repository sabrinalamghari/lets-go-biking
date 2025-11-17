using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoutingServiceLib
{
    internal class StationSelector
    {
        private const double MAX_WALK_DISTANCE = 500;


        public static JcStation FindNearestWithBikes(List<JcStation> stations, LatLng origin)
        {
            return stations
                .Where(s => s.available_bikes > 0)
                .OrderBy(s => Distance(origin, s.position))
                .FirstOrDefault();
        }

        public static JcStation FindNearestWithStands(List<JcStation> stations, LatLng destination)
        {
            return stations
                .Where(s => s.available_bike_stands > 0)
                .OrderBy(s => Distance(destination, s.position))
                .FirstOrDefault();
        }

        public static bool IsStationCloseEnough(LatLng point, LatLng station)
        {
            return Distance(point, station) <= MAX_WALK_DISTANCE;
        }

        private static double Distance(LatLng a, LatLng b)
        {
            var R = 6371000; // rayon de la Terre (m)
            var lat1 = a.lat * Math.PI / 180;
            var lat2 = b.lat * Math.PI / 180;
            var dLat = (b.lat - a.lat) * Math.PI / 180;
            var dLng = (b.lng - a.lng) * Math.PI / 180;

            var h = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos(lat1) * Math.Cos(lat2) *
                    Math.Sin(dLng / 2) * Math.Sin(dLng / 2);
            var c = 2 * Math.Atan2(Math.Sqrt(h), Math.Sqrt(1 - h));
            return R * c;
        }
    }
}
