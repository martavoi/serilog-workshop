using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;

namespace Workshop.Api
{
    public class Registry: StructureMap.Registry
    {
        public Registry(Config conf)
        {   
            var loggerFactory = new LoggerFactory()
                .AddSerilog(new LoggerConfiguration()
                    .MinimumLevel.Debug() 
                    // some frameworks (AspNet Core, EF Core) do log a lot of details, so that's the way to disable it
                    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                    .Enrich.FromLogContext()
                    // Sink configuration
                    .WriteTo.Console(outputTemplate: "[{Level}] {Message:l}{NewLine}{Exception:l}")
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