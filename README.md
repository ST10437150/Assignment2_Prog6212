# RaceDay — Part 1: System Planning and Database

## System description

**RaceDay** is a sports event management system (running, walking, and cycling events) built for the South African road running community. The platform allows Organisers to create and manage events, categories, and participant results, while Participants can browse events, enter categories, and track their personal performance history.

This Part 1 covers the planning of the system: database modelling (ERD), the API endpoint plan, and the SQL script used to create and seed the database. No API code is written at this stage.

## System roles

- **Organiser** — can create, edit, and delete events, manage event categories, capture participant results, and view all enrolments for an event.
- **Participant** — can create an account, browse available events, enter an event by selecting a category, view their own enrolments, and track their personal result history.

## Contents of the /docs folder

- `RaceDay_ERD.png` — Entity Relationship Diagram with the system's 6 entities (Roles, Users, Events, Categories, Enrolments, Results), primary/foreign keys, and cardinalities.
- `RaceDay_API_Endpoints.md` — Full table of planned API endpoints (Auth, Profile, Events, Categories, Enrolments, Results).
- `RaceDay_Database.sql` — SQL script with `CREATE TABLE` statements for every entity and `INSERT` statements seeding sample data (min. 2 Organisers, 2 Participants, 3 Events).

## How to test the SQL script

1. Open SQL Server Management Studio (SSMS).
2. Connect to a local or development instance.
3. Open the file `docs/RaceDay_Database.sql`.
4. Execute (F5) on a clean instance — the script creates the database, the tables, and inserts the sample data without errors.

## CI/CD — GitHub Actions

The workflow at `.github/workflows/validate-structure.yml` automatically validates that the `/docs` folder exists and contains the three required files (ERD, endpoint plan, and SQL script) on every push/pull request.

**Green build screenshot:**

_(Add the Actions green check screenshot here before submission)_

## Video presentation

Video link (unlisted YouTube): _[add link here]_

The video walks through: the ERD design decisions, the choices made in the endpoint plan, and runs the SQL script live in SSMS.
