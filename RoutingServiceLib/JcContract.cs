using System.Collections.Generic;

namespace RoutingServiceLib
{
    public class JcContract
    {
        public string name { get; set; }
        public string commercial_name { get; set; }
        public List<string> cities { get; set; }
        public string country_code { get; set; }
    }
}
