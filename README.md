# User Management API

This API is built for TechHive Solutions to manage employee records used by HR and IT operations.

## CRUD endpoints

- GET /api/users — retrieve all users
- GET /api/users/{id} — retrieve a single user by ID
- POST /api/users — create a new user
- PUT /api/users/{id} — update an existing user
- DELETE /api/users/{id} — remove a user

## Example payload

```json
{
  "firstName": "Daniel",
  "lastName": "Smith",
  "email": "daniel.smith@techhive.com",
  "department": "Finance",
  "isActive": true
}
```

## How the app was tested

### 1. Automated xUnit Integration & Middleware Tests
The test suite in `UserManagementAPI.Tests` uses `WebApplicationFactory<Program>` to test all endpoints and custom middleware without spinning up external servers:
- **Authentication Middleware Tests**: Validates token enforcement (`techhive-secret-token`), bearer header format validation, and rejection with `401 Unauthorized` and exact error contracts.
- **Exception Handler Middleware Tests**: Validates that unhandled exceptions (`/api/debug/error`) return standard JSON error responses with `500 InternalServerError`.
- **API CRUD Tests**: Validates `GET`, `POST`, `PUT`, and `DELETE` operations, including `200 OK`, `201 Created` with `Location` header, `204 NoContent`, and `404 NotFound` scenarios.

To run the automated tests:
```bash
dotnet test
```

### 2. Manual HTTP Testing
The project includes HTTP request samples in `UserManagementAPI.http`. These calls were executed against the running API to validate each CRUD operation and confirm expected status codes:

- GET: 200 OK
- POST: 201 Created
- PUT: 200 OK
- DELETE: 204 No Content

## Microsoft Copilot assistance

Microsoft Copilot helped improve the API by:

- suggesting the minimal API structure and route grouping for user endpoints
- recommending a clean user model and DTO pattern for create/update requests
- helping map the correct HTTP status codes and response shapes for each operation
- assisting with validation of expected behavior through request examples and endpoint testing
- accelerating the final cleanup by identifying unused/default template code and reducing extra dependencies

## Run locally

```bash
dotnet run --urls http://localhost:5190
```

Then test the examples from `UserManagementAPI.http` or use a REST client.
