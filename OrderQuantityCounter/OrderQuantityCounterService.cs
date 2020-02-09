using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Polly;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.SystemConsole.Themes;

namespace OrderQuantityCounter
{
    static public class OrderQuantityCounterService
    {
        static HttpClient httpClient;
        static IConnection conn;
        static IModel channel;
        static readonly int quantityCounter;
        private static readonly HttpClient client = new HttpClient();

        public static IConfiguration Configuration { get; set; }

        private static void Consumer_Received(object sender, BasicDeliverEventArgs ea)
        {
            var body = ea.Body;
            var message = Encoding.UTF8.GetString(body);

            dynamic orderEvent = JsonConvert.DeserializeObject(message);


            if (string.IsNullOrEmpty(orderEvent?.OrderData?.Id?.ToString()))
            {
                Log.Warning($"No Id found for Order Number {orderEvent?.OrderData?.OrderData?.ReportNumberAsString}");
            }

            int.TryParse(orderEvent?.OrderData?.Quantity.ToString(), out int quantity);
            var productIcon = orderEvent?.OrderData?.Product?.Emoji;
            var orderNumber = orderEvent?.OrderData?.OrderNumber;
            if (quantity % 3 == 0 && quantity % 5 == 0)
            {
                Log.Information($"Order Number {orderNumber} is FizzBuzz {productIcon}");
            }
            else if (quantity % 3 == 0)
            {
                Log.Information($"Order Number {orderNumber} is Fizz {productIcon}");
            }
            else if (quantity % 5 == 0)
            {
                Log.Information($"Order Number {orderNumber} is Buzz {productIcon}");
            }
            else
            {
                Log.Information($"Order Number {orderNumber} is ---- {productIcon}");
            }

        }

        public static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            var environmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

            var configurationBuilder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{environmentName}.json", optional: true, reloadOnChange: true)
                .AddJsonFile("appsettings.secrets.json", optional: true)
                .AddEnvironmentVariables();
            var config = configurationBuilder.Build();
            Configuration = config;


            var logFileLocation = Configuration["LogFileDir"] + Configuration["LogFilePrefix"];

            Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
            .Enrich.FromLogContext()
            .WriteTo.Console(theme: AnsiConsoleTheme.Code, outputTemplate: "{MachineName} {EnvironmentUserName}: {Message:lj}{NewLine}{Exception}")
            .CreateLogger();


            var hostName = Configuration.GetSection("RabbitMQConnection:HostName").Value;
            var exchangeName = Configuration.GetSection("RabbitMQConnection:Exchanges:Fanout").Value;

            Int32.TryParse(Configuration.GetSection("RabbitMQConnection:Port").Value,
                out int port);
            Boolean.TryParse(Configuration.GetSection("RabbitMQConnection:IsSSL").Value,
                out bool isSsl);
            double.TryParse(Configuration["RabbitMQConnectionRetrySeconds"],
                out double rabbitMQRetryLengthInSeconds);


            ConnectionFactory connectionFactory = new ConnectionFactory()
            {
                HostName = hostName,
                UserName = Configuration.GetSection("RabbitMQConnection:Username").Value,
                Password = Configuration.GetSection("RabbitMQConnection:Password").Value,
                Port = port

            };
            if (isSsl)
            {
                connectionFactory.Ssl.Enabled = true;
                connectionFactory.Ssl.ServerName = hostName;
            }




            var retry = Policy
                .Handle<Exception>()
                .WaitAndRetryForever(
                retryAttempt => TimeSpan.FromSeconds(rabbitMQRetryLengthInSeconds),
                (exception, retryAttemptCount, context) =>
                {
                    var msg = $"Failed to connect to RabbitMQ Host {hostName} -- retrying in {TimeSpan.FromSeconds(rabbitMQRetryLengthInSeconds)} seconds ... Attempt # {retryAttemptCount}";
                    Log.Error(msg);
                });

            retry.Execute(() =>
            {
                conn = connectionFactory.CreateConnection();
            });

            channel = conn.CreateModel();
            var consumer = new EventingBasicConsumer(channel);

            #region Channel Event Handler Wireup
            channel.BasicAcks += Channel_BasicAcks;
            channel.BasicNacks += Channel_BasicNacks;
            channel.BasicRecoverOk += Channel_BasicRecoverOk;
            channel.BasicReturn += Channel_BasicReturn;
            channel.CallbackException += Channel_CallbackException;
            #endregion Channel Event Handler Wireup

            #region Consumer Event Handler Wireup
            consumer.Received += Consumer_Received;
            consumer.Registered += Consumer_Registered;
            consumer.Shutdown += Consumer_Shutdown;
            consumer.Unregistered += Consumer_Unregistered;
            consumer.ConsumerCancelled += Consumer_ConsumerCancelled;
            #endregion Consumer Event Handler Wireup



            var handler = new HttpClientHandler();



            httpClient = new HttpClient(handler);



            Log.Information("Starting Order Quantity Counter Service.");
            Log.Information($"Connected to Message Broker {hostName}");

            string queueName = "inventorycounter";
            Dictionary<string, object> queueArgs = new Dictionary<string, object>()
            {
                { "x-dead-letter-exchange", exchangeName },
                { "x-dead-letter-routing-key", "orderproducer.dead-letter"}
            };

            Log.Information($"Listening to queue {queueName}");

            channel.QueueDeclare(queueName, true, false, false, queueArgs);
            channel.QueueBind(queueName, exchangeName, "", queueArgs);
            channel.BasicConsume(queueName, true, consumer);

            BuildWebHost(args).Run();
        }

        public static IWebHost BuildWebHost(string[] args)
        {
            return WebHost.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((webHostBuilderContext, configurationBuilder) =>
            {

            })
            .UseStartup<Startup>().UseKestrel(options =>
            {
                options.Limits.MaxRequestBodySize = null; //unlimited
            })
            .Build();
        }

        private static void Channel_CallbackException(object sender, CallbackExceptionEventArgs e)
        {
            Log.Error(e.Exception, e?.Exception.Message);
        }

        private static void Channel_BasicReturn(object sender, BasicReturnEventArgs e)
        {
            Log.Information($"Basic Return - Reply Code {e.ReplyCode} Reply Text {e.ReplyText}");
        }

        private static void Channel_BasicRecoverOk(object sender, EventArgs e)
        {
            Log.Information("All message received before this point will be redelivered, all others will not be");
        }

        private static void Channel_BasicNacks(object sender, BasicNackEventArgs e)
        {
            Log.Information($"Broker returned Nack, Requeue:{e.Requeue} DeliveryTag:{e.DeliveryTag}");
        }

        private static void Channel_BasicAcks(object sender, BasicAckEventArgs e)
        {
            Log.Information($"Broker returned Ack,  DeliveryTag:{e.DeliveryTag}");
        }

        private static void Consumer_ConsumerCancelled(object sender, ConsumerEventArgs e)
        {
            Log.Information($"Consumer canceled. ConsumerTag:{e.ConsumerTag}.");
        }

        private static void Consumer_Unregistered(object sender, ConsumerEventArgs e)
        {
            Log.Information($"Consumer unregistered. ConsumerTag:{e.ConsumerTag}.");
        }

        private static void Consumer_Shutdown(object sender, ShutdownEventArgs e)
        {
            Log.Information($"Consumer Shutdown {e.ReplyCode} Reply Text {e.ReplyText}");
        }

        private static void Consumer_Registered(object sender, ConsumerEventArgs e)
        {
            Log.Information($"Consumer registered. ConsumerTag:{e.ConsumerTag}.");
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }
}
