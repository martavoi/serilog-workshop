using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using StructureMap;
using Swashbuckle.AspNetCore.Swagger;
using Workshop.Api.Middleware;

namespace Workshop.Api
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public IServiceProvider ConfigureServices(IServiceCollection services)
        {
            services.AddMvcCore()
                .AddJsonFormatters(settings =>
                {
                    settings.Formatting = Formatting.Indented;
                })
                .AddApiExplorer();
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new Info { Title = "Workshop API", Version = "v1" });
            });
            
            var container = new Container();
            container.Configure(config =>
            {
                config.AddRegistry(new Registry(new Config(Configuration)));
                config.Populate(services);
            });

            return container.GetInstance<IServiceProvider>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IServiceProvider serviceProvider, ILoggerFactory loggerFactory)
        {
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "Workshop.API V1");
            });
            
            app.UseCorrelationHeader(loggerFactory);
            app.UseMvc();
        }
    }
}
