# Task Two. Serilog Configuration

## AspNet Core 

We are going to use Microsoft.Extensions.Logging infrastructure as an abstraction layer on top of Serilog. That's the infrastructure being used accross all moders frameworks (AspNet Core, EF Core, etc.) allows you to not being coupled with 3rd party logger's interfaces and similar endpoint and configuration accross your project.

It basically exposes few interfaces to work with: `ILogger<T>`, `ILoggerFactory` (has `CreateLogger()` method), `ILoggerProvider`. Within the app we usually use two of them, while `ILoggerProvider` usually implemented by 3rd party loggers (aka Serilog, NLog, etc.).

To make it very easy to setup, all 3rd party loggers provide extension methods to attach `ILoggerProvider` to you `ILoggerFactory`, i.e `loggerFactory.AddSerilog()`. Such a fluent api is a cornerstone of whole .NET Core platform. Easy. So, lets define `LoggerFactory`, add the following to the Registry: 

```
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
```
We've just defined the factory with Serilog log provider within. It worth to note for now we use Console Sink - just for simplicity. In order to resolve not factory, but some closed generic `ILogger<T>` (lets say `ILogger<UsersController>`) we would need to register open generic interface too
```
For(typeof(ILogger<>)).Use(typeof(Logger<>)).ContainerScoped();
```
The `<T>` parameter is a source context, usually it is a type (class) that use the logger instance. It worth to note we use `Logger<>` as an implementation here. It make sense because of his constructor which accepts `ILoggerFactory` we've registered earlier
```
// Decompiled with JetBrains decompiler
// Type: Microsoft.Extensions.Logging.Logger`1
// Assembly: Microsoft.Extensions.Logging.Abstractions, Version=2.0.1.0, Culture=neutral, PublicKeyToken=adb9793829ddae60
public Logger(ILoggerFactory factory)
{
    if (factory == null)
    throw new ArgumentNullException(nameof (factory));
    this._logger = factory.CreateLogger(TypeNameHelper.GetTypeDisplayName(typeof (T)));
}
```
Thus, when we resolve `ILogger<Foo>` new `Logger<Foo>` going to be created using our (Serilog) implementation of `ILoggerFactory.CreateLogger()`. 

After we registered loggers in a container we can inject concrete `ILogger<T>` into Controller (and anywhere else), lets modify our `UsersController` and add some loggs

```
private readonly IUsersRepository _repository;
private readonly ILogger<UsersController> _logger;

public UsersController(IUsersRepository repository, ILogger<UsersController> logger)
{
    _repository = repository;
    _logger = logger;
}

[HttpPost]
public async Task<IActionResult> Create([FromBody]CreateUserRequest request)
{
    var user = new Data.User
    {
        Email = request.Email,
        FirstName = request.FirstName,
        LastName = request.LastName,
        Id = Guid.NewGuid()
    };
    await _repository.Add(user);
    _logger.LogInformation("{@user} has been created", user);
    
    return CreatedAtAction(nameof(Get), new {id = user.Id}, user);
}
```
Here we added Serilog-formatted message. `{@user}` (`@` means object destructuring) going to be interpolated with serialized `User` object along with all the properties within. Console Sink just outputs such an objects as a JSON, but some sinks (Elasticsearch, Seq) targeted data storages that supports document data will produce JSON document.

## EF Core

As was outlined earlies, modern frameworks works with `ILoggerFactory` interface when it comes to logging. EF Core is example of such a framework. It is very easy to enable SQL queries tracing - we just need to configure Context to work with our `ILoggerFactory`. In order to more accurately configure what to log (we only need console sink here, no need to have EF Core logs in Elasticsearch or another log storage), i would recommend to use dedicated `ILoggerFactory` instances for AspNet Core and EF Core. Lets create & register a new one.
```
var efLoggerFactory = new LoggerFactory()
    .AddSerilog(new LoggerConfiguration()
        .MinimumLevel.Information()
        .Enrich.FromLogContext()
        .WriteTo.Console(outputTemplate: "[{Level}] {Message:l}{NewLine}{Exception:l}")
        .CreateLogger());

For<Data.Context>().Use<Data.Context>()
                .Ctor<string>().Is(conf.ConnectionString)
                .Ctor<ILoggerFactory>().Is(efLoggerFactory)
                .ContainerScoped();
```
Lets go to the Context now and pass the factory to it
```
private readonly string _connectionString;
private readonly ILoggerFactory _loggerFactory;

public Context(string connectionString, ILoggerFactory loggerFactory)
{
    _connectionString = connectionString;
    _loggerFactory = loggerFactory;
}

protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
{
    optionsBuilder
        .UseLoggerFactory(_loggerFactory)
        .ConfigureWarnings(warnings =>
        {
            warnings.Log(RelationalEventId.QueryClientEvaluationWarning);
            warnings.Log(RelationalEventId.QueryPossibleExceptionWithAggregateOperator);
            warnings.Log(RelationalEventId.QueryPossibleUnintendedUseOfEqualsWarning);
            warnings.Ignore(CoreEventId.IncludeIgnoredWarning);
            warnings.Ignore(CoreEventId.ContextInitialized);
        })
        .UseNpgsql(_connectionString);
}
```
We also configured what to ignore and what to output, there a lot of event types supported but i found these the most important.
Dont forget to update `DesignTimeContextFactory` used for data migrations, since we changed Context's constructur signature.
```
return new Context("Host=localhost;Username=postgres;Password=12345;Database=Users", null);
```
That's it. Now we can run the app and test logging infrastructure
```
$ dotnet run -p Workshop.Api.csproj
$ curl -X POST "http://localhost:5000/api/users" -H "accept: application/json" -H "Content-Type: application/json-patch+json" -d "{ \"email\": \"Dzmitry.martavoi@oxagile.com\", \"firstName\": \"dzmitry\", \"lastName\": \"martavoi\"}"
HTTP/1.1 201 Created
Date: Mon, 23 Apr 2018 23:19:29 GMT
Content-Type: application/json; charset=utf-8
Server: Kestrel
Transfer-Encoding: chunked
Location: http://localhost:5000/api/users/46043be2-29a1-447d-aecc-125b90a4a653
X-Correlation-Id: 8169f3ed-e6f4-448c-8048-39fd475d6e24

{
  "id": "46043be2-29a1-447d-aecc-125b90a4a653",
  "email": "Dzmitry.martavoi@oxagile.com",
  "firstName": "dzmitry",
  "lastName": "martavoi"
}

[Information] User {Id=11331e18-c335-48fd-b5ae-311a2d252fd2, Email="Dzmitry.martavoi@oxagile.com", FirstName="dzmitry", LastName="martavoi"} has been created
```

An additional thing we could try to play with is `LogContext`. That's special static context allows you to push any properties/values to and (depending on sink you use) they going to be added to every message entry. Typically, you want to use `LogContext` for properties like CorrelationId, RequestId, SessionId, ProcessName, Thread, Environment, etc. You can configure `LogContext` at middleware lvl and dont care about these properties when you yet another time write to log. Cool.

But...we use Microsoft.Extensions.Logging abstraction layer and `LogContext` is defined in Serilog nuget pckg...Luckily, abstraction layer we use has `Scope` object that can (and must!) be used for this purpose via `ILogger<T>.CreateScope()`. Lets try to add X-Correlation-Id HTTP header to our request and add the Id itself to log scope (`LogContext`):
```
public static class CorrelationMiddleware
{
    public static void UseCorrelationHeader(this IApplicationBuilder builder, ILoggerFactory loggerFactory)
    {
        builder.Use(async (context, next) =>
        {
            const string xCorrelationId = "X-Correlation-Id";
            string correlationId;
            if (context.Request.Headers.ContainsKey(xCorrelationId))
            {
                correlationId = context.Request.Headers[xCorrelationId];
            }
            else
            {
                correlationId = Guid.NewGuid().ToString();
                context.Request.Headers.Append(xCorrelationId, correlationId);
            }
            
            context.Response.Headers.Append(xCorrelationId, correlationId);
            var logger = loggerFactory.CreateLogger("CorrelationMiddleware");

            //here we just specify a format string with a list of properties we want to push to context
            using (logger.BeginScope("{CorrelationId}", correlationId))
            {
                await next();
            }
        });
    }
}
```
Modify `Startup`'s `Configure` method with:
```
app.UseCorrelationHeader(loggerFactory);
```
If you re-run the app. and try to POST a user X-Correlation-Id header will be returned in a response. Log message will remain the same since Console sink doesn't work with properties from context
```
$ curl -X POST "http://localhost:5000/api/users" -H "accept: application/json" -H "Content-Type: application/json-patch+json" -d "{ \"email\": \"Dzmitry.martavoi12@gmail.com\", \"firstName\": \"dzmitry\", \"lastName\": \"martavoi\"}"
HTTP/1.1 201 Created
Date: Mon, 23 Apr 2018 23:19:29 GMT
Content-Type: application/json; charset=utf-8
Server: Kestrel
Transfer-Encoding: chunked
Location: http://localhost:5000/api/users/46043be2-29a1-447d-aecc-125b90a4a653
X-Correlation-Id: 25d2b605-2f2c-4344-a8ea-82d82125cfa9

{
  "id": "46043be2-29a1-447d-aecc-125b90a4a653",
  "email": "Dzmitry12martavoi@gmail.com",
  "firstName": "dzmitry",
  "lastName": "martavoi"
}
```
In order to see `LogContext` properties, you can configure Elasticsearch sink. See you in task.three