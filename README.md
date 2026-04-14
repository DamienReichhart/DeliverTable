# DeliverTable

A full-stack food delivery and restaurant management platform built with .NET 10. Customers browse restaurants, build carts, and place orders. Restaurant owners manage menus, track orders, and monitor earnings. Administrators oversee the entire platform through a comprehensive dashboard.

## Table of Contents

- [Key Features](#key-features)
- [Tech Stack](#tech-stack)
- [Architecture](#architecture)
- [Prerequisites](#prerequisites)
- [Getting Started](#getting-started)
- [Environment Variables](#environment-variables)
- [Available Commands](#available-commands)
- [Project Structure](#project-structure)
- [Architecture Deep Dive](#architecture-deep-dive)
- [Database Schema](#database-schema)
- [API Reference](#api-reference)
- [Design System](#design-system)
- [Testing](#testing)
- [CI/CD Pipeline](#cicd-pipeline)
- [Deployment](#deployment)
- [Troubleshooting](#troubleshooting)
- [Contributing](#contributing)
- [License](#license)
- [Commercial Licensing](#commercial-licensing)

---

## Key Features

**For Customers**
- Browse restaurants with list and interactive map views (Leaflet.js)
- Filter by cuisine, location, and availability
- Build a cart, apply discount codes and promotions
- Place delivery or pickup orders with real-time status tracking
- Earn and redeem loyalty points
- Rate restaurants and dishes

**For Restaurant Owners**
- Create and manage restaurant profiles with image uploads
- Full menu management (dishes, pricing, availability)
- Real-time order dashboard with status workflow (Pending → Confirmed → Preparing → Ready → Delivered)
- Earnings tracking, balance management, and withdrawal requests
- Create promotions, discount codes, and loyalty programs

**For Administrators**
- Platform-wide dashboard with analytics (charts, KPIs, recent activity)
- User management (roles, statuses, suspensions, bans)
- Restaurant and dish moderation
- Order oversight and configuration (rules, blocked slots)
- Transaction monitoring and financial reports
- Notification and event management

---

## Tech Stack

| Layer | Technology |
|-------|-----------|
| **Frontend** | Blazor WebAssembly (.NET 10) |
| **Backend** | ASP.NET Core 10 Web API |
| **Database** | PostgreSQL 15+ |
| **Cache** | Redis |
| **Object Storage** | Garage (S3-compatible) |
| **Reverse Proxy** | Nginx |
| **Authentication** | JWT Bearer tokens + ASP.NET Core Identity |
| **ORM** | Entity Framework Core 10 |
| **Styling** | SCSS with design tokens (DM Serif Display + Plus Jakarta Sans) |
| **Icons** | Blazicons Lucide |
| **Maps** | Leaflet.js via JS interop |
| **Testing** | NUnit 4 + NSubstitute + Coverlet |
| **CI/CD** | GitHub Actions (3-tier pipeline) |
| **Containerization** | Docker Compose (dev + prod) |
| **Production Ingress** | Cloudflare Tunnel (Zero Trust) |

---

## Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                        Nginx Reverse Proxy                      │
│                         (port 8080)                             │
├──────────────────────────┬──────────────────────────────────────┤
│                          │                                      │
│   ┌──────────────────┐   │   ┌──────────────────────────────┐   │
│   │  Blazor WASM     │   │   │  ASP.NET Core API            │   │
│   │  (Frontend)      │   │   │  (Backend)                   │   │
│   │  port 5147       │◄──┼──►│  port 5268                   │   │
│   └──────────────────┘   │   └──────┬───────┬───────┬───────┘   │
│                          │          │       │       │            │
│        Frontend Net      │    ┌─────┴──┐ ┌──┴───┐ ┌─┴──────┐   │
│       192.168.50.0/24    │    │Postgres│ │Redis │ │Garage  │   │
│                          │    │  5432  │ │ 6379 │ │(S3)    │   │
│                          │    └────────┘ └──────┘ │ 3900   │   │
│                          │                        └────────┘   │
│                          │        Backend Net                   │
│                          │       192.168.60.0/24                │
└──────────────────────────┴──────────────────────────────────────┘
```

**Request flow:** Browser → Nginx → Blazor WASM (static files) or ASP.NET Core API → Service → Repository → PostgreSQL

The frontend and backend live on isolated Docker networks. Only Nginx bridges both, routing `/api/*` to the backend and everything else to the Blazor WASM static files.

---

## Prerequisites

- **Docker** with Compose plugin (v2+)
- **Make** (GNU Make)

That's it. The entire dev stack runs inside Docker containers. No local .NET SDK, Node.js, or database installation required.

---

## Getting Started

### 1. Clone the Repository

```bash
git clone https://github.com/DamienReichhart/DeliverTable.git
cd DeliverTable
```

### 2. Configure Environment Variables

```bash
cp .env.example .env
```

The defaults in `.env.example` work out of the box for local development. See [Environment Variables](#environment-variables) for details on each setting.

### 3. Install Git Hooks

```bash
make hooks-install
```

This configures pre-commit (format check + build) and commit-msg (conventional commit validation) hooks.

### 4. Start the Development Stack

```bash
make dev
```

This starts 7 containers: Nginx proxy, Blazor frontend, ASP.NET backend, PostgreSQL, Redis, Garage (S3), and S3 Manager UI. First run takes a few minutes to build images and restore NuGet packages.

### 5. Open the Application

| Service | URL |
|---------|-----|
| **Application** | [http://localhost:8080](http://localhost:8080) |
| **API Documentation** (Swagger) | [http://localhost:8080/docs](http://localhost:8080/docs) |
| **S3 Manager** | [http://localhost:8888](http://localhost:8888) |

### 6. Apply Database Migrations

On first run, or after pulling new migrations:

```bash
make dev-migrate
```

### 7. Verify Everything Works

```bash
make check    # format-check → build → test (quick pre-commit gate)
```

---

## Stripe webhooks (dev)

Webhook events for Stripe are forwarded to the backend via the Stripe CLI. Install from [stripe.com/docs/stripe-cli](https://stripe.com/docs/stripe-cli), then:

```bash
stripe login
stripe listen --forward-to http://localhost:5268/api/v1/stripe/webhook
```

The CLI prints a signing secret starting with `whsec_...`. Copy it into `STRIPE_WEBHOOK_SECRET` in your local `.env`. Restart the backend container for it to reload (`docker compose -f docker-dev.yaml restart backend`).

Trigger a test event:

```bash
stripe trigger payment_intent.amount_capturable_updated
```

---

## Environment Variables

All environment variables live in a single `.env` file at the repository root. Copy `.env.example` to `.env` and adjust as needed. Never commit `.env`.

### Infrastructure

| Variable | Description | Default |
|----------|-------------|---------|
| `POSTGRES_DB` | PostgreSQL database name | `delivertable` |
| `POSTGRES_USER` | PostgreSQL username | `delivertable` |
| `POSTGRES_PASSWORD` | PostgreSQL password | `delivertable_dev` |
| `REDIS_PASSWORD` | Redis password (used in production) | `redis_dev_password` |
| `GARAGE_RPC_SECRET` | Garage inter-node RPC secret | *(dev default provided)* |
| `GARAGE_ADMIN_TOKEN` | Garage admin API bearer token | `devadmintoken` |
| `GARAGE_S3_ACCESS_KEY` | S3 access key (format: `GK` + 24 hex) | *(dev default provided)* |
| `GARAGE_S3_SECRET_KEY` | S3 secret key (64 hex chars) | *(dev default provided)* |
| `GARAGE_BUCKET_NAME` | S3 bucket name | `delivertable` |

### Server (ASP.NET Core)

| Variable | Description | Default |
|----------|-------------|---------|
| `ASPNETCORE_ENVIRONMENT` | Runtime environment | `Development` |
| `ASPNETCORE_URLS` | Listen URLs | `http://0.0.0.0:5268` |
| `CONNECTION_STRING_DATABASE` | PostgreSQL connection string | *(docker-internal)* |
| `CONNECTION_STRING_REDIS` | Redis connection string | *(docker-internal)* |
| `JWT_KEY` | HMAC-SHA256 signing key | *(dev default provided)* |
| `JWT_ISSUER` | JWT issuer claim | `DeliverTable` |
| `JWT_AUDIENCE` | JWT audience claim | `DeliverTable` |
| `JWT_EXPIRE_MINUTES` | Token expiration | `60` |
| `OBJECT_STORAGE_SERVICE_URL` | S3-compatible endpoint | *(docker-internal)* |
| `OBJECT_STORAGE_ACCESS_KEY` | S3 access key | *(mirrors Garage key)* |
| `OBJECT_STORAGE_SECRET_KEY` | S3 secret key | *(mirrors Garage key)* |
| `OBJECT_STORAGE_BUCKET_NAME` | S3 bucket | `delivertable` |
| `OBJECT_STORAGE_FORCE_PATH_STYLE` | Path-style S3 URLs | `true` |
| `OBJECT_STORAGE_REGION` | S3 signing region | `garage` |
| `UPLOAD_MAX_SIZE_MB` | Max upload file size | `5` |
| `OPENAPI_ENABLE_DOCUMENTATION` | Expose Swagger in non-dev envs | `false` |

### Development Ports

| Variable | Description | Default |
|----------|-------------|---------|
| `PROXY_PORT` | Nginx proxy host port | `8080` |
| `DB_PORT` | PostgreSQL host port | `5432` |
| `GARAGE_API_PORT` | Garage S3 host port | `3900` |
| `REDIS_PORT` | Redis host port | `6379` |
| `S3_MANAGER_PORT` | S3 Manager UI host port | `8888` |
| `COVERAGE_THRESHOLD` | Minimum code coverage % | `80` |

### Production

| Variable | Description |
|----------|-------------|
| `CLOUDFLARE_TUNNEL_TOKEN` | Cloudflare Zero Trust tunnel credential |

### Client Configuration

The client uses a JSON config file (not environment variables) served to the browser:

- `wwwroot/appconfig.json` — production defaults
- `wwwroot/appconfig.Development.json` — dev overrides

Schema: `{ "api": { "baseUrl": "" }, "environment": "Development" }`

Inject `IAppConfiguration` in services to access the API base URL.

---

## Available Commands

Run `make help` for the full list. Key commands:

### Development

| Command | Description |
|---------|-------------|
| `make dev` | Start the full dev stack (proxy, frontend, backend, database, Redis, Garage, S3 Manager) |
| `make dev-detach` | Start in background (detached mode) |
| `make dev-down` | Stop all containers |
| `make dev-down-volumes` | Stop containers and delete data volumes |
| `make dev-logs` | Tail container logs |
| `make dev-migrate` | Apply EF Core database migrations |

### Build & Quality

| Command | Description |
|---------|-------------|
| `make format-check` | Check code formatting (dotnet format) |
| `make format-fix` | Auto-fix formatting issues |
| `make build-release` | Clean Release build (warnings as errors) |
| `make security-audit` | Scan for vulnerable NuGet packages |
| `make outdated` | List outdated NuGet packages |

### Testing

| Command | Description |
|---------|-------------|
| `make test` | Run the full test suite |
| `make test-unit` | Run unit tests only |
| `make test-integration` | Run integration tests (ephemeral database) |
| `make coverage` | Generate HTML coverage report → `./reports/coverage/` |
| `make check` | Quick pre-commit gate: format → build → test |
| `make ci` | Full CI gate: format → build → test → security → coverage |

To run a specific test class:

```bash
docker compose -f docker-dev.yaml exec backend \
  dotnet test /src/DeliverTableTests/DeliverTableTests.csproj \
  --no-build --filter "FullyQualifiedName~AuthServiceTests"
```

### Production

| Command | Description |
|---------|-------------|
| `make prod` | Start production stack (includes Cloudflare tunnel) |
| `make prod-down` | Stop production containers |
| `make prod-rebuild` | Rebuild and restart production |

### Git Hooks

| Command | Description |
|---------|-------------|
| `make hooks-install` | Install pre-commit + commit-msg hooks |
| `make hooks-uninstall` | Remove Git hooks |

---

## Project Structure

```
DeliverTable/
├── DeliverTableServer/              # ASP.NET Core 10 Web API
│   ├── Controllers/                 #   26 controllers (Auth, Restaurant, Admin*, etc.)
│   ├── Services/                    #   Business logic (interfaces + implementations)
│   │   └── Interfaces/
│   ├── Repositories/                #   Data access layer (EF Core)
│   │   └── Interfaces/
│   ├── Models/                      #   32 EF Core entity models
│   ├── Data/                        #   DbContext + Fluent API configurations
│   │   └── ModelConfiguration/
│   ├── Mappers/                     #   Entity ↔ DTO mapping
│   ├── Middleware/                   #   RefreshClaimsMiddleware
│   ├── Configuration/               #   AppEnvironment, JwtConfig
│   ├── Constants/                   #   ErrorMessages (French), OpenApiConstants
│   ├── Common/                      #   ServiceResult<T> pattern
│   ├── Extensions/                  #   DI registration, JWT setup, pagination
│   ├── Infrastructure/              #   ObjectStorage, middleware
│   ├── Migrations/                  #   EF Core migration history
│   └── Program.cs                   #   Application entry point
│
├── DeliverTableClient/              # Blazor WebAssembly 10
│   ├── Pages/                       #   Routable pages (34 total)
│   │   ├── Admin/                   #     14 admin pages (dashboard, CRUD panels)
│   │   ├── Auth/                    #     Login, Register, RestaurantRegister
│   │   ├── Explore/                 #     Restaurants (list + map), Cart, RestaurantDetail
│   │   ├── Order/                   #     OrderConfirmation, OrderHistory
│   │   ├── Profile/                 #     User profile
│   │   └── Restaurant/             #     Owner pages (orders, account, menus, promotions)
│   ├── Components/                  #   Reusable components (21 total)
│   │   ├── Admin/                   #     Charts, tables, modals, sidebar, stat cards
│   │   ├── Forms/                   #     RestaurantCreationForm
│   │   ├── Lists/                   #     RestaurantList
│   │   ├── Modales/                 #     StandardModale
│   │   └── Dish/                    #     DishManagement
│   ├── Services/                    #   HTTP clients mirroring backend API
│   │   ├── Auth/                    #     ApiAuthStateProvider, AuthService, JwtInterceptor
│   │   └── Interfaces/
│   ├── Layout/                      #   MainLayout, AuthLayout, NavMenu
│   ├── Styles/                      #   SCSS design system
│   │   ├── abstracts/               #     _variables.scss, _mixins.scss
│   │   ├── base/                    #     _reset.scss, _typography.scss
│   │   ├── components/              #     Buttons, forms, alerts, admin components
│   │   ├── layout/                  #     Nav, sidebar, page, modal
│   │   ├── pages/                   #     Page-specific styles
│   │   └── app.scss                 #     Main entry point
│   ├── wwwroot/                     #   Static assets (CSS, JS, images, PWA manifest)
│   │   └── js/restaurant-map.js     #     Leaflet.js map interop
│   └── Configuration/              #   App config loader
│
├── DeliverTableSharedLibrary/       # Shared types (no external dependencies)
│   ├── Constants/
│   │   ├── ApiRoutes.cs             #   Single source of truth for all API paths
│   │   ├── UploadLimits.cs
│   │   └── Enums/                   #   UserRole, HealthStatus
│   ├── Enums/                       #   17 business enums (OrderStatus, PaymentStatus, etc.)
│   └── Dtos/                        #   90+ DTOs organized by domain
│       ├── Admin/                   #     42 admin dashboard DTOs
│       ├── Auth/                    #     Login, Register, Profile DTOs
│       ├── Cart/, Dish/, Order/     #     Domain DTOs
│       ├── Restaurant/              #     RestaurantDto, RestaurantMapDto, queries
│       └── PaginatedResult.cs       #     Generic pagination wrapper
│
├── DeliverTableTests/               # NUnit 4 test suite
│   ├── Server/
│   │   ├── Unit/
│   │   │   ├── Controllers/         #   Controller tests (mock services)
│   │   │   ├── Services/            #   Service tests (mock repositories)
│   │   │   └── Mappers/
│   │   ├── Integration/             #   EF Core database tests
│   │   └── Fixtures/                #   TestDatabase (in-memory EF context)
│   ├── Client/
│   │   ├── Unit/Services/           #   HTTP client service tests
│   │   ├── Factories/               #   ClientTestFactory (JWT tokens, responses)
│   │   └── Helpers/                 #   MockHttpMessageHandler
│   ├── SharedLibrary/               #   DTO validation, API route tests
│   └── Global/Helpers/              #   Auth, UserManager, validation helpers
│
├── docker/                          # Docker configuration
│   ├── images/                      #   Dockerfiles for all 7 services (dev + prod)
│   └── config/                      #   Nginx, Garage, PostgreSQL init configs
│
├── .github/
│   ├── workflows/                   #   CI pipeline (ci.yml + 8 reusable workflows)
│   ├── actions/setup-dotnet/        #   Custom .NET setup action
│   └── dependabot.yml               #   Auto-dependency updates
│
├── docker-dev.yaml                  # Development Docker Compose
├── docker-prod.yaml                 # Production Docker Compose (+ Cloudflare tunnel)
├── docker-utils.yaml                # Utility services (test runner, coverage)
├── Makefile                         # Build automation (dev, test, prod, CI)
├── DeliverTable.sln                 # .NET 10 solution (4 projects)
├── .env.example                     # Environment variable template
└── CLAUDE.md                        # AI coding assistant guidelines
```

---

## Architecture Deep Dive

### Controller → Service → Repository

The backend follows a strict three-layer pattern:

```
Controller (HTTP concerns, JWT claims extraction, ServiceResult → IActionResult)
    ↓
Service (business logic, validation, DTO ↔ entity mapping, returns ServiceResult<T>)
    ↓
Repository (EF Core queries, entity CRUD, no business logic, no DTOs)
```

**Controllers** are thin HTTP orchestrators. They extract user identity from JWT claims, delegate to services, and map results to HTTP responses. No business logic lives here.

**Services** contain all business rules. They accept DTOs, validate inputs, perform authorization checks, map between DTOs and entities, and return `ServiceResult<T>` (never throw exceptions for expected failures).

**Repositories** do pure data access. They work with EF Core entities only and never see DTOs. All queries use `IQueryable` for composability.

### ServiceResult Pattern

Services never throw exceptions for business errors. Instead, they return discriminated result types:

```csharp
// For operations that return data
ServiceResult<T>.Success(value)
ServiceResult<T>.Failure(new ServiceError("Message", 404))

// For void operations
ServiceResult.Success()
ServiceResult.Failure(new ServiceError("Message", 400))
```

Controllers map results to HTTP responses using extension methods:

```csharp
return result.ToOkResult();         // 200 + value, or error status code
return result.ToNoContentResult();  // 204, or error status code
return result.ToOkMessageResult();  // 200 + { Message }, or error
```

### Authentication Flow

1. User submits credentials to `POST /api/v1/auth/login`
2. `AuthService` validates credentials via ASP.NET Core Identity
3. `TokenService` generates a JWT with `sub` (user ID) and `role` claims
4. Client stores the JWT in `localStorage` via JS interop
5. `ApiAuthStateProvider` notifies Blazor of authenticated state
6. All subsequent API calls include the JWT in the `Authorization` header
7. `RefreshClaimsMiddleware` runs on every authenticated request:
   - Fetches current user from the database
   - Rejects suspended/banned accounts (403)
   - Updates role claims if they changed (no token refresh needed)

### Dependency Injection

All services are registered in `ServiceCollectionExtensions.cs`, organized into three groups:

- `RegisterRepositories()` — 15 repositories (Scoped)
- `RegisterServices()` — 37 services (Scoped)
- `RegisterInfrastructure()` — HttpClient-backed services (GeoLocation)

Called from `Program.cs` as `builder.Services.AddDeliverTableServices()`.

---

## Database Schema

The database uses 32 EF Core entity models with PostgreSQL. Key entities and their relationships:

```
User (extends IdentityUser<int>)
├── Customer (1:1)
│   ├── Cart (1:1)
│   │   └── CartItem (1:N) → Dish
│   ├── Order (1:N)
│   │   ├── OrderItem (1:N) → Dish
│   │   ├── OrderDiscount (1:N)
│   │   └── Payment (1:1)
│   ├── LoyaltyAccount (1:N) → LoyaltyProgram
│   │   └── LoyaltyTransaction (1:N)
│   ├── DiscountCodeRedemption (1:N) → DiscountCode
│   ├── CustomerRating (1:N) → Restaurant
│   ├── CustomerFavouriteRestaurant (N:M)
│   └── CustomerHiddenRestaurant (N:M)
│
└── RestaurantOwner (1:1)
    └── Restaurant (1:N)
        ├── Dish (1:N)
        ├── Promotion (1:N)
        │   └── PromotionDish (N:M) → Dish
        ├── DiscountCode (1:N)
        ├── LoyaltyProgram (1:N)
        ├── Event (1:N)
        │   ├── EventMenuItem (1:N)
        │   └── EventBookingPolicy (1:1)
        ├── RestaurantTable (1:N)
        ├── RestaurantTransaction (1:N)
        ├── RestaurantRating (1:1, aggregate)
        ├── OrderRule (1:N)
        ├── OrderBlockedSlot (1:N)
        ├── Notification (1:N)
        └── ModerationAction (1:N)
```

### Key Enums

| Enum | Values |
|------|--------|
| `UserRole` | Administrator, Customer, RestaurantOwner |
| `UserStatus` | Active, Suspended, Banned |
| `OrderStatus` | Pending, Confirmed, Refused, Preparing, Ready, Delivering, Delivered, Cancelled |
| `OrderType` | Delivery, Pickup |
| `PaymentStatus` | Pending, Completed, Failed, Refunded |
| `PromotionType` | Percentage, FixedAmount, FreeItem |
| `DiscountType` | Percentage, FixedAmount |
| `TransactionType` | Commission, Withdrawal, Refund, Adjustment |

All enums are stored as strings in the database via EF Core `HasConversion`. Code references use `nameof()` for compile-time safety — never hardcoded strings.

---

## API Reference

All routes are defined in `DeliverTableSharedLibrary/Constants/ApiRoutes.cs`, the single source of truth used by both server `[Route]` attributes and client HTTP calls.

### Authentication

| Method | Route | Description |
|--------|-------|-------------|
| POST | `/api/v1/auth/login` | Login, returns JWT |
| POST | `/api/v1/auth/register` | Register a customer |
| POST | `/api/v1/auth/register/restaurant` | Register a restaurant owner |
| GET | `/api/v1/auth/me` | Get current user profile |
| PUT | `/api/v1/auth/profile` | Update profile |
| PUT | `/api/v1/auth/change-password` | Change password |

### Restaurants

| Method | Route | Description |
|--------|-------|-------------|
| GET | `/api/v1/restaurant` | List restaurants (paginated, filterable) |
| GET | `/api/v1/restaurant/{id}` | Restaurant detail |
| POST | `/api/v1/restaurant` | Create restaurant |
| PUT | `/api/v1/restaurant/{id}` | Update restaurant |
| GET | `/api/v1/restaurant/map` | Restaurants for map view |

### Dishes

| Method | Route | Description |
|--------|-------|-------------|
| GET | `/api/v1/dish` | List dishes for a restaurant |
| POST | `/api/v1/dish` | Create dish |
| PUT | `/api/v1/dish/{id}` | Update dish |
| DELETE | `/api/v1/dish/{id}` | Delete dish |

### Cart

| Method | Route | Description |
|--------|-------|-------------|
| GET | `/api/v1/cart` | Get current cart |
| POST | `/api/v1/cart/items` | Add item to cart |
| PUT | `/api/v1/cart/items/{id}` | Update cart item quantity |
| DELETE | `/api/v1/cart/items/{id}` | Remove cart item |

### Orders

| Method | Route | Description |
|--------|-------|-------------|
| POST | `/api/v1/order` | Place order |
| GET | `/api/v1/order` | Order history (paginated) |
| GET | `/api/v1/order/{id}` | Order detail |
| PUT | `/api/v1/order/{id}/status` | Update order status |
| PUT | `/api/v1/order/{id}/cancel` | Cancel order |

### Admin (14 sub-domains)

All admin routes are prefixed with `/api/v1/admin/` and require the `Administrator` role:

`users`, `restaurants`, `dishes`, `orders`, `promotions`, `discount-codes`, `loyalty`, `events`, `transactions`, `ratings`, `notifications`, `moderation`, `order-config`, `dashboard`

Each sub-domain supports standard CRUD operations plus domain-specific actions (e.g., suspend user, approve restaurant, refund order).

### Additional Endpoints

| Method | Route | Description |
|--------|-------|-------------|
| GET | `/api/v1/promotion` | Active promotions |
| POST | `/api/v1/discount-code/validate` | Validate a discount code |
| GET | `/api/v1/loyalty` | Loyalty account info |
| GET | `/api/v1/restaurant/{id}/account` | Restaurant earnings |
| GET | `/api/v1/health` | Health check |
| GET | `/images/{path}` | S3 image proxy |
| GET | `/documents/{path}` | S3 document proxy |

---

## Design System

### Brand Colors

| Token | Hex | Usage |
|-------|-----|-------|
| **Coral Fire** | `#E85D34` | Primary action color, buttons, links, focus rings |
| **Deep Charcoal** | `#1A1A2E` | Text, accent backgrounds, navigation |
| **Warm Cream** | `#FFF8F0` | Page backgrounds |
| **Amber Gold** | `#F2A541` | Secondary actions, warnings |
| **Garden Sage** | `#7BAE7A` | Success states, confirmations |
| **Soft Coral** | `#FF7F50` | Primary hover state |
| Error Red | `#C0392B` | Error states, destructive actions |

### Typography

| Role | Font | Weight |
|------|------|--------|
| Display headings | DM Serif Display | 400 |
| Body text | Plus Jakarta Sans | 300–800 |
| Monospace / labels | JetBrains Mono | 400 |

Font scale: `xs` (12px), `sm` (13px), `base` (15px), `lg` (17px), `h4` (18px), `h3` (24px), `h2` (36px), `h1` (48px)

### Spacing Scale

`xs` (0.25rem) → `sm` (0.5rem) → `md` (1rem) → `lg` (1.5rem) → `xl` (2rem) → `2xl` (2.5rem) → `3xl` (3rem)

### SCSS Architecture

```
Styles/
├── abstracts/          # Design tokens (_variables.scss) and reusable patterns (_mixins.scss)
├── base/               # CSS reset and base typography
├── components/         # Buttons, forms, alerts, admin UI components
├── layout/             # Navigation, sidebar, page container, modals
├── pages/              # Page-specific overrides
├── utilities/          # Blazor-specific CSS fixes
└── app.scss            # Main entry point (imports everything)
```

Each Blazor page and component also has a scoped `.razor.scss` file compiled to CSS isolation.

Key mixins: `focus-ring` (accessibility), `card` (card component), `heading-display` (serif headings), `label-mono` (monospace labels), `visually-hidden` (screen reader content).

---

## Testing

### Framework

- **NUnit 4** — test framework
- **NSubstitute** — mocking library (no concrete implementations in tests)
- **Coverlet** — code coverage collection
- **In-memory EF Core** — isolated database per test via `TestDatabase` fixture

### Running Tests

```bash
# Full test suite
make test

# Unit tests only
make test-unit

# Integration tests (ephemeral database)
make test-integration

# Coverage report (HTML output to ./reports/coverage/)
make coverage

# Specific test class
docker compose -f docker-dev.yaml exec backend \
  dotnet test /src/DeliverTableTests/DeliverTableTests.csproj \
  --no-build --filter "FullyQualifiedName~OrderServiceTests"
```

### Test Organization

```
DeliverTableTests/
├── Server/Unit/Controllers/    # Controller tests → mock service interfaces
├── Server/Unit/Services/       # Service tests → mock repository interfaces
├── Server/Unit/Mappers/        # Mapper tests (pure functions)
├── Server/Integration/         # EF Core tests with in-memory database
├── Client/Unit/Services/       # HTTP client tests with MockHttpMessageHandler
└── SharedLibrary/Unit/         # DTO validation, API route integrity
```

### Test Patterns

**Controller tests** mock the service interface and verify HTTP behavior:

```csharp
[TestFixture]
public class AuthControllerTests
{
    private IAuthService _authService;
    private AuthController _sut;

    [SetUp]
    public void SetUp()
    {
        _authService = Substitute.For<IAuthService>();
        _sut = new AuthController(_authService);
    }

    [Test]
    public async Task Login_WithSuccessResult_ReturnsOk()
    {
        _authService.LoginAsync(Arg.Any<LoginRequest>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<ConnectionResponse>.Success(response));

        var result = await _sut.Login(request, CancellationToken.None);

        Assert.That(result, Is.InstanceOf<OkObjectResult>());
    }
}
```

**Service tests** mock repository interfaces and verify business logic:

```csharp
[TestFixture]
public class OrderServiceTests
{
    private IOrderRepository _orderRepository;
    private OrderService _sut;

    [SetUp]
    public void SetUp()
    {
        _orderRepository = Substitute.For<IOrderRepository>();
        _sut = new OrderService(_orderRepository, /* other mocked deps */);
    }
}
```

**Client tests** use `MockHttpMessageHandler` to simulate API responses:

```csharp
var mockHandler = new MockHttpMessageHandler();
mockHandler.QueueJsonResponse(expectedDto);
var httpClient = new HttpClient(mockHandler);
```

### Test Factories

- `ServerEntityFactory` — creates domain entities with sensible defaults
- `ClientTestFactory` — generates valid JWTs, connection responses, error bodies
- `SharedLibraryDtoFactory` — creates DTOs for validation testing

### Known Failure

`AppEnvironmentTests.Load_AppliesDefaults_WhenOptionalVarsAreMissing` fails inside Docker because the Redis environment variable leaks from the container. This is expected and ignored in CI.

---

## CI/CD Pipeline

GitHub Actions runs a 3-tier pipeline on every pull request and push to `main`/`dev`:

```
Tier 1 — Fast Gates (parallel, no build)
├── Lint (code formatting check)
├── SAST (CodeQL static analysis)
├── Security Scan (NuGet vulnerability audit)
├── Compose Lint (Docker Compose validation)
└── License Check (dependency license compliance)

Tier 2 — Compilation Gate
└── Build (Release mode, warnings as errors)

Tier 3 — Expensive Verification (parallel, after build)
├── Test (unit + integration, coverage report, threshold enforcement)
└── Docker Build (production images + Trivy security scan)
```

Additional automation:
- **Dependabot** auto-updates NuGet packages, Docker base images, and GitHub Actions
- **Auto-merge** for passing Dependabot PRs

---

## Deployment

### Production Architecture

Production uses the same Docker Compose setup with hardened security:

- **Cloudflare Tunnel** — zero-trust ingress (no exposed ports)
- **Read-only filesystems** for all containers
- **Capability dropping** (CAP_DROP = ALL, selective CAP_ADD)
- **Log rotation** (json-file driver, 10MB max, 5 files)
- **Redis authentication** enabled
- **Isolated networks** with fixed subnets:
  - `dt-public-net` (172.30.0.0/24) — tunnel + proxy
  - `dt-frontend-net` (172.31.0.0/24) — proxy + frontend
  - `dt-backend-net` (172.32.0.0/24) — backend + database + Redis + Garage

### Deploy

```bash
# Set production environment variables in .env
# (especially CLOUDFLARE_TUNNEL_TOKEN and strong passwords)

# Start production stack
make prod

# Rebuild after code changes
make prod-rebuild
```

### Production Checklist

- [ ] Generate strong secrets: `openssl rand -base64 64` for JWT_KEY
- [ ] Generate unique passwords for PostgreSQL and Redis
- [ ] Generate S3 credentials: `openssl rand -hex 32` for secret key
- [ ] Set `ASPNETCORE_ENVIRONMENT=Production`
- [ ] Configure Cloudflare Tunnel to route to `http://172.30.0.10:80`
- [ ] Set `OPENAPI_ENABLE_DOCUMENTATION=false`
- [ ] Verify `COVERAGE_THRESHOLD` meets team standards

---

## Troubleshooting

### Containers Won't Start

**Symptom:** `make dev` fails or containers exit immediately.

```bash
# Check container status and logs
make dev-logs

# Rebuild images from scratch
docker compose -f docker-dev.yaml build --no-cache
make dev
```

### Database Connection Refused

**Symptom:** Backend logs show `Npgsql.NpgsqlException: Connection refused`

```bash
# Verify PostgreSQL is healthy
docker compose -f docker-dev.yaml ps database

# Check the connection string in .env matches docker network IPs
# Default: Host=192.168.60.20;Port=5432

# Reset database volume (destroys data)
make dev-down-volumes
make dev
make dev-migrate
```

### Pending Migrations

**Symptom:** `The model has changed since the database was last created`

```bash
make dev-migrate
```

### NuGet Restore Failures

**Symptom:** Build fails with `Unable to resolve package`

```bash
# Clear NuGet cache volume
docker volume rm delivertable_nuget-cache
make dev
```

### Port Conflicts

**Symptom:** `Bind for 0.0.0.0:8080 failed: port is already allocated`

Change the conflicting port in `.env`:

```bash
PROXY_PORT=8081    # Change from default 8080
DB_PORT=5433       # Change from default 5432
```

### Frontend Not Loading / Blank Page

```bash
# Check if the frontend container is running
docker compose -f docker-dev.yaml ps frontend

# Check Nginx proxy logs
docker compose -f docker-dev.yaml logs proxy
```

### Tests Failing

```bash
# Run build first to ensure compilation succeeds
docker compose -f docker-dev.yaml exec backend dotnet build /src/DeliverTable.sln

# Run tests with verbose output
docker compose -f docker-dev.yaml exec backend \
  dotnet test /src/DeliverTableTests/DeliverTableTests.csproj -v detailed
```

### Format Check Failures

```bash
# Auto-fix all formatting issues
make format-fix

# Then verify
make format-check
```

---

## Contributing

### Commit Convention

This project uses [Conventional Commits](https://www.conventionalcommits.org/) with Azure Boards references:

```
<type>(<scope>): <description>

PBI: AB#<id>
Task: AB#<id>
```

**Types:** `feat`, `fix`, `docs`, `style`, `refactor`, `perf`, `test`, `build`, `ci`, `chore`

**Scopes:** `client`, `server`, `shared`, `tests`, `api`, `auth`, `db`, `docker`

See [docs/commits-convention.md](docs/commits-convention.md) for detailed examples.

### Development Workflow

1. Create a feature branch from `main`
2. Follow TDD: write tests first, then implementation
3. Commit early and often (one logical unit per commit)
4. Run `make check` before pushing
5. Open a pull request — CI runs automatically

### Pre-commit Gate

Git hooks (installed via `make hooks-install`) enforce:
- **Pre-commit:** format check + build
- **Commit-msg:** conventional commit format validation

---

## License

This project is dual-licensed:

- **Open source**: [GNU Affero General Public License v3.0 (AGPL-3.0)](LICENSE)
- **Commercial**: Available for proprietary use

Copyright 2026 DeliverTable Team. See [LICENSING.md](LICENSING.md) for full details.

### Contributing

By contributing to DeliverTable, you agree to our [Contributor License Agreement](CLA.md) and the [Developer Certificate of Origin](DCO.md). All commits must include a `Signed-off-by` line (`git commit -s`).

## Commercial Licensing

If you want to use DeliverTable in proprietary software, deploy it as a SaaS without sharing source code, or embed it in a closed-source product, a commercial license is available. Please contact us for details.
