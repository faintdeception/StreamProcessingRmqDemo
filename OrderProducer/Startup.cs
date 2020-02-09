using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.IO;
using Serilog;
using System.Text;

namespace OrderProducer
{
    public class Startup
    {

        public IConfiguration Configuration { get; set; }
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
            Log.Logger = new LoggerConfiguration()
            .WriteTo.Console()
            .CreateLogger();
        }

       

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        static public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapGet("/", async context =>
                {
                    StringBuilder welcomeMessage = new StringBuilder();
                    welcomeMessage.Append(@"
   ____           __             ____                 __                    
  / __ \_________/ /__  _____   / __ \_________  ____/ /_  __________  _____
 / / / / ___/ __  / _ \/ ___/  / /_/ / ___/ __ \/ __  / / / / ___/ _ \/ ___/
/ /_/ / /  / /_/ /  __/ /     / ____/ /  / /_/ / /_/ / /_/ / /__/  __/ /    
\____/_/   \__,_/\___/_/     /_/   /_/   \____/\__,_/\__,_/\___/\___/_/     
                                                                            


");
                    await context.Response.WriteAsync(welcomeMessage.ToString()).ConfigureAwait(true);
                });
            });
        }
    }
}
