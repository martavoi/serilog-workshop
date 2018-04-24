# Task Three. Elasticsearch & Kinaba configuration

First things first. Here we are going to add  some 'moving parts' (elasticsearch instance and kibana) to our system. 
It is not really handy to configure and manage these services by hands (later it might be needed to add some reverse proxy in front of them, setup multi-node cluster etc.).

Instead, we are going to build few Docker images and Compose file to start a whole cluster with a single bash command. So, lets pack all the services into custom Docker image.

## AspNet Core 

Our Api is the fist one we would like to run using Docker. The Image is pretty simple, just put `Dockerfile` with the following instructions at `api` folder:
```
FROM microsoft/aspnetcore-build:2.0 AS build-env
WORKDIR /app

COPY . ./
WORKDIR /app/Workshop.Api
RUN dotnet publish -c Release -o build

FROM microsoft/aspnetcore:2.0

WORKDIR /app
COPY --from=build-env /app/Workshop.Api/build .

EXPOSE 80

ENTRYPOINT ["dotnet", "Workshop.Api.dll"]
```

Thats it. The Dockerfile going to create temp container with .NET Core SDK -> make a publish -> crete .NET Core runtime container -> copy build files and run the app.

## Elasticsearch

Lets create a folder for elasticsearch service:
```
$ mkdir ws/elasticsearch
```
After that we would need to create Dockerfile and some basic elasticsearch config file. Put the following to `ws/elasticsarch/elasticsearch.yml`
```
---
## Default Elasticsearch configuration from elasticsearch-docker.
## from https://github.com/elastic/elasticsearch-docker/blob/master/build/elasticsearch/elasticsearch.yml
#
cluster.name: "workshop.elasticsearch"
network.host: 0.0.0.0

# minimum_master_nodes need to be explicitly set when bound on a public IP
# set to 1 to allow single node clusters
# Details: https://github.com/elastic/elasticsearch/pull/17288
discovery.zen.minimum_master_nodes: 1

## Use single node discovery in order to disable production mode and avoid bootstrap checks
## see https://www.elastic.co/guide/en/elasticsearch/reference/current/bootstrap-checks.html
#
discovery.type: single-node

## Disable X-Pack
## see https://www.elastic.co/guide/en/x-pack/current/xpack-settings.html
##     https://www.elastic.co/guide/en/x-pack/current/installing-xpack.html#xpack-enabling
#
xpack.security.enabled: false
xpack.monitoring.enabled: false
xpack.ml.enabled: false
xpack.watcher.enabled: false
```
Nothing special here. We just disabled X-Pack module (it is not free) and setup single-node mode.
Dockerfile:
```
FROM docker.elastic.co/elasticsearch/elasticsearch:6.2.4

ENV ES_JAVA_OPTS="-Xmx256m -Xms256m"

ADD elasticsearch.yml /usr/share/elasticsearch/config/
USER root
RUN chown elasticsearch:elasticsearch config/elasticsearch.yml
USER elasticsearch
```
Here we just replaced default elastic configuration with our own, and configured an appropriate ownership on this file.

## Kibana

Lets do the same for Kibana. Just add `kibana.yml` and `Dockerfile` to `ws/kibana` folder with the following:
```
---
## Default Kibana configuration from kibana-docker.
## from https://github.com/elastic/kibana-docker/blob/master/build/kibana/config/kibana.yml
#
server.name: workshop.kibana
server.host: "0"
## will be approprietly by Docker internal DNS
elasticsearch.url: http://workshop.elasticsearch:9200

## Disable X-Pack
## see https://www.elastic.co/guide/en/x-pack/current/xpack-settings.html
##     https://www.elastic.co/guide/en/x-pack/current/installing-xpack.html#xpack-enabling
#
xpack.security.enabled: false
xpack.monitoring.enabled: false
xpack.ml.enabled: false
xpack.graph.enabled: false
xpack.reporting.enabled: false
xpack.grokdebugger.enabled: false
```

Dockerfile
```
FROM docker.elastic.co/kibana/kibana:6.2.4

COPY kibana.yml /usr/share/kibana/config/kibana.yml
```

## Docker Compose

Now, its time integrate all the parts. Lets create `ws/docker-compose.yml` file with the lines below
```
version: '3'
services:
  workshop.api:
    build: ./api
    ports:
     - 80:80
    depends_on:
     - workshop.postgres
     - workshop.elasticsearch
  workshop.postgres:
    image: postgres:alpine
    volumes:
     - workshop.postgres.data:/var/lib/postgresql/data
    environment:
      - POSTGRES_PASSWORD=12345
  workshop.elasticsearch:
    build: ./elasticsearch
    volumes:
      - workshop.elasticsearch.data:/usr/share/elasticsearch/data
    ulimits:
      memlock:
        soft: -1
        hard: -1
      nofile:
        soft: 65536
        hard: 65536
  workshop.kibana:
    build: ./kibana
    depends_on:
     - workshop.elasticsearch
    ports:
      - 5601:5601

volumes:
  workshop.postgres.data:
  workshop.elasticsearch.data:
```

## App configuration
We almost ready to launch the cluster. We just need to configure elasticsearch sink for Serilog logger and change our `appsettings.json` configuration to reflect proper PostgreSQL connection string:
```
{
  "ConnectionString": "Host=workshop.postgres;Username=postgres;Password=12345;Database=Users",
  "ElasticSearchUri": "http://workshop.elasticsearch:9200"
}
```
Add `Elastisearch` property to `Config.cs`:
```
public class Config
{
    public Config(IConfiguration conf)
    {
        conf.Bind(this);
    }
    
    public string ConnectionString { get; set; }
    public string ElasticSearchUri { get; set; }
}
```
Add Elasticsearch Sink nuget pckg to the Api:
```
$ dotnet add ws/api/Workshop.Api/Workshop.Api.csproj package Serilog.Sinks.Elasticsearch
```

... loggerFactory configuration:
```
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
```

## Lauch!

That's it. We are ready to launch:
```
$ cd ws
$ docker-compose up --build
```
Try to POST a new User to check cluster works fine. If it is, lets go to Kibana endpoint (it might take few minutes to initialize Kibana, keep calm :) exposed at `http:\\localhost:5601`. For the first time we need to configure Kibana to track a proper Index by specifying Index pattern. In a Serilog configuration we've specified `ws-{0:yyyy.MM}`, so type `ws-*` in an appropriate Kibana setting and click Next. Choose time filter field `@timestamp` and click 'Create index pattern' button. Here you can see a table with all the fields accessible in the Index, no need to change it. Go to Discover module to see your logs!