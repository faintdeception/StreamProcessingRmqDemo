using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace LargeOrderAlertService
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
.____                                 ________            .___                _____  .__                 __   
|    |   _____ _______  ____   ____   \_____  \_______  __| _/___________    /  _  \ |  |   ____________/  |_ 
|    |   \__  \\_  __ \/ ___\_/ __ \   /   |   \_  __ \/ __ |/ __ \_  __ \  /  /_\  \|  | _/ __ \_  __ \   __\
|    |___ / __ \|  | \/ /_/  >  ___/  /    |    \  | \/ /_/ \  ___/|  | \/ /    |    \  |_\  ___/|  | \/|  |  
|_______ (____  /__|  \___  / \___  > \_______  /__|  \____ |\___  >__|    \____|__  /____/\___  >__|   |__|  
        \/    \/     /_____/      \/          \/           \/    \/                \/          \/             
");
                    await context.Response.WriteAsync(welcomeMessage.ToString());
                });
            });
        }
    }
}
