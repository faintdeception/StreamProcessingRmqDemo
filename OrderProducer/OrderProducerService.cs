using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using DomainModel;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OrderProducer;
using Serilog;

namespace OrderProducer
{
    public static class OrderProducerService
    {
        const string ORDERCREATED = "order.created";
        static EventPublisher eventPublisher;
        const int MAXORDERS = 99;
        static Timer orderTimer;
        static readonly List<Product> products = new List<Product>() { 
        new Product{Name = "apples", Id = Guid.Parse("{61c54bbd-c2c6-5271-96e7-009a87ff44bf}"), Emoji="🍎" },
        new Product{Name = "peaches", Id = Guid.Parse("{0caa0dad-35be-5f56-a8ff-afceeeaa6101}"), Emoji = "🍑"},
        new Product{Name = "strawberries", Id = Guid.Parse("{2c4de342-38b7-51cf-b940-2309a097f518}"), Emoji="🍓" },
        new Product{Name = "bananas", Id = Guid.Parse("{574e775e-4f2a-5b96-ac1e-a2962a402336}"), Emoji="🍌" },
        new Product{Name = "mangoes", Id = Guid.Parse("{b453ae62-4e3d-5e58-b989-0a998ec441b8}"), Emoji="🥭" }};

        static readonly Random randomGenerator = new Random(DateTime.Now.Millisecond);
        static void Main(string[] args)
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

            eventPublisher = new EventPublisher(config);

            var timerState = new ProducerState { Counter = 0 };

            //Create a timer for producing the orders
            orderTimer = new Timer(
                callback: new TimerCallback(CreateOrder),
                state: timerState,
                dueTime: 1000,
                period: 2000);

            
            Console.WriteLine($"{DateTime.Now:HH:mm:ss.fff}: done.");

            CreateHostBuilder(args).Build().Run();
        }

        private static void CreateOrder(object timerState)
        {   
            var state = timerState as ProducerState;
            if (state.Counter > MAXORDERS)
            {
                Log.Information($"Max reached. {state.TotalQuantityOrdered} items ordered.");
                orderTimer.Dispose();
                return; 
            }
            Interlocked.Increment(ref state.Counter);
            var randomProduct = products[randomGenerator.Next(0, 4)];
            var randomQuantity = randomGenerator.Next(1, 100);
            var order = new Order()
            {
                Id = Guid.NewGuid(),
                OrderNumber = state.Counter,
                OrderDate = DateTime.Now,
                Product = randomProduct,
                Quantity = randomQuantity
            };

            eventPublisher.PublishOrderCreatedEvent(order, ORDERCREATED);
            state.TotalQuantityOrdered += order.Quantity;

            JObject parsedJson = JObject.Parse(JsonConvert.SerializeObject(order));
            var jsonOutput = new StringBuilder();
            jsonOutput.AppendLine("Order Data:");
            jsonOutput.AppendLine(parsedJson.ToString());
            Log.Information(jsonOutput.ToString());
        }

        class ProducerState
        {
            public int Counter;
            public int TotalQuantityOrdered;
                
        }
        

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });

        
    }
}
