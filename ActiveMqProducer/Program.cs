using System;
using System.Threading;
using Apache.NMS;
using Apache.NMS.ActiveMQ;
using System.Text.Json;

namespace ActiveMqProducer
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("=== ActiveMq C# Producer ===");

            string brokerUri = "activemq:tcp://localhost:61616";
            IConnectionFactory factory = new ConnectionFactory(brokerUri);

            using (var connection = factory.CreateConnection())
            {
                connection.Start();

                using (var session = connection.CreateSession(AcknowledgementMode.AutoAcknowledge))
                {
                    IDestination destination = session.GetQueue("TP_LetsGo");

                    using (var producer = session.CreateProducer(destination))
                    {
                        producer.DeliveryMode = MsgDeliveryMode.NonPersistent;

                        Console.WriteLine("Envoi de messages dans la queue 'TP_LetsGo'.");
                        Console.WriteLine("Ctrl+C pour arrêter.\n");

                        int counter = 0;

                        while (true)
                        {
                            counter++;

                            while (true)
                            {
                                counter++;

                                string[] types = { "meteo", "pollution", "bike" };
                                string type = types[counter % types.Length];

                                string severity;
                                int mod = counter % 5;

                                switch (mod)
                                {
                                    case 0:
                                    case 1:
                                        severity = "info";
                                        break;
                                    case 2:
                                    case 3:
                                        severity = "warning";
                                        break;
                                    default:
                                        severity = "critical";
                                        break;
                                }

                                string message;

                                if (type == "meteo")
                                {
                                    if (severity == "critical")
                                        message = "Orage violent imminent ⚡";
                                    else
                                        message = "Ciel couvert, risque de pluie 🌧";
                                }
                                else if (type == "pollution")
                                {
                                    if (severity == "critical")
                                        message = "Pic de pollution important 🚫";
                                    else
                                        message = "Qualité de l'air moyenne";
                                }
                                else if (type == "bike")
                                {
                                    if (severity == "critical")
                                        message = "Plus de vélos à la station de départ ❌";
                                    else
                                        message = "Disponibilité des vélos correcte";
                                }
                                else
                                {
                                    message = "Info";
                                }


                                var alert = new
                                {
                                    type,
                                    severity,
                                    message,
                                    timestamp = DateTime.UtcNow
                                };

                                string json = JsonSerializer.Serialize(alert);

                                ITextMessage msg = producer.CreateTextMessage(json);
                                producer.Send(msg);

                                Console.WriteLine("[SENT] " + json);

                                Thread.Sleep(3000);
                            }
                            Thread.Sleep(2000); 
                        }
                    }
                }
            }
        }
    }
}
