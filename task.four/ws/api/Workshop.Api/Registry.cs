using System;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.Elasticsearch;

namespace Workshop.Api
{
    public class Registry: StructureMap.Registry
    {
        public Registry(Config conf)
        {   
            var loggerFactory = new LoggerFactory()
                .AddSerilog(new LoggerConfiguration()
                    .MinimumLevel.Verbose()
                    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                    .MinimumLevel.Override("System", LogEventLevel.Warning)
                    .Enrich.FromLogContext()
                    .WriteTo.Console(outputTemplate: "[{Level}] {Message:l}{NewLine}{Exception:l}")
                    .WriteTo.Elasticsearch(
                        new ElasticsearchSinkOptions(
                            new Uri(conf.ElasticSearchUri))
                        {
                            InlineFields = true,
                            MinimumLogEventLevel = LogEventLevel.Verbose,
                            AutoRegisterTemplate = true,
                            IndexFormat = "ws-{0:yyyy.MM}"
                        })
                    .CreateLogger());

            For<ILoggerFactory>().Use(loggerFactory).Singleton();
            For(typeof(ILogger<>)).Use(typeof(Logger<>)).ContainerScoped();
            
            For<Data.Context>().Use<Data.Context>()
                            .Ctor<string>().Is(conf.ConnectionString)
                            .ContainerScoped();
                        For<Data.IUsersRepository>().Use<Data.UsersRepository>().Transient();
        }
    }
}