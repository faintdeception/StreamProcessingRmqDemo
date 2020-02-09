using DomainModel;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using RabbitMQ.Client;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OrderProducer
{
    public class EventPublisher
    {
        private readonly IConfiguration Configuration;
        private readonly ConnectionFactory connectionFactory;
        private readonly IConnection connection;
        private readonly IModel channel;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1303:Do not pass literals as localized parameters", Justification = "This is a sample application.")]
        public EventPublisher(IConfiguration configuration)
        {
            Configuration = configuration ?? throw new ArgumentNullException(paramName:nameof(configuration), message: "Configuration is null.");

            var hostName = Configuration.GetSection("RabbitMQConnection:HostName").Value;

            var canParsePort = int.TryParse(Configuration.GetSection("RabbitMQConnection:Port").Value,
                out int port);

            if (!canParsePort)
                throw new Exception("Unable to parse rabbitmq port.");

            var canParseIsSsl = bool.TryParse(Configuration.GetSection("RabbitMQConnection:IsSSL").Value,
                out bool isSsl);

            if (!canParseIsSsl)
            {
                Log.Warning("Unable to parse isSsl config setting, assuming it's false");
            }

            if (isSsl)
            {
                connectionFactory.Ssl.Enabled = true;
                connectionFactory.Ssl.ServerName = hostName;
            }
            connectionFactory = new ConnectionFactory()
            {
                HostName = hostName,
                UserName = configuration.GetSection("RabbitMQConnection:Username").Value,
                Password = configuration.GetSection("RabbitMQConnection:Password").Value,
                Port = port
            };
            connection = connectionFactory.CreateConnection();
            channel = connection.CreateModel();

            
            

        }

        public void PublishOrderCreatedEvent(Order orderData, string eventName)
        {
            if (orderData == null)
                return;

            Log.Information($"Publishing {eventName} for report Id {orderData.Id}.");
            var dateTimeOffset = new DateTimeOffset(DateTime.Now.ToUniversalTime());
            var unixDateTime = dateTimeOffset.ToUnixTimeSeconds();

            try
            {

                        if (string.IsNullOrEmpty(eventName))
                            eventName = "orderProducer.unknown";




                        var exchangeName = Configuration.GetSection("RabbitMQConnection:Exchanges:Fanout").Value;

                        byte[] byteResponse;
                        string jsonResponse;

                        //factory.Uri = new Uri($"{protocol}://{username}:{password}@{hostname}");


                        //Set the message to persist in the event of a broker shutdown
                        var messageProperties = channel.CreateBasicProperties();
                        messageProperties.Persistent = true;
                        Dictionary<string, object> args = new Dictionary<string, object>()
                            {
                                { "x-dead-letter-routing-key", "orderproducer.dead-letter"},
                                { "x-dead-letter-exchange", exchangeName }
                            };

                        var queueName = "orderproducer.orders";
                        channel.ExchangeDeclare(exchangeName, ExchangeType.Fanout, true, false, args);

                        channel.BasicAcks -= Channel_BasicAcks;
                        channel.BasicAcks += Channel_BasicAcks;
                        channel.ConfirmSelect();
                        var orderEvent = new
                        {
                            EventName = eventName,
                            OrderData = orderData
                        };

                        jsonResponse = JsonConvert.SerializeObject(orderEvent);

                        byteResponse = Encoding.UTF8.GetBytes(jsonResponse);
                        Log.Information("Sending message to broker");
                        channel.BasicPublish(exchange: exchangeName,
                                             routingKey: queueName,
                                             basicProperties: messageProperties,
                                             body: byteResponse);

                        channel.WaitForConfirmsOrDie();


            }
            catch (Exception ex)
            {
                Log.Error($"Unable to publish message. Exception: {ex.Message} {Environment.NewLine} {ex?.InnerException?.Message}");
                throw;
            }

        }
        private void Channel_BasicAcks(object sender, RabbitMQ.Client.Events.BasicAckEventArgs e)
        {
            IEnumerable<ulong> ids = Enumerable.Repeat(e.DeliveryTag, 1);
            Log.Information("Broker Received Message {id}", ids);
        }
    }
}



