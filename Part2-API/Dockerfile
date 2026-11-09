FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY Part2-API/src/RaceDay.Api/RaceDay.Api.csproj Part2-API/src/RaceDay.Api/
RUN dotnet restore Part2-API/src/RaceDay.Api/RaceDay.Api.csproj
COPY Part2-API/ Part2-API/
WORKDIR /src/Part2-API/src/RaceDay.Api
RUN dotnet publish RaceDay.Api.csproj -c Release -o /app/publish --no-restore
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080 ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
COPY --from=build /app/publish .
USER $APP_UID
ENTRYPOINT ["dotnet", "RaceDay.Api.dll"]
