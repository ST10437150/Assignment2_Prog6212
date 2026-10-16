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
