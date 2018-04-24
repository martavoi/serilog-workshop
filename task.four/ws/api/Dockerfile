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