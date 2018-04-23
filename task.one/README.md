# Task One. AspNet Core Api Bootstrap

## First things first. Prepare folders structure
```
$ mkdir ws
$ mkdir ws/api
```
after that we would need ASP.NET Core MVC app (api) and EF Core data layer 
```
$ dotnet new webapi -n Workshop.Api -o ws/api/Workshop.Api
$ dotnet new lib -n Workshop.Data -o ws/api/Workshop.Data
$ dotnet new sln -n Workshop -o ws/api
$ dotnet sln ws/api/Workshop.sln add ws/api/**/*.csproj
$ cd ws/api/Workshop.Api/
$ dotnet add reference ../Workshop.Data/Workshop.Data.csproj
```
...here we go
```
$ tree ws
ws
└── api
    ├── Workshop.Api
    │   ├── Controllers
    │   │   └── ValuesController.cs
    │   ├── Program.cs
    │   ├── Startup.cs
    │   ├── Workshop.Api.csproj
    │   ├── appsettings.Development.json
    │   ├── appsettings.json
    │   ├── obj
    │   │   ├── Workshop.Api.csproj.nuget.cache
    │   │   ├── Workshop.Api.csproj.nuget.g.props
    │   │   ├── Workshop.Api.csproj.nuget.g.targets
    │   │   └── project.assets.json
    │   └── wwwroot
    ├── Workshop.Data
    │   ├── Class1.cs
    │   ├── Workshop.Data.csproj
    │   └── obj
    │       ├── Workshop.Data.csproj.nuget.cache
    │       ├── Workshop.Data.csproj.nuget.g.props
    │       ├── Workshop.Data.csproj.nuget.g.targets
    │       └── project.assets.json
    └── Workshop.sln

7 directories, 17 files
```

## Add Nuget packages
```
$ dotnet add ws/api/Workshop.Api/Workshop.Api.csproj package Serilog.Extensions.Logging
$ dotnet add ws/api/Workshop.Api/Workshop.Api.csproj package Serilog.Sinks.Console
$ dotnet add ws/api/Workshop.Api/Workshop.Api.csproj package Swashbuckle.AspNetCore
$ dotnet add ws/api/Workshop.Api/Workshop.Api.csproj package StructureMap.Microsoft.DependencyInjection

$ dotnet add ws/api/Workshop.Data/Workshop.Data.csproj package Microsoft.Extensions.Logging
$ dotnet add ws/api/Workshop.Data/Workshop.Data.csproj package Npgsql.EntityFrameworkCore.PostgreSQL
```

## Configure Api startup

Default WebHost configuration `WebHost.CreateDefaultBuilder()` do a lot behind the scene (configure IIS integration, configure default logging middleware, etc) and is not really needed. Instead, we going to configure only those parts we are intrested in:

```
public static IWebHost BuildWebHost(string[] args) =>
            new WebHostBuilder()
                .UseKestrel()
                .UseContentRoot(Directory.GetCurrentDirectory())
                .ConfigureAppConfiguration((context, builder) =>
                {
                    builder.SetBasePath(context.HostingEnvironment.ContentRootPath)
                        .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                        .AddJsonFile($"appsettings.{context.HostingEnvironment.EnvironmentName}.json", optional: true);
                })
                .UseStartup<Startup>()
                .Build();
```

add Mvc along with Swagger configuration to `ConfigureServices` method
```
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
```
...and `Configure` method
```
public void Configure(IApplicationBuilder app, IServiceProvider serviceProvider, ILoggerFactory loggerFactory)
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Workshop.API V1");
    });

    app.UseMvc();
}
```

We would also need some configuration to pass connection string to DB (and elasticsearch later). For this purpose an easiest way is to use .NET Core Confuration Framework.
```
public class Config
{
    public Config(IConfiguration conf)
    {
        conf.Bind(this);
    }
    
    public string ConnectionString { get; set; }
}
```

Lets prepare DI container (it is up to you which one to choose, StructureMap being used below) and dependencies resitry
```
public class Registry: StructureMap.Registry
{
    public Registry(Config conf)
    {   
        // empty for now
    }
}
```
...and register a container with ASP.NET Core framework; add the following at the very end of `ConfigureServices` method (make it return `IServiceProvider`)
```
var container = new Container();
container.Configure(config =>
{
    config.AddRegistry(new Registry(new Config(Configuration)));
    config.Populate(services);
});

return container.GetInstance<IServiceProvider>();
```

Here we are... we've just bootstrapped ASP.NET Core App. In order to do something meaningfull we would need to setup EF Core and add some controllers.

## EF Core configuration

Lets create a `User` , `UserConfiguration` and setup `DbContext` to manage it
```
public class User
{
    public Guid Id { get; set; }
    public string Email { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
}

public class UserConfiguration: IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasKey(user => user.Id);
        builder.Property(user => user.Email).HasMaxLength(255);
        builder.HasIndex(user => user.Email).IsUnique();
        builder.Property(user => user.FirstName).HasMaxLength(96);
        builder.Property(user => user.LastName).HasMaxLength(96);
    }
}

public class Context: DbContext
{
    private readonly string _connectionString;

    public Context(string connectionString)
    {
        _connectionString = connectionString;
    }
    
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder
            //it is up to you what DB to use, but i would recommend PostgreSQL, just coz OS and free
            .UseNpgsql(_connectionString);
    }

    public DbSet<User> Users { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfiguration(new UserConfiguration());
    }
}
```
An interface to work with the Context will be `IUsersRepository`
<details><summary>UsersRepository</summary>

```
public interface IUsersRepository
{
    Task<User> Get(Guid id);
    Task<User[]> Get();
    Task Add(User u);
}

public class UsersRepository : IUsersRepository
{
    private readonly Context _ctx;

    public UsersRepository(Context ctx)
    {
        _ctx = ctx;
    }
    
    public Task<User> Get(Guid id)
    {
        return _ctx.Users.FindAsync(id);
    }

    public Task<User[]> Get()
    {
        return _ctx.Users.ToArrayAsync();
    }

    public Task Add(User u)
    {
        _ctx.Users.Add(u);
        return _ctx.SaveChangesAsync();
    }
}
```
</details>

In order to add migration we would need an appropriate context factory (will be used by EF Core Tools to create and apply Code First Migrations).
There is connection string to local PostgreSql installation (see instructions below how to run it if you dont have one)
```
public class DesignTimeContextFactory: IDesignTimeDbContextFactory<Context>
{
    public Context CreateDbContext(string[] args)
    {
#if DEBUG
        return new Context("Host=localhost;Username=postgres;Password=12345;Database=Users"); 
#else
        throw new System.InvalidOperationException();
#endif
    }
}
```
The last thing we need to add is EF Core Tool package, it's needed to be done manually via edititng *.csproj, just add the following:
```
<ItemGroup>
    <DotNetCliToolReference Include="Microsoft.EntityFrameworkCore.Tools.DotNet" Version="2.0.0" />
</ItemGroup>
```

If you dont have PostgreSQL locally, its time to lauch Docker container with it (omit `-d` option to run in foreground):
```
$ docker run --name postgres -e POSTGRES_PASSWORD=12345 -p 5432:5432 -d postgres:alpine
```

Now, we are ready to create EF Core migration, lets do it
```
$ cd ../Workshop.Data/
$ dotnet ef migrations add -s ../Workshop.Api/Workshop.Api.csproj -c Context -v Add_User
```
If the migration creating succeded you will see newly create file in Migrations folder

Add DI container configuration to the Registry
```
For<Data.Context>().Use<Data.Context>()
    .Ctor<string>().Is(conf.ConnectionString)
    .ContainerScoped();
For<Data.IUsersRepository>().Use<Data.UsersRepository>().Transient();
```

Mofidy `Program.Main` method to run migrations at app. startup
```
public static void Main(string[] args)
{
    var host = BuildWebHost(args);
    using (var scope = host.Services.CreateScope())
    {
        var services = scope.ServiceProvider;
        var context = services.GetService<Data.Context>();
        context.Database.Migrate();
    }
    host.Run();
}
```

The last step is to add Controller and few Models
<details><summary>UsersController</summary>

```
public class User
{
    public Guid Id { get; set; }
    public string Email { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
}

public class CreateUserRequest
{
    public string Email { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
}

public class GetUsersResponse
{
    public User[] Users { get; set; }
}

[Route("api/users")]
public class UsersController : Controller
{
    private readonly IUsersRepository _repository;

    public UsersController(IUsersRepository repository)
    {
        _repository = repository;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var users = await _repository.Get();
        return Ok(new GetUsersResponse
        {
            Users = users.Select(u => new Models.User
            {
                Id = u.Id,
                Email = u.Email,
                FirstName = u.FirstName,
                LastName = u.LastName
    
            }).ToArray()});
    }

    [HttpGet("{id:Guid}")]
    public async Task<IActionResult> Get(Guid id)
    {
        var user = await _repository.Get(id);
        return Ok(new User
        {
            Id = user.Id,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName
        });
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateUserRequest request)
    {
        var user = new User
        {
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName,
            Id = Guid.NewGuid()
        };
        await _repository.Add(user);
    
        return CreatedAtAction(nameof(Get), new {id = user.Id}, user);
    }
}
```
</details>

Before the first run lets configure connection string, go to `appsettings.json` and add
```
"ConnectionString": "Host=localhost;Username=postgres;Password=12345;Database=Users"
```

Run the app
```
$ cd ../Workshop.Api/
$ dotnet run -p Workshop.Api.csproj
$ curl -i -X GET http://localhost:5000/api/users # or via browser :)
```
If migration was fine - well done. Get ready for logger configuration.   