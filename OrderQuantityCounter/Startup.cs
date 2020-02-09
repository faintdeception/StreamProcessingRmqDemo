using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace OrderQuantityCounter
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
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "This method gets called by the runtime.")]
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
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
________            .___             _________                      __                
\_____  \_______  __| _/___________  \_   ___ \  ____  __ __  _____/  |_  ___________ 
 /   |   \_  __ \/ __ |/ __ \_  __ \ /    \  \/ /  _ \|  |  \/    \   __\/ __ \_  __ \
/    |    \  | \/ /_/ \  ___/|  | \/ \     \___(  <_> )  |  /   |  \  | \  ___/|  | \/
\_______  /__|  \____ |\___  >__|     \______  /\____/|____/|___|  /__|  \___  >__|   
        \/           \/    \/                \/                  \/          \/       

");
                    await context.Response.WriteAsync(welcomeMessage.ToString());
                });
            });
        }
    }
}
