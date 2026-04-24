# ASP.NET Core Template

A production-ready ASP.NET Core 10 Web API template with JWT authentication, role-based authorization, EF Core + PostgreSQL, file uploads, background jobs, and Docker — ready to clone and extend.

---

## Stack

| Layer | Technology |
|---|---|
| Framework | ASP.NET Core 10 |
| Database | PostgreSQL 16 via EF Core 10 + Npgsql |
| Auth | JWT Bearer + BCrypt |
| Validation | FluentValidation |
| Background Jobs | Hangfire (PostgreSQL storage) |
| Logging | Serilog (console + rolling file) |
| API Docs | Swagger UI + Scalar |
| Containerization | Docker + Docker Compose |

---

## Project Structure

```
AspNetTemplate/
├── Core/                        # Shared infrastructure — no business logic here
│   ├── Data/
│   │   ├── Base/                # BaseEntity, BaseSoftEntity, BaseService, BaseFilter
│   │   ├── Common/              # IApiResponse, SuccessResponse, FailureResponse, PagedList
│   │   ├── Converters/          # UTC DateTime EF converter
│   │   └── DbContexts/          # AppData DbContext, interceptors, UploadedFile entity
│   └── Infra/
│       ├── Attributes/          # [AutoRegister], [RoleAuthorize], [AllowedFile]
│       ├── Extensions/          # DI wiring, app builder/logging helpers, ClaimsPrincipal helpers
│       ├── Filters/             # GlobalExceptionFilter, SoftDeleteAccessFilter, TransactionFilter
│       └── Helpers/             # JWT, FileHelper, UploadHelper, cleanup jobs, Swagger auth
│
├── Data/                        # App-level config and DB bootstrapping
│   ├── AppState.cs              # Typed config accessors (connection string, JWT key, paths)
│   ├── Enums.cs                 # Shared enums (AppRoles)
│   └── Seeder.cs                # Seeds default admin and user accounts on startup
│
└── Features/                    # One folder per domain — all business logic lives here
    └── Auth/                    # Authentication + user management
        ├── AuthController.cs    # /api/auth endpoints
        ├── UsersController.cs   # /api/users endpoints (Admin only)
        ├── Contracts/           # Request/Response DTOs + FluentValidation validators
        ├── Data/                # User, AccessToken entities + EF configurations
        └── Services/            # AuthService, UserService, TokenCleanupJob
```

### Adding a new feature

1. Create `Features/YourFeature/` with sub-folders: `Contracts/`, `Data/`, `Services/`.
2. Add a partial `AppData` file (e.g. `Data/AppData.YourFeature.cs`) to register your `DbSet<T>`.
3. Implement `IEntityTypeConfiguration<YourEntity>` — EF picks it up automatically via `ApplyConfigurationsFromAssembly`.
4. Decorate your service with `[AutoRegister(typeof(IYourService))]` — DI registration is automatic.

---

## API Endpoints

### Auth — `/api/auth`

| Method | Endpoint | Auth | Description |
|---|---|---|---|
| `POST` | `/api/auth/login` | Public | Returns a JWT access token |
| `POST` | `/api/auth/logout` | Admin, User | Revokes the current token |
| `GET` | `/api/auth/profile` | Admin, User | Returns the authenticated user's profile |
| `POST` | `/api/auth/change-password` | Admin, User | Changes own password (requires current password) |
| `POST` | `/api/auth/reset-password` | Admin | Resets any user's password by ID |

### Users — `/api/users` (Admin only)

| Method | Endpoint | Description |
|---|---|---|
| `POST` | `/api/users` | Create a new user (supports avatar upload) |
| `GET` | `/api/users` | List all users (paginated + filterable) |
| `GET` | `/api/users/{id}` | Get a user by ID |
| `PUT` | `/api/users/{id}` | Update a user |
| `DELETE` | `/api/users/{id}/soft` | Soft-delete a user |
| `PATCH` | `/api/users/{id}/soft` | Restore a soft-deleted user |
| `DELETE` | `/api/users/{id}` | Permanently delete (requires prior soft-delete) |

---

## Default Credentials

| Username | Password | Role |
|---|---|---|
| `admin` | `Password@123` | Admin |
| `john.doe` | `Password@123` | User |

> Change these immediately in any non-development environment.

---

## Local Development

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/)

### 1. Configure secrets

Copy `appsettings.local.json` and fill in your local values (git-ignored). The Docker Compose defaults work as-is for a clean local run.

For production, use [.NET User Secrets](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets):

```bash
dotnet user-secrets set "Jwt:SecretKey" "your-64-byte-secret"
dotnet user-secrets set "ConnectionStrings:C_str_PostgreSql" "Host=...;..."
```

### 2. Run with Docker Compose (recommended)

```bash
docker compose up --build
```

API available at `http://localhost:8080`.

### 3. Run locally (without Docker)

Start a PostgreSQL instance, then:

```bash
cd AspNetTemplate
dotnet run
```

EF migrations and seed data are applied automatically on startup.

---

## Dev Tools

| UI | URL |
|---|---|
| Swagger UI | `http://localhost:8080/swagger` |
| Scalar | `http://localhost:8080/scalar/v1` |
| Hangfire Dashboard | `http://localhost:8080/hangfire` |
| Health Check | `http://localhost:8080/health-check` |

---

## Key Patterns

### Uniform API responses

All service methods return `IApiResponse`. Controllers are a single line after validation:

```csharp
var result = await myService.DoSomethingAsync(req, ct);
return StatusCode((int)result.StatusCode, result);
```

Response types: `SuccessResponse<T>`, `EmptySuccessResponse`, `FailureResponse`.

### Role-based authorization

```csharp
[RoleAuthorize(Enums.AppRoles.Admin)]
[RoleAuthorize(Enums.AppRoles.Admin, Enums.AppRoles.User)]
```

### Auto-registration

```csharp
[AutoRegister(typeof(IMyService))]
public class MyService : BaseService, IMyService { }
```

No manual `services.AddScoped` needed — the assembly scan wires it up.

### Soft delete

Entities extending `BaseSoftEntity` support `SoftDelete()` / `UnSoftDelete()`. Soft-deleted records are automatically excluded from queries by the global `SoftDeleteAccessFilter`.

### Pagination

```csharp
var result = await PagedList<MyDto>.Create(
    query.Select(MyDto.Projection), filter.PageNumber, filter.PageSize, ct);
return new SuccessResponse<PagedList<MyDto>>(result);
```

### Logging

Configured via `builder.AddSerilogLogging()` in `AppBuilderExtensions`. Development mode enables `Debug`-level EF Core logs to a rolling file under `DebugEFLogs/`; production emits `Warning`+ only to a versioned rolling file under `Logs/`.

### File upload (public, served statically)

Implement `IHasOptionalFile` or `IHasFile` on your entity and call:

```csharp
await FileHelper.TryUploadOptionalAsync(RootPath, errorEvents, logger, _context, entity, file, "folder", ct);
```

Files land under `data/uploads/` and are served at `/uploads/<path>`.

---

## Configuration Reference

| Key | Description |
|---|---|
| `ConnectionStrings:C_str_PostgreSql` | PostgreSQL connection string |
| `AppSetting:App_domain` | Domain used as JWT issuer/audience |
| `AppSetting:Uploads_Domain` | Base URL prepended to uploaded file URLs |
| `Jwt:SecretKey` | JWT signing key — minimum 64 bytes |
| `SwaggerSetup:UserName` / `Password` | Optional HTTP Basic Auth for Swagger UI |
