using System.Collections.Generic;
using System.Runtime.Serialization;

namespace RoutingServiceLib
{
    [DataContract]
    public class LatLng
    {
        [DataMember] public double lat { get; set; }
        [DataMember] public double lng { get; set; }
    }

    [DataContract]
    public class RouteLeg
    {
        [DataMember] public string type { get; set; }                
        [DataMember] public double distanceMeters { get; set; }
        [DataMember] public double durationSeconds { get; set; }
        [DataMember] public List<string> instructions { get; set; }
        [DataMember] public List<double[]> geometry { get; set; }

    }

    [DataContract]
    public class RouteResult
    {
        [DataMember] public string mode { get; set; }                 
        [DataMember] public double totalDistanceMeters { get; set; }
        [DataMember] public double totalDurationSeconds { get; set; }
        [DataMember] public List<RouteLeg> legs { get; set; }
        [DataMember] public string note { get; set; }
    }

    public class JcStation
    {
        public string name { get; set; }
        public LatLng position { get; set; }
        public int available_bikes { get; set; }
        public int available_bike_stands { get; set; }
    }
}
