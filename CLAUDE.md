# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build & Development Commands

Everything runs inside Docker. The dev stack must be running for build/test commands.

```bash
# Start the full dev stack (proxy, frontend, backend, PostgreSQL, Redis, S3)
make dev

# Build inside the running backend container
docker compose -f docker-dev.yaml exec backend dotnet build /src/DeliverTable.sln

# Run all tests
make test

# Run unit tests only
make test-unit

# Run a single test class or method inside the dev container
docker compose -f docker-dev.yaml exec backend dotnet test /src/DeliverTableTests/DeliverTableTests.csproj --no-build --filter "FullyQualifiedName~AuthControllerTests"

# Apply EF Core migrations
make dev-migrate

# Format check / fix
make format-check
make format-fix

# Full CI gate (format → build → test → security → coverage)
make ci
```

## Architecture

**4-project .NET 10 solution**: Blazor WASM client + ASP.NET Core API + shared library + NUnit tests.

### Controller → Service → Repository

Controllers are thin HTTP orchestrators. Business logic lives in services. Repositories do pure data access with entities only (no DTOs).

```
Controller (HTTP concerns, claims extraction, ServiceResult → IActionResult)
    ↓
Service (business logic, validation, DTO ↔ entity mapping, returns ServiceResult<T>)
    ↓
Repository (EF Core queries, entity CRUD, no business logic)
```

**ServiceResult pattern**: Services return `ServiceResult<T>` or `ServiceResult` (for void operations). Controllers map these via extension methods: `.ToOkResult()`, `.ToNoContentResult()`, `.ToErrorResult()`. For `CreatedAtAction`, check `result.IsSuccess` manually.

### Key server-side types

- `ServiceResult<T>` / `ServiceError` — in `DeliverTableServer/Common/`
- `ErrorMessages` — centralized French error strings in `DeliverTableServer/Constants/`
- `PaginatedResult<T>` — shared DTO in `DeliverTableSharedLibrary/Dtos/`

### Domains and their layers

| Domain | Controller | Service | Repository |
|--------|-----------|---------|------------|
| Auth | AuthController | IAuthService / AuthService | IUserRepository / UserRepository |
| Admin | AdminController | IAdminService / AdminService | IUserRepository / UserRepository |
| Restaurant | RestaurantController | IRestaurantService / RestaurantService | IRestaurantRepository |
| Dish | DishController | IDishService / DishService | IDishRepository |
| Cart | CartController | ICartService / CartService | ICartRepository / CartRepository |
| Order | OrderController | IOrderService / OrderService | IOrderRepository / OrderRepository |
| Health | HealthController | IHealthService | — |

### Enums

Roles, statuses, and health states use enums with `nameof()` for compile-time safety. Never use hardcoded strings like `"Customer"` or `"RestaurantOwner"` — use `nameof(UserRole.Customer)`.

- `UserRole`, `HealthStatus` — in `DeliverTableSharedLibrary/Constants/Enums/`
- `UserStatus`, `RestaurantType`, `AvailableCountries` — in `DeliverTableSharedLibrary/Enums/`
- `OrderStatus`, `OrderType`, `PaymentStatus`, `BookingStatus`, `BookingSource` — in `DeliverTableSharedLibrary/Enums/`

### Client (Blazor WASM)

- JWT auth via `ApiAuthStateProvider` with localStorage token
- HTTP services mirror server endpoints (e.g., `IRestaurantService`, `IAdminService`)
- Global usings in `_Imports.razor` — includes `DeliverTableSharedLibrary.Constants.Enums` and `DeliverTableSharedLibrary.Enums`
- SCSS with variables/mixins in `Styles/abstracts/`
- Role-based UI via `<AuthorizeView Roles="@nameof(UserRole.X)">`

### API Routes

Single source of truth: `DeliverTableSharedLibrary/Constants/ApiRoutes.cs`. Used by both server `[Route]` attributes and client HTTP calls.

### DI Registration

All services registered in `DeliverTableServer/Extensions/ServiceCollectionExtensions.cs`, organized as `RegisterRepositories`, `RegisterServices`, `RegisterInfrastructure`.

## Commit Convention

Conventional Commits format with **required** Azure Boards references:

```
<type>(<scope>): <description>

PBI: AB#<id>
Task: AB#<id>
```

Types: `feat`, `fix`, `docs`, `style`, `refactor`, `perf`, `test`, `build`, `ci`, `chore`
Scopes: `client`, `server`, `shared`, `tests`, `api`, `auth`, `db`, `docker`

## Testing

- Framework: NUnit 4 + NSubstitute for mocking
- Controller tests mock the service interface (e.g., `Substitute.For<IAuthService>()`)
- Service tests mock the repository interface
- Test factories in `DeliverTableTests/*/Factories/`
- `TestDatabase` fixture provides in-memory EF Core context
- Pre-existing failure: `AppEnvironmentTests.Load_AppliesDefaults_WhenOptionalVarsAreMissing` fails when run inside Docker due to Redis env var leaking from the container

## Language

The application UI and error messages are in **French**. Error messages are centralized in `DeliverTableServer/Constants/ErrorMessages.cs`.
