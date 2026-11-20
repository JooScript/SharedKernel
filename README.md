# SharedKernel

A reusable **.NET 10** shared kernel for ASP.NET Core applications. It bundles the common building blocks (domain models, data access, business logic, and API infrastructure) that are repeated across projects, so new services can be built on top of a consistent, tested foundation instead of re-implementing the same plumbing.

- **Repository:** https://github.com/JooScript/SharedKernel
- **Target framework:** `net10.0`
- **Solution:** `SharedKernel.sln`

## Features

### Domain layer (`SharedKernel.Domains`)
- `SharedKernelBaseTable` base entity with `Id`, `CurrentState`, `CreatedAt`, `CreatedBy`, `UpdatedAt`, `UpdatedBy`.
- Common value types/enums: `CurrentStateType` (`Deleted`, `Active`, `InActive`), `SaveResultType`, `ValidationResult`, `StrEnum`.
- Domain models for refresh tokens, user sessions, and audit logs.

### Data access layer (`SharedKernel.Da`)
- Generic EF Core `DbContext` (`SharedKernelDbContext<TUser, TRole>`) built on `IdentityDbContext`, with global conventions (string max length 100) and automatic base-table configuration (keys, audit columns, soft-delete state, user FKs).
- Identity: `SharedKernelAppUser`, `SharedKernelAppRole`.
- Repository contracts & implementations:
  - `ISharedKernelQueryRepo<T>` / `SharedKernelQueryRepo<T, TDbContext>` — filtering, projection, paging, counting, existence checks.
  - `ISharedKernelTableQryRepo<T, TDbContext>` / `SharedKernelTableQryRepo<,>` — table-specific queries (`GetByIdAsync`, `GetByIdsAsync`, tracked loading).
  - `ISharedKernelTableCmdRepo<Tb, TDbContext>` / `SharedKernelTableCmdRepo<,>` — add/update, status changes (activate/deactivate/soft & hard delete).
  - `ISharedKernelViewRepo<Vw, TDbContext>` / `SharedKernelViewRepo<,>` — read-only views.
  - `ISharedKernelUnitOfWork<TDbContext>` / `SharedKernelUnitOfWork<>` — transactions (`BeginTransactionAsync`, `CommitAsync`, `RollbackAsync`, `SaveChangesAsync`).
- EF Core configurations for `TbAuditLog`, `TbRefreshToken`, `TbUserSession`.
- `DataAccessException` for consistent data-layer error reporting.

### Business logic layer (`SharedKernel.Bl`)
- `SharedKernelBaseService<Tb, Dto, TDbContext>` — generic CRUD service (get all/by id, count, exists, add, duplicate, update, activate/deactivate, soft/hard delete) with `ToDto`/`ToEntity` mapping hooks.
- `BlHelper` — normalization helpers for add/update (sets audit fields & initial state).
- Auth services: `RefreshTokenService<TDbContext>` (token generation, rotation, deactivation).
- `AuditLogService<TDbContext>` + `AuditLogSubscriber<TDbContext>` for automatic audit logging of CRUD events.
- `EmailingService` (SMTP/Gmail) implementing `IEmailing`.
- Events: `CrudPublisher` + event args (`EntityCreated`, `EntityUpdated`, `EntityStatusChanged`).
- Exceptions: `BusinessLogicException`, `MappingException`, `NotFoundException`.
- Validation attributes: `FutureDateAttribute`, `NotFutureDateAttribute`, `GreaterThanZeroAttribute`.
- Constants/enums: `SystemRoles` (`Admin`, `User`), `DeleteType`, `SaveModeType`, `FilterMode`.
- Models: `PagedQuery`, `Filter`, `JwtSettings`, `EmailSettings`, `EntityValidationResult`.
- DTOs: base DTO, user/auth DTOs (`CreateUserDto`, `RegisterDto`, `LoginDto`, `LogInResultDto`, `UserDto`, `UserResultDto`, `ForgotPasswordDto`, `ResetPasswordDto`, `RefreshTokenDto`), `AuditLogDto`.
- Localization resources: `ResMessages`, `ResLabels`, `ResActions`.

### API layer (`SharedKernel.Api`)
- Standardized `ApiResponse<T>` wrapper and `HeaderPagination` model.
- `AuthController<TDbContext>` — full auth flow: register, create admin/user, login, logout, email confirmation, forgot/reset password, refresh access/refresh tokens (rotation), 2FA flag.
- User services: `BaseUserCmdService<TDbContext, AppUser>` (registration, login, confirmation, password reset, role assignment) and `BaseUserQryService<AppUser>` (queries, claims, roles).
- `TokenService` — JWT access tokens + secure refresh tokens.
- `UserSessionService<TDbContext>` — session tracking (user agent, IP).
- Middlewares: `ErrorHandlingMiddleware` (RFC 7807 problem details), `IdempotencyMiddleware` (in-memory cache by `Idempotency-Key`), `EventsSubscriptionMiddleware`.
- `TrimStringsFilter` action filter.
- Constants: `HeadersConsts`, `RateLimitingPolicies`.
- `ServiceCollectionExtensions.AddSharedKernel()` — registers all shared services.

## Project structure

```
SharedKernel/
├── SharedKernel.sln
├── Src/
│   ├── Domains/        # Domain models & value types
│   ├── Da/             # Data access (DbContext, repos, UoW, identity, config)
│   ├── Bl/             # Business logic (services, DTOs, events, validation)
│   └── Api/            # API (controllers, middlewares, models, filters)
├── Test/
│   ├── Domains/        # Domain tests
│   ├── Da/             # Data access tests (EF InMemory)
│   ├── Bl/             # Business logic tests (Moq)
│   └── Api/            # API tests (Moq)
└── .github/workflows/  # (empty)
```

The solution also references two sibling toolkits (see **Prerequisites**):
- `..\ToolKits\CS_Utils\Src\Utils.csproj`
- `..\ToolKits\ZatcaKit\src\ZatcaKit.csproj`

## Prerequisites

- .NET SDK 10.0+
- Visual Studio 2022 (17.x) or compatible IDE
- The sibling `ToolKits` folder (`CS_Utils`, `ZatcaKit`) at `D:\Joe\GitHub\ToolKits` (or adjust the project references in the `.csproj` files).

## Getting started

### 1. Clone

```bash
git clone https://github.com/JooScript/SharedKernel.git
cd SharedKernel
```

### 2. Build

```bash
dotnet build SharedKernel.sln
```

### 3. Run tests

```bash
dotnet test SharedKernel.sln
```

Tests use xUnit, Moq, and EF Core InMemory.

## Usage in a consuming application

### Register shared services

```csharp
builder.Services.AddSharedKernel();
```

### Configure options (`appsettings.json`)

```json
{
  "Jwt": {
    "Issuer": "your-issuer",
    "Audience": "your-audience",
    "Key": "your-secret-key-at-least-32-chars",
    "ExpireMinutes": 5
  },
  "Email": {
    "SenderEmail": "you@gmail.com",
    "Password": "app-password",
    "SenderName": "Your App"
  }
}
```

### DbContext

Create your own `DbContext` deriving from (or using) `SharedKernelDbContext<TUser, TRole>`:

```csharp
public class AppDbContext : SharedKernelDbContext<AppUser, AppRole>
{
    public AppDbContext(DbContextOptions options) : base(options) { }
}
```

Register with EF Core as usual (`AddDbContext<AppDbContext>(...)`).

### Implementing a feature service

Inherit from `SharedKernelBaseService<Tb, Dto, TDbContext>` and implement `ToDto`/`ToEntity`:

```csharp
public class ProductService(AppDbContext ctx, IUserQry userQry)
    : SharedKernelBaseService<Product, ProductDto, AppDbContext>(
        ctx, /* repos */, userQry)
{
    public override ProductDto ToDto(Product entity) => new() { ... };
    public override Product ToEntity(ProductDto dto) => new() { ... };
}
```

### Auth controller

`AuthController<TDbContext>` is generic; register it with your `DbContext` type and wire `IUserCmd`/`IUserQry` implementations (e.g. `BaseUserCmdService`/`BaseUserQryService` with your `AppUser`).

## Key conventions

- Entities derive from `SharedKernelBaseTable` and use `CurrentStateType` for soft-delete (`Deleted = 0`).
- All repositories and services are async and accept `CancellationToken`.
- `AddSharedKernel()` registers repositories as **Scoped**.
- String properties default to max length 100 via EF conventions.
- Audit columns (`CreatedAt`, `CreatedBy`, etc.) are protected from modification on update.

## Testing

- `Test/Domains` — domain type tests.
- `Test/Da` — repository tests against EF Core InMemory.
- `Test/Bl` — service tests with Moq.
- `Test/Api` — controller/filter/service tests with Moq.

Run a specific project:

```bash
dotnet test Test/Da/SharedKernel.Da.Test.csproj
```

## Notes

- `EmailingService` is hardcoded to Gmail SMTP (`smtp.gmail.com:587`). Adjust for other providers.
- `TokenService` issues short-lived access tokens (5 minutes) plus 7-day refresh tokens stored server-side with rotation.
- No license file is included in this repository; add one if you plan to distribute.

## Contributing

1. Fork & clone.
2. Create a feature branch.
3. Add/adjust tests.
4. Open a pull request.