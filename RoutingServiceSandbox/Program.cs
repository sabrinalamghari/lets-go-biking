using System;
using System.Collections.Generic;
using RoutingServiceLib;

namespace RoutingServiceSandbox
{
    internal class Program
    {
        static void Main(string[] args)
        {
            try
            {
                List<JcStation> stations = JcDecauxClient.GetStations("Lyon");

                Console.WriteLine($"Stations récupérées : {stations.Count}");
                foreach (var s in stations)
                {
                    Console.WriteLine($"{s.name} - bikes: {s.available_bikes}, stands: {s.available_bike_stands}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("ERREUR : " + ex);
            }

            Console.WriteLine("Terminé, appuie sur une touche pour quitter...");
            Console.ReadKey();
        }
    }
}
