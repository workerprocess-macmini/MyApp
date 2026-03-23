# MyApp

A production-ready **.NET 10 Web API** boilerplate using **Clean Architecture**, **CQRS**, **Entity Framework Core**, **JWT authentication with refresh tokens**, and a full test suite.

[![CI](https://github.com/workerprocess-macmini/MyApp/actions/workflows/ci.yml/badge.svg)](https://github.com/workerprocess-macmini/MyApp/actions/workflows/ci.yml)
[![CD](https://github.com/workerprocess-macmini/MyApp/actions/workflows/cd.yml/badge.svg)](https://github.com/workerprocess-macmini/MyApp/actions/workflows/cd.yml)

---

## Tech Stacks

| Layer | Technology |
|---|---|
| Framework | ASP.NET Core 10 |
| ORM | Entity Framework Core 10 (SQL Server) |
| CQRS | MediatR 12 |
| Validation | FluentValidation 11 |
| Authentication | JWT Bearer + Refresh Tokens |
| Password Hashing | BCrypt.Net |
| API Docs | Scalar (OpenAPI) |
| Logging | Serilog (structured JSON + rolling file) |
| Rate Limiting | ASP.NET Core built-in (fixed + sliding window) |
| Health Checks | ASP.NET Core + EF Core DbContext probe |
| Unit Tests | xUnit + Moq + FluentAssertions |
| Integration Tests | WebApplicationFactory + SQLite |
| CI/CD | GitHub Actions + GHCR |
| Container | Docker + Docker Compose |

---

## Architecture

```
src/
├── MyApp.Domain/           # Entities, base types — no dependencies
├── MyApp.Application/      # CQRS handlers, validators, interfaces
├── MyApp.Infrastructure/   # EF Core, repositories, JWT, BCrypt
└── MyApp.API/              # Controllers, middleware, Program.cs

tests/
├── MyApp.Domain.Tests/         # Entity logic (23 tests)
├── MyApp.Application.Tests/    # Handler + validator tests (33 tests)
└── MyApp.IntegrationTests/     # Full HTTP round-trip tests (24 tests)
```

**Dependency rule:** each layer only references the layer directly inside it. `Domain` has zero dependencies.

---

## Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- SQL Server / LocalDB **or** Docker

### Run locally

```bash
# 1. Clone
git clone https://github.com/workerprocess-macmini/MyApp.git
cd MyApp

# 2. Update the connection string (appsettings.json uses LocalDB by default)
#    and set a real JWT secret key

# 3. Apply migrations and start
cd src/MyApp.API
dotnet run
```

The database is **created, migrated, and seeded automatically** on first run.

Open **https://localhost:{port}/scalar** for the interactive API docs.

### Run with Docker

```bash
# 1. Copy and fill in secrets
cp .env.example .env

# 2. Start API + SQL Server
docker compose up
```

API → `http://localhost:8080`
Scalar docs → `http://localhost:8080/scalar`

---

## Configuration

| Key | Description | Default |
|---|---|---|
| `ConnectionStrings:DefaultConnection` | SQL Server connection string | LocalDB |
| `JwtSettings:SecretKey` | Signing key (min 32 chars) | ⚠️ change this |
| `JwtSettings:Issuer` | JWT issuer | `MyApp` |
| `JwtSettings:Audience` | JWT audience | `MyApp` |
| `JwtSettings:AccessTokenExpiryMinutes` | Access token lifetime | `15` |
| `JwtSettings:RefreshTokenExpiryDays` | Refresh token lifetime | `7` |

Override any value via environment variables using `__` as separator:

```bash
JwtSettings__SecretKey=my-super-secret-key
```

---

## API Endpoints

### Auth

| Method | Endpoint | Auth | Rate limit | Description |
|---|---|---|---|---|
| `POST` | `/api/auth/register` | — | 10 req/min | Register a new user |
| `POST` | `/api/auth/login` | — | 10 req/min | Login, receive tokens |
| `POST` | `/api/auth/refresh` | — | 10 req/min | Rotate refresh token |
| `POST` | `/api/auth/revoke` | Bearer | 10 req/min | Revoke refresh token (logout) |

**Register / Login response:**
```json
{
  "accessToken": "eyJ...",
  "refreshToken": "base64...",
  "email": "user@example.com",
  "firstName": "Jane",
  "lastName": "Doe"
}
```

### Products (requires `Authorization: Bearer <token>`)

| Method | Endpoint | Rate limit | Description |
|---|---|---|---|
| `GET` | `/api/products` | 60 req/min | List all products |
| `GET` | `/api/products/{id}` | 60 req/min | Get product by ID |
| `POST` | `/api/products` | 60 req/min | Create product |
| `PUT` | `/api/products/{id}` | 60 req/min | Update product |
| `DELETE` | `/api/products/{id}` | 60 req/min | Delete product |

### Health

| Method | Endpoint | Auth | Description |
|---|---|---|---|
| `GET` | `/health/live` | — | Liveness — returns `200` if the process is up |
| `GET` | `/health/ready` | — | Readiness — checks DB connectivity, returns JSON |

**Readiness response:**
```json
{
  "status": "Healthy",
  "duration": "00:00:00.012",
  "checks": [
    { "name": "database", "status": "Healthy", "duration": "00:00:00.011", "description": null, "error": null }
  ]
}
```

Returns `HTTP 200` when healthy, `HTTP 503` when unhealthy.

---

## Logging

Structured logging via [Serilog](https://serilog.net/). Every request is logged with method, path, status code, elapsed time, and authenticated user ID.

| | Development | Production |
|---|---|---|
| Console format | Human-readable text | Compact JSON (CLEF) |
| File output | — | `logs/myapp-YYYYMMDD.log`, 14-day retention |
| Min level | `Debug` | `Information` |
| EF Core SQL | `Information` | `Warning` |

Each log entry is enriched with `MachineName` and `ProcessId`. Override log levels via environment variables:

```bash
Serilog__MinimumLevel__Default=Warning
```

---

## Rate Limiting

| Policy | Endpoints | Algorithm | Limit | Partition |
|---|---|---|---|---|
| `auth` | `/api/auth/*` | Fixed window | 10 req / min | Remote IP |
| `api` | `/api/products/*` | Sliding window | 60 req / min | User ID (JWT) or IP |

Exceeded requests receive `HTTP 429 Too Many Requests`.

---

## Seed Data

On first startup the database is seeded with:

| Email | Password | Role |
|---|---|---|
| `admin@myapp.com` | `Admin1234!` | Admin |
| `john@myapp.com` | `User1234!` | User |
| `jane@myapp.com` | `User1234!` | User |

And 5 sample products (Laptop Pro 15, Wireless Mouse, etc.).

---

## Tests

```bash
# All tests
dotnet test

# By project
dotnet test tests/MyApp.Domain.Tests
dotnet test tests/MyApp.Application.Tests
dotnet test tests/MyApp.IntegrationTests
```

| Suite | Tests | Tooling |
|---|---|---|
| Domain | 23 | xUnit + FluentAssertions |
| Application | 33 | xUnit + Moq + FluentAssertions |
| Integration | 24 | WebApplicationFactory + SQLite |
| **Total** | **80** | |

Integration tests spin up the full ASP.NET Core pipeline against an in-memory SQLite database — no SQL Server required.

---

## Migrations

```bash
# Add a new migration
dotnet ef migrations add <MigrationName> \
  --project src/MyApp.Infrastructure \
  --startup-project src/MyApp.API

# Apply to database
dotnet ef database update \
  --project src/MyApp.Infrastructure \
  --startup-project src/MyApp.API
```

---

## CI/CD

| Workflow | Trigger | Steps |
|---|---|---|
| **CI** | Push + PRs to `master` | Build → Unit tests → Integration tests → Report |
| **CD** | Push to `master` | Build → All tests → Docker build → Push to GHCR |

Docker image is published to:

```
ghcr.io/workerprocess-macmini/myapp:latest
ghcr.io/workerprocess-macmini/myapp:sha-<commit>
```

```bash
docker pull ghcr.io/workerprocess-macmini/myapp:latest
```

---

## Project Structure

```
MyApp/
├── .github/workflows/
│   ├── ci.yml                  # Build + test on push / PR
│   └── cd.yml                  # Docker publish on master
├── src/
│   ├── MyApp.Domain/
│   │   ├── Common/BaseEntity.cs
│   │   └── Entities/           # Product, User, RefreshToken
│   ├── MyApp.Application/
│   │   ├── Common/
│   │   │   ├── Behaviors/      # ValidationBehavior (MediatR pipeline)
│   │   │   ├── Interfaces/     # IRepository, IUnitOfWork, IJwtTokenService…
│   │   │   └── Models/         # Result<T>
│   │   └── Features/
│   │       ├── Auth/           # Register, Login, Refresh, Revoke
│   │       └── Products/       # CRUD commands + queries
│   ├── MyApp.Infrastructure/
│   │   ├── Migrations/
│   │   ├── Persistence/        # AppDbContext, repositories, seeder
│   │   └── Services/           # JwtTokenService, PasswordHasher
│   └── MyApp.API/
│       ├── Controllers/        # AuthController, ProductsController
│       ├── HealthChecks/       # HealthCheckResponseWriter
│       ├── Middleware/         # ExceptionHandlingMiddleware
│       ├── OpenApi/            # BearerSecuritySchemeTransformer
│       └── Program.cs
├── tests/
│   ├── MyApp.Domain.Tests/
│   ├── MyApp.Application.Tests/
│   └── MyApp.IntegrationTests/
├── Dockerfile
├── docker-compose.yml
└── .env.example
```
