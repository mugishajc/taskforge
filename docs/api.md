# TaskForge API Reference

Base URL: `http://localhost:8080/api`

All endpoints except `/api/auth/*` and `/healthz` require a JWT bearer token in the `Authorization` header.

## Authentication

### POST /auth/register

Create an account. Returns a JWT.

```json
{
  "email": "user@example.com",
  "password": "securepassword",
  "name": "Alice"
}
```

Response `201`:
```json
{
  "accessToken": "eyJ...",
  "userId": 1,
  "email": "user@example.com"
}
```

### POST /auth/login

```json
{
  "email": "user@example.com",
  "password": "securepassword"
}
```

Response `200`: same shape as register.

## Users

### GET /users/me

Returns the authenticated user's profile.

### PUT /users/me

Update your display name.

```json
{ "name": "Alice Smith" }
```

### GET /users

List all registered users. Useful for picking task assignees.

## Projects

### POST /projects

```json
{ "name": "Q1 Launch" }
```

### GET /projects

List projects owned by the current user.

### GET /projects/{id}

### DELETE /projects/{id}

## Tasks

### POST /projects/{projectId}/tasks

```json
{
  "title": "Write specs",
  "description": "Draft the technical spec",
  "priority": "High",
  "dueOn": "2025-03-01T00:00:00Z",
  "assigneeId": 2
}
```

### GET /tasks

List all tasks across projects. Supports filtering and pagination.

Query parameters:
- `status` - filter by Todo, InProgress, or Done
- `assigneeId` - filter by assigned user
- `projectId` - filter by project
- `search` - search in task title
- `page` - page number (default 1)
- `pageSize` - items per page (default 20, max 100)

Response:
```json
{
  "items": [...],
  "totalCount": 42,
  "page": 1,
  "pageSize": 20,
  "totalPages": 3
}
```

### GET /projects/{projectId}/tasks

List tasks for a specific project. Same pagination support.

### GET /tasks/{id}

### PUT /tasks/{id}

Full update of a task.

```json
{
  "title": "Write specs",
  "description": "Updated description",
  "status": "InProgress",
  "priority": "High",
  "dueOn": "2025-03-15T00:00:00Z",
  "assigneeId": 2
}
```

### PATCH /tasks/{id}/assign

Assign or unassign a task.

```json
{ "assigneeId": 2 }
```

Pass `null` to unassign.

### DELETE /tasks/{id}

## Comments

### POST /tasks/{taskId}/comments

```json
{ "body": "Looks good, let's ship it." }
```

### GET /tasks/{taskId}/comments

List comments on a task, ordered by creation time.

### DELETE /tasks/{taskId}/comments/{commentId}

Only the comment author can delete it.

## Health Check

### GET /healthz

Open endpoint. Returns `{ "status": "ok" }`.

## Error Responses

- `400` - validation error (FluentValidation)
- `401` - missing or invalid JWT
- `404` - resource not found or not owned by user
- `409` - conflict (duplicate email on register)
