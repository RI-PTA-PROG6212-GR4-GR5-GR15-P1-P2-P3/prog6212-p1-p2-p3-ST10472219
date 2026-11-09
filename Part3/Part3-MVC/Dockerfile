# Build and publish in the SDK image; run only the published output in the runtime image.
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY Part3/Part3-MVC/RaceDay.Mvc.csproj Part3/Part3-MVC/
RUN dotnet restore Part3/Part3-MVC/RaceDay.Mvc.csproj
COPY Part3/Part3-MVC/ Part3/Part3-MVC/
WORKDIR /src/Part3/Part3-MVC
RUN dotnet publish RaceDay.Mvc.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080 ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
COPY --from=build /app/publish .
USER $APP_UID
ENTRYPOINT ["dotnet", "RaceDay.Mvc.dll"]
