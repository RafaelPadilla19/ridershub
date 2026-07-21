# RidersHub — imagen para Cloud Run.
# Build:  docker build -t ridershub .
# Sin secretos horneados: ConnectionStrings__Riders, Jwt__SigningKey, Services__*, Payments__CallbackSecret, ApiKeys__* llegan por env vars.

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY RidersHub.csproj .
RUN dotnet restore RidersHub.csproj
COPY . .
RUN dotnet publish RidersHub.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080
ENTRYPOINT ["dotnet", "RidersHub.dll"]
