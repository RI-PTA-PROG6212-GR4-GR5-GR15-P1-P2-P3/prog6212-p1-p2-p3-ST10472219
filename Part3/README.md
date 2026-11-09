# RaceDay – Part 3 (ASP.NET Core MVC front end)

PROG6212 POE Part 3. An MVC web app that consumes the Part 2 RESTful API. Organisers manage events, categories,
enrolments and results; Participants browse, enrol and view their race history.


- **Green CI build:** 
` 

## Architecture

```
Browser ──► MVC (Part3-MVC, :8080) ──HTTP + session cookie──► API (Part2-API, :8081) ──► SQL Server
                                                                    └──────────────► Azure Blob Storage (Azurite locally)
```

- The MVC app never touches the database or Azure. All data and images go through the API.
- Login calls `POST /api/auth/login`. The MVC app keeps its own session (user id, role, name) and forwards the API's
  `.RaceDay.Session` cookie on every call. Pages are protected with `[RoleAuthorize("Organiser" | "Participant")]`.
- Images are uploaded to the API (`POST /api/events/{id}/banner`, `POST /api/profile/picture`), which stores them in
  Azure Blob Storage. The MVC app streams them back through `/Media/EventBanner/{id}` and `/Media/ProfilePicture`.

## Features

| Role | Pages |
| --- | --- |
| Public | Home (upcoming events, filter by type/search), Login, Register |
| Organiser | Dashboard, My events (create / edit / delete, banner upload), Categories (distance **and** age), Enrolments, Results entry and **Publish** |
| Participant | Browse + filter, Event details with categories, Enrol / cancel, My events + status, **My Results** ("47th out of 312 finishers"), Profile + picture |

Results are private until the Organiser publishes them (only after the event date, and only when every active enrolment
has a result).

## Run with Docker (recommended)

1. `cp .env.example .env` and set a strong `MSSQL_SA_PASSWORD`.
2. `docker compose up --build` (run from this `Part3` folder).
3. Open http://localhost:8080 (MVC). API Swagger: http://localhost:8081/swagger.

The API applies EF Core migrations and seeds demo data on start. Demo accounts (password `Password123!`):

| Role | Email |
| --- | --- |
| Organiser | thabo.mokoena@raceday.co.za |
| Participant | sipho.dlamini@gmail.com |

The MVC image is multi-stage (SDK build → ASP.NET runtime), runs as a non-root user and listens on port 8080.

To store images in a real Azure account instead of Azurite, put the account's connection string in `.env` as
`BLOB_CONNECTION_STRING=...` before running compose. Leave it empty to use Azurite.

## Docker run command (MVC image on its own)

From the repository root, with the API already running on port 8081 (for example from compose):

```
docker build -f Part3/Part3-MVC/Dockerfile -t raceday-mvc .
docker run --rm -p 8080:8080 -e Api__BaseUrl=http://host.docker.internal:8081/ raceday-mvc
```

## Run locally without Docker

Double-click `run-local.bat` (API on :5080, MVC on :5000, opens the browser), or run the two commands below.

```
dotnet run --project ../Part2-API/src/RaceDay.Api --urls http://localhost:5080
Api__BaseUrl=http://localhost:5080/ dotnet run --project Part3-MVC --urls http://localhost:5000
```

No SQL Server on the machine? Add `Database__UseInMemory=true` in front of the API command. The data then lives in
memory and is lost when the API stops.

For image uploads start Azurite (`docker run -p 10000:10000 mcr.microsoft.com/azure-storage/azurite azurite-blob --blobHost 0.0.0.0`).

## Azure Blob Storage configuration (API only)

`BlobStorage:ConnectionString` in the API configuration. Defaults to `UseDevelopmentStorage=true` (Azurite). For a real
account set it with an environment variable or user secrets, never in the repo:

```
BlobStorage__ConnectionString="DefaultEndpointsProtocol=https;AccountName=...;AccountKey=...;EndpointSuffix=core.windows.net"
```

## Tests and CI

`dotnet test RaceDay-Full.sln` (inside your repo) runs the API tests and the MVC tests; `RaceDay-Part3.sln` opens just the MVC app and its tests (view-model validation, role filter, API client
cookie/error handling, "47th out of 312" formatting). GitHub Actions (`.github/workflows/ci.yml`) builds, runs all
tests and builds both Docker images on every push. It then runs the app the same way as above
(`docker compose up --build` with SQL Server, Azurite, the API and the MVC app) and checks that the API answers, the
MVC home page loads and a picture can be uploaded to blob storage and read back.

## Repository layout

```
Part2-API/            RESTful API from Part 2 (+ Part 3 additions: blob storage, publish results, dashboard)
Part3/
  Part3-MVC/          ASP.NET Core MVC app (+ Dockerfile)
  Part3-MVC.Tests/    xUnit tests
  docker-compose.yml  SQL Server + Azurite + API + MVC
  RaceDay-Part3.sln     MVC + tests only (opens standalone)
  RaceDay-Full.sln      API + MVC + all tests (needs Part2-API next to Part3)
```
