# Task Management System

A multi-tenant task management application built as a take-home assignment. The solution includes an ASP.NET Core Web API, a React single-page app, and a lightweight WPF desktop client that share the same backend.

Tenant isolation, JWT authentication, role-based authorization (Admin/User), and full task CRUD are implemented on the API. The React app provides the primary user interface; the WPF app demonstrates MVVM and API integration for desktop scenarios.

---

## Architecture

```
React SPA (TaskManagement.Web)
        |
        v
ASP.NET Core API (TaskManagement.API)
        |
        v
SQL Server (LocalDB)

WPF Desktop (TaskManagement.Desktop)
        |
        v
ASP.NET Core API (same endpoints)
```

The API is the single source of truth. Both clients authenticate with JWT and call the same REST endpoints. Tenant scope is enforced on the server using claims from the token, not from client-supplied tenant IDs on task operations.

---

## Features

### Backend (TaskManagement.API)

- Multi-tenancy — users belong to a tenant; tasks are filtered by tenant
- JWT authentication — login returns a bearer token used on protected routes
- Role-based authorization — `Admin` and `User` roles with different permissions
- Entity Framework Core with code-first migrations
- Structured logging and global exception handling middleware
- Stored procedure (`GetTasksByTenant`) available for optimized tenant task queries

### Frontend (TaskManagement.Web)

- Login and registration
- Task dashboard with tenant-scoped task list
- Create, edit, complete, and delete tasks (Admin-only for write operations)
- Responsive layout with React Router

### Desktop (TaskManagement.Desktop)

- Login against the same API
- View tasks in a DataGrid
- Mark a task as completed (Admin role required by the API)

---

## Technology Stack

| Layer | Technologies |
|-------|----------------|
| Backend | .NET 8, ASP.NET Core, EF Core, SQL Server (LocalDB) |
| Frontend | React, Vite, Axios |
| Desktop | WPF, MVVM |
| Testing | xUnit, EF Core InMemory (auth tests) |
| CI | GitHub Actions |

---

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Node.js LTS](https://nodejs.org/) (for the React app)
- SQL Server LocalDB (included with Visual Studio) or a SQL Server instance
- Windows (required to build/run the WPF project)

---

## Running the Application

### 1. Database migrations

The API applies pending migrations automatically on startup. To run them manually:

```powershell
cd TaskManagement.API
dotnet ef database update
```

> Requires the [EF Core tools](https://learn.microsoft.com/en-us/ef/core/cli/dotnet): `dotnet tool install --global dotnet-ef`

Default connection string (LocalDB) is in `TaskManagement.API/appsettings.json`. Update it if you use a different SQL Server instance.

On first run, the API seeds two tenants: **Default Tenant** (Id: 1) and **Tenant Two** (Id: 2).

### 2. Start the API

```powershell
cd TaskManagement.API
dotnet run
```

API listens on **http://localhost:5016**. Swagger UI: http://localhost:5016/swagger

### 3. Start the React app

```powershell
cd TaskManagement.Web
npm install
npm run dev
```

Open **http://localhost:5173**. Vite proxies `/api` requests to the API on port 5016.

### 4. Start the WPF desktop app

Start the API first, then:

```powershell
cd TaskManagement.Desktop
dotnet run
```

The desktop client is configured to call `http://localhost:5016` (`Helpers/ApiSettings.cs`).

---

## Test Credentials

There are no pre-seeded users. Register through the React app or Swagger:

**POST** `/api/Auth/register`

```json
{
  "username": "admin1",
  "password": "pass",
  "role": "Admin",
  "tenantId": 1
}
```

```json
{
  "username": "user1",
  "password": "pass",
  "role": "User",
  "tenantId": 1
}
```

| Role | Can view tasks | Can create/edit/complete/delete |
|------|----------------|-------------------------------|
| Admin | Yes | Yes |
| User | Yes | No (403 on write endpoints) |

Use an **Admin** account when testing task creation or the desktop “mark complete” action.

---

## Testing

Run the API unit/integration tests:

```powershell
dotnet test TaskManagement.API.Tests/TaskManagement.API.Tests.csproj
```

Tests cover registration and login flows using an in-memory database.

---

## CI/CD

GitHub Actions workflow: [`.github/workflows/ci.yml`](.github/workflows/ci.yml)

Triggers on **push** and **pull_request**.

| Job | Runner | Validates |
|-----|--------|-----------|
| .NET Build and Tests | `windows-latest` | Restores NuGet packages, builds the full solution (Release), runs `TaskManagement.API.Tests` |
| React Build | `ubuntu-latest` | `npm ci` and `npm run build` for `TaskManagement.Web` |

The Windows runner is used for the backend job because the WPF project targets `net8.0-windows`. The workflow validates builds and tests only — no deployment step is configured.

---

## Design Decisions

**JWT authentication** — Stateless tokens work well for a SPA and a desktop client calling the same API. The server validates the token on each request without server-side session storage.

**Tenant isolation** — `TenantId` is stored on the user record and included in JWT claims. Task queries filter by the authenticated user's tenant in the service layer and EF Core, so clients cannot access another tenant's data by changing request parameters.

**React + Vite** — React is a practical choice for a task dashboard with routing and form-heavy UI. Vite provides fast local development and a simple production build.

**WPF + MVVM** — The desktop app is intentionally small. MVVM keeps views thin, puts API calls in a service layer, and separates UI state in view models — a standard pattern for maintainable WPF code.

**Stored procedure** — A `GetTasksByTenant` SQL script and migration are included to show an alternative to pure LINQ for tenant-scoped reads where DB-side optimization may matter.

---

## Future Improvements

- Deploy API and React app to Azure App Service
- Store secrets (JWT key, connection strings) in Azure Key Vault
- Refresh tokens for longer-lived sessions without widening JWT expiry
- Docker containers for API and web
- More automated tests (task service, authorization, API integration tests)
- CI publish artifacts or deployment stages (CD)

---

## Project Structure

```
TaskManagement/
├── TaskManagement.API/           # ASP.NET Core Web API
├── TaskManagement.API.Tests/     # xUnit tests
├── TaskManagement.Web/           # React + Vite SPA
├── TaskManagement.Desktop/       # WPF MVVM desktop client
└── .github/workflows/ci.yml      # GitHub Actions CI pipeline
```

---

## Author

**marypamis** — take-home assignment submission.
