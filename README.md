# RaceDay — Event Management API

## 1. System Overview

RaceDay is a sports event management system designed for the South African road running community. It supports running, walking, and cycling events.

The system allows Organisers to manage events and related information, while Participants can browse events, enrol in categories, and track their performance history.

This repository covers system planning, database design, SQL implementation, and API development.

## 2. System Roles

- **Organiser** — can create, update, and delete events, manage event categories, record participant results, and view event enrolments, subject to authorization rules.
- **Participant** — can register an account, browse events, enrol in categories, view their enrolments, and access their personal results, subject to authorization rules.

## 3. Technologies Used

- C# and ASP.NET Core Web API
- .NET 8
- Microsoft SQL Server
- Entity Framework Core
- JWT (JSON Web Token) authentication
- Swagger / OpenAPI
- xUnit for unit testing
- GitHub Actions for continuous integration

## 4. Database Design

The database supports users, events, categories, enrolments, results, and related event information.

The `docs` folder contains the planning and database resources from Part 1:

- `RaceDay_ERD.png` — Entity Relationship Diagram.
- `RaceDay_API_Endpoints.md` — planned API endpoints.
- `RaceDay_Database.sql` — SQL database creation and sample data script.

### Running the SQL script

1. Open SQL Server Management Studio (SSMS).
2. Connect to the appropriate SQL Server instance.
3. Open `docs/RaceDay_Database.sql`.
4. Execute the script on a suitable database instance.

## 5. Authentication and Authorization

The API provides user registration and login through the authentication endpoints.

- Passwords are hashed before being stored.
- Successful login returns a JWT access token.
- Protected endpoints require a valid token.
- Role-based authorization restricts access to permitted actions.
- The API distinguishes between unauthenticated requests and requests made by users without the required permissions.

To test protected endpoints in Swagger, log in, copy the returned token, and use the **Authorize** option to provide it.

## 6. Running the API Locally

### Prerequisites

- .NET 8 SDK
- Microsoft SQL Server
- SQL Server Management Studio (SSMS), or another suitable SQL client

### Setup

1. Clone or download this repository.
2. Open the solution or the `RaceDayApi` project in your development environment.
3. Configure the database connection string in `appsettings.json` for your local SQL Server instance.
4. Configure the JWT settings required by the application. Use a secure secret and do not commit production credentials.
5. Restore dependencies and build the project.
6. Apply the Entity Framework Core migrations if required by the current database setup.
7. Run the API.

From the API project directory, use:

```bash
dotnet restore
dotnet build
dotnet run
```

Open the Swagger URL displayed in the application output to explore and test the API endpoints.

## 7. Running Unit Tests

The project includes an xUnit test project, `RaceDayApi.Tests`.

From the repository root, run the test command using the actual test project path:

```bash
dotnet test RaceDayApi.Tests/RaceDayApi.Tests.csproj
```

The test project currently includes a password hashing and verification test.

Additional assessment scenarios should cover registration, login, authorization restrictions, event management, and enrolment functionality.

## 8. CI/CD — GitHub Actions

GitHub Actions automatically runs the configured workflow when changes are pushed or a pull request is opened.

The workflow restores dependencies, builds the test project, and runs the automated tests.

**Successful CI build screenshot**

Add a screenshot from the successful GitHub Actions run here.

## 9. API Documentation

The API endpoints can be explored and tested through Swagger when the application is running.

The planned endpoint documentation from Part 1 is available in `docs/RaceDay_API_Endpoints.md`.

## 10. Video Presentation

**Unlisted YouTube demonstration:** Add the actual video URL before submission.

The demonstration should show:

- The project structure and main implementation.
- Registration and login with JWT authentication.
- Protected endpoints and role-based authorization.
- Event and enrolment functionality.
- The automated tests and successful GitHub Actions run.

## 11. Repository

GitHub repository: https://github.com/ST10437150/Assignment2_Prog6212

---

# Part 3 — RaceDay Web Application (ASP.NET Core MVC)

Part 3 adds the web front end, Azure Blob Storage image uploads (through the API) and Docker.

## 12. Architecture

```
Browser  ──►  RaceDayWeb (MVC, session + Bootstrap)  ──HTTP + JWT──►  RaceDayApi  ──►  SQL Server
                                                                          └──────────►  Azure Blob Storage
```

- **RaceDayWeb** has no database access and no business logic. Every read and write is an HTTP call to the API (`Services/ApiClient.cs`).
- The JWT returned by `/api/auth/login` is kept **server-side in the session** together with the user id, name and role.
- Role-based access is enforced twice: `[SessionAuthorize(Role = ...)]` in the MVC app (redirects to login / "access denied") and `[Authorize(Roles = ...)]` in the API (401 / 403, plus ownership checks).
- **Azure Blob Storage** lives only in the API (`Services/BlobStorageService.cs`). The MVC app forwards uploaded files to the API; it never talks to Azure.

## 13. New API endpoints (added for Part 3)

| Method | Route | Role | Purpose |
|---|---|---|---|
| GET | /api/events?search=&type=&from=&to= | public | Upcoming events with filters |
| GET | /api/events/mine | Organiser | The organiser's own events |
| POST / PUT / DELETE | /api/events, /api/events/{id} | Organiser (owner) | Manage events |
| POST | /api/events/{id}/banner | Organiser (owner) | Upload event banner → Blob Storage |
| GET / POST | /api/events/{eventId}/categories | public / Organiser | List / add categories |
| PUT / DELETE | /api/categories/{id} | Organiser (owner) | Edit / remove category |
| POST | /api/categories/{id}/enrol | Participant | Enrol (starts as **Pending**) |
| GET | /api/users/{id}/enrolments | Participant | My enrolments |
| GET | /api/events/{eventId}/enrolments | Organiser (owner) | Enrolments for an event |
| DELETE | /api/enrolments/{id} | Participant | Cancel my enrolment |
| PUT | /api/enrolments/{id}/status | Organiser (owner) | Confirm / cancel an enrolment |
| POST | /api/enrolments/{id}/results | Organiser (owner) | Capture finish time + position |
| GET | /api/users/{id}/results | Participant | My results (with total finishers) |
| GET | /api/events/{eventId}/results | Organiser (owner) | All results of an event |
| POST | /api/users/{id}/picture | any user | Upload profile picture → Blob Storage |

## 14. Running Part 3 locally

### 14.1 Database migration (new columns + "Pending" status)

From `RaceDayApi/RaceDayApi`:

```bash
dotnet ef migrations add Part3_BlobImagesAndPendingStatus
dotnet ef database update
```

### 14.2 Image storage

The API reads `AzureBlob:ConnectionString` and `AzureBlob:Container` from configuration.

**Local development (Azurite emulator)** — the default in `appsettings.json` is `UseDevelopmentStorage=true`:

```bash
docker run -d -p 10000:10000 mcr.microsoft.com/azure-storage/azurite azurite-blob --blobHost 0.0.0.0 --skipApiVersionCheck
```

**Real Azure Storage account** — do not commit the key; use user-secrets:

```bash
cd RaceDayApi/RaceDayApi
dotnet user-secrets init
dotnet user-secrets set "AzureBlob:ConnectionString" "<your storage account connection string>"
```

The container (`raceday-images`) is created automatically with public read access on blobs, so browsers can display the images.

### 14.3 Start the API

```bash
cd RaceDayApi/RaceDayApi
dotnet run --launch-profile http     # http://localhost:5157
```

### 14.4 Start the MVC application

```bash
cd RaceDayWeb
dotnet run                           # http://localhost:5200
```

The API address is set in `RaceDayWeb/appsettings.json` (`Api:BaseUrl`).

Add the MVC project to the solution once with: `dotnet sln RaceDayApi/RaceDayApi.sln add RaceDayWeb/RaceDayWeb.csproj`

## 15. Running with Docker

The API must be running on the host (section 14.3). Then, from the repository root, **one command** builds and starts the web application:

```bash
docker build -t raceday-web ./RaceDayWeb && docker run --rm -p 8080:8080 --add-host=host.docker.internal:host-gateway -e Api__BaseUrl=http://host.docker.internal:5157 raceday-web
```

Open **http://localhost:8080**. The container behaves the same as `dotnet run`; only the API address is injected through the `Api__BaseUrl` environment variable.

## 16. Using the application

**Participant:** register (choose *Participant*) → browse and filter events on the home page → open an event → **Enrol** in a category → see it under *My enrolments* (Pending until the organiser confirms) → after the event, *My results* shows time and position (e.g. "47th out of 312 finishers") → *Profile* to change details and upload a picture.

**Organiser:** register (choose *Organiser*) → *Dashboard* (events, total enrolments, upcoming dates) → *My events* to create / edit / delete events with a banner → *Categories* to add distances → *Enrolments* to confirm participants → *Results* to publish finish times and positions once the event date has passed.

Enrolment statuses are colour coded: **green = Confirmed**, **amber = Pending**, **grey = Cancelled**.

Business rules worth knowing:
- Results can only be captured for **confirmed** enrolments of events that have **taken place**; positions are unique per category.
- An event or category that already has enrolments cannot be deleted.
- A participant can hold one active enrolment per event.

## 17. Part 3 video and CI

**Unlisted YouTube demonstration:** *add the URL before submission*

The GitHub Actions workflow (`.github/workflows/blank.yml`) now restores, builds and tests the API, builds the MVC project, and builds the Docker image.

**Successful CI build screenshot (Part 3):** *add a screenshot of the green run here*
