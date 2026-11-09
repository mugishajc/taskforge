# dotnet-tasks-api

ASP.NET Core 8 Web API for managing projects and tasks. JWT auth, EF Core with SQLite, FluentValidation, Swagger. Every endpoint is scoped to the authenticated user, so a user can only see and modify their own projects.

## Stack

- .NET 8, ASP.NET Core Web API
- EF Core 8 with SQLite (InMemory in tests)
- JWT bearer authentication (HS256), PBKDF2-SHA256 password hashing with a constant-time compare
- FluentValidation for request validation
- xUnit integration tests over `WebApplicationFactory`

## Endpoints

```
POST /api/auth/register         register, returns JWT
POST /api/auth/login            login, returns JWT
GET  /api/projects              list projects for the current user
POST /api/projects              create a project
GET  /api/projects/{id}
DELETE /api/projects/{id}
GET  /api/projects/{id}/tasks?status=Todo|InProgress|Done
POST /api/projects/{id}/tasks
PUT  /api/tasks/{id}
DELETE /api/tasks/{id}
GET  /healthz                   open, for load balancers
```

## Run

```bash
dotnet run --project src/Tasks.Api
# Swagger at http://localhost:5000/swagger in Development
```

## Test

```bash
dotnet test
```

The tests cover:

- password hashing round-trip, wrong-password rejection and tampered-hash rejection
- healthz is open while `/api/projects` is 401 without a token
- full register, login, create project, create task, update task flow
- another user cannot see or fetch a project they don't own
- duplicate registration is `409 Conflict`

## Why it is shaped this way

- Vertical slices: each feature folder (`Users`, `Projects`, `Tasks`) holds its DTOs, service and controller together, so adding a feature means one folder, not changes across a layered monolith.
- Thin controllers: controllers map HTTP to a service call, the service owns the business rules. Easy to test either layer in isolation.
- Enums stored as strings in SQLite so a schema dump is readable and migrations stay honest when a value is added.
- Password hash format is `pbkdf2$iters$salt$hash` so the work factor can be raised later without a migration.
