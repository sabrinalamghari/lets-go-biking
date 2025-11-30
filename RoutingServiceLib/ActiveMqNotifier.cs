using System;
using System.Web.Script.Serialization;
using Apache.NMS;
using Apache.NMS.ActiveMQ;

namespace RoutingServiceLib
{
    public static class ActiveMqNotifier
    {
        private static readonly IConnectionFactory _factory =
            new ConnectionFactory("activemq:tcp://localhost:61616");

        // envoie UNE alerte météo pour la destination
        public static void SendWeatherAlert(string destinationLabel, WeatherInfo info)
        {
            try
            {
                using (var connection = _factory.CreateConnection())
                {
                    connection.Start();

                    using (var session = connection.CreateSession(AcknowledgementMode.AutoAcknowledge))
                    {
                        IDestination dest = session.GetQueue("TP_LetsGo_Weather");
                        using (var producer = session.CreateProducer(dest))
                        {
                            producer.DeliveryMode = MsgDeliveryMode.NonPersistent;

                            var alert = new
                            {
                                type = "meteo",
                                severity = ComputeSeverity(info),
                                message = BuildMessage(destinationLabel, info),
                                city = destinationLabel,
                                temperature = info.Temperature,
                                humidity = info.Humidity,
                                timestamp = DateTime.UtcNow
                            };

                            var serializer = new JavaScriptSerializer();
                            string json = serializer.Serialize(alert);

                            ITextMessage msg = producer.CreateTextMessage(json);
                            producer.Send(msg);

                            Console.WriteLine("[ActiveMQ][Weather] " + json);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("[ActiveMQ][Weather] ERROR: " + ex.Message);
            }
        }

        private static string ComputeSeverity(WeatherInfo info)
        {
            if (info.Description != null &&
                info.Description.IndexOf("orage", StringComparison.OrdinalIgnoreCase) >= 0)
                return "critical";

            if (info.Description != null &&
                info.Description.IndexOf("pluie", StringComparison.OrdinalIgnoreCase) >= 0)
                return "warning";

            return "info";
        }

        private static string BuildMessage(string destinationLabel, WeatherInfo info)
        {
            return $"Météo à {destinationLabel} : {info.Description}, {info.Temperature:0}°C, humidité {info.Humidity}%";
        }
    }
}
