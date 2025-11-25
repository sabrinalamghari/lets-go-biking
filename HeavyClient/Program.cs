using System;
using HeavyClient.RoutingSoapProxy;  
namespace HeavyClient
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var client = new RoutingServiceSoapClient();

            Console.WriteLine("=== Heavy Client SOAP — LetsGoBiking ===");
            Console.WriteLine();

            Console.Write("Adresse de départ : ");
            string from = Console.ReadLine();

            Console.Write("Adresse d'arrivée : ");
            string to = Console.ReadLine();

            Console.WriteLine();
            Console.WriteLine("Appel SOAP en cours...");
            Console.WriteLine();

            try
            {
                var result = client.GetRoute(from, to);

                Console.WriteLine($"Mode : {result.mode}");
                Console.WriteLine($"Distance totale : {result.totalDistanceMeters:N0} m");
                Console.WriteLine($"Durée totale   : {result.totalDurationSeconds:N0} s");
                Console.WriteLine($"Note           : {result.note}");
                Console.WriteLine();

                if (result.legs != null && result.legs.Length > 0)
                {
                    Console.WriteLine("Étapes :");
                    for (int i = 0; i < result.legs.Length; i++)
                    {
                        var leg = result.legs[i];
                        var emoji = leg.type == "bike" ? "🚲" : "🚶";

                        Console.WriteLine(
                            $"  {emoji} Étape {i + 1} ({leg.type}) — " +
                            $"{leg.distanceMeters:N0} m, {leg.durationSeconds:N0} s");

                        if (leg.instructions != null)
                        {
                            foreach (var instr in leg.instructions)
                                Console.WriteLine($"     • {instr}");
                        }
                    }
                }
                else
                {
                    Console.WriteLine("Aucune étape renvoyée.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("❌ Erreur SOAP :");
                Console.WriteLine(ex.Message);
                if (ex.InnerException != null)
                    Console.WriteLine(ex.InnerException.Message);
            }

            Console.WriteLine();
            Console.WriteLine("Appuie sur Entrée pour quitter.");
            Console.ReadLine();
        }
    }
}
