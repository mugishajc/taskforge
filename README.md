# TaskForge

ASP.NET Core 8 Web API for project management with task assignment, comments, and filtering.

## Stack

- .NET 8, ASP.NET Core Web API
- EF Core 8 with SQLite (local) or PostgreSQL (Docker)
- JWT bearer authentication (HS256), PBKDF2-SHA256 password hashing
- FluentValidation for request validation
- xUnit integration tests with WebApplicationFactory
- Docker + PostgreSQL for deployment

## Quick Start

### Local (SQLite)

```bash
dotnet restore
dotnet run --project src/Tasks.Api
# API is at http://localhost:5000
```

### Docker (PostgreSQL)

```bash
docker compose up --build -d
# API is at http://localhost:8080
```

### Run Tests

```bash
dotnet test
```

### Makefile

```bash
make build        # restore + build
make test         # build + run tests
make run          # start the API locally
make docker-up    # build and start with Docker
make docker-down  # stop containers
```

## Architecture

```
src/Tasks.Api/
  Auth/               JWT issuer, password hasher
  Data/               EF Core DbContext
  Domain/             Entity classes (User, Project, TaskItem, Comment)
  Features/
    Users/            Auth (register/login) + profile endpoints
    Projects/         CRUD for projects
    Tasks/            CRUD + assignment + filtering/pagination
    Comments/         Comments on tasks
  Program.cs          DI setup, middleware pipeline
tests/Tasks.Api.Tests/
  EndToEndTests.cs    Integration tests over WebApplicationFactory
```

Each feature folder holds a controller and a service. The service owns the data access logic and returns DTOs. Controllers handle HTTP concerns and auth context.

## API Endpoints

```
POST   /api/auth/register            register, returns JWT
POST   /api/auth/login               login, returns JWT
GET    /api/users/me                 current user profile
PUT    /api/users/me                 update display name
GET    /api/users                    list all users
GET    /api/projects                 list your projects
POST   /api/projects                 create project
GET    /api/projects/{id}            get project
DELETE /api/projects/{id}            delete project
POST   /api/projects/{pid}/tasks     create task in project
GET    /api/projects/{pid}/tasks     list tasks in project (paginated)
GET    /api/tasks                    list all tasks (filter, paginate)
GET    /api/tasks/{id}               get single task
PUT    /api/tasks/{id}               update task
PATCH  /api/tasks/{id}/assign        assign/unassign task
DELETE /api/tasks/{id}               delete task
POST   /api/tasks/{tid}/comments     add comment
GET    /api/tasks/{tid}/comments     list comments
DELETE /api/tasks/{tid}/comments/{id} delete comment
GET    /healthz                      health check (open)
```

### Filtering and Pagination

`GET /api/tasks` supports query parameters:

- `status` - Todo, InProgress, or Done
- `assigneeId` - filter by assigned user
- `projectId` - filter by project
- `search` - substring match on task title
- `page` - page number (default 1)
- `pageSize` - items per page (default 20, max 100)

### Example: Register and Create a Task

```bash
# Register
TOKEN=$(curl -s -X POST http://localhost:8080/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{"email":"dev@example.com","password":"secure-pass!","name":"Dev"}' \
  | jq -r .accessToken)

# Create a project
PROJECT_ID=$(curl -s -X POST http://localhost:8080/api/projects \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"name":"My Project"}' \
  | jq -r .id)

# Create a task
curl -s -X POST "http://localhost:8080/api/projects/$PROJECT_ID/tasks" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"title":"Build the thing","priority":"High"}'

# List tasks with filtering
curl -s "http://localhost:8080/api/tasks?status=Todo&pageSize=10" \
  -H "Authorization: Bearer $TOKEN"
```

## Configuration

Key settings in `appsettings.json`:

- `ConnectionStrings:Default` - database connection string
- `Jwt:Key` - signing key (change in production)
- `Jwt:Issuer` / `Jwt:Audience` - token issuer/audience
- `Jwt:AccessTokenMinutes` - token lifetime (default 60)

Swagger UI is available at `/swagger` in development mode.

## Design Notes

- Vertical slices: each feature folder holds its DTOs, service, and controller together. Adding a feature means one folder, not changes across a layered structure.
- Thin controllers: controllers map HTTP to a service call, the service owns the business rules.
- Enums stored as strings in the database so schema dumps are readable.
- Password hash format is `pbkdf2$iters$salt$hash` so the work factor can be raised later without a migration.

## Full API Docs

See [docs/api.md](docs/api.md) for the complete API reference.
