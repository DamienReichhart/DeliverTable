# DeliverTable — Project Structure

This document describes the solution layout and the role of each project and main folders.

---

## Solution Overview

| Project | Purpose |
|--------|---------|
| **DeliverTableServer** | ASP.NET Core Web API (backend). |
| **DeliverTableClient** | Blazor frontend (SPA). |
| **DeliverTableSharedLibrary** | Shared types, DTOs, and contracts used by server and client. |
| **DeliverTableTests** | Unit and integration tests. |

---

## DeliverTableServer

Backend API. Hosts controllers, configuration, and application services.

| Folder | Purpose |
|--------|---------|
| `Configuration/` | OpenAPI/Swagger setup, constants, and service registration extensions. |
| `Controllers/` | API controllers (e.g. `HealthController`). |
| `Data/` | Database connection and entities column rules (unique, size, ...) |
| `Infrastructure/` | Data access, external integrations, infrastructure concerns. |
| `Mappers/` | Object-to-object mapping (e.g. AutoMapper profiles). |
| `Middleware/` | Custom HTTP middleware. |
| `Models/` | Request/response and API-specific models. |
| `Repositories/` | Data access abstractions and implementations. |
| `Services/` | Application/business logic services. |
| `Validators/` | FluentValidation or other validation logic. |

**Key files:** `Program.cs` (host setup, CORS, OpenAPI), `appsettings.json` / `appsettings.Development.json`.

---

## DeliverTableClient

Blazor frontend. Calls the API and hosts UI components, pages, and styles.

| Folder | Purpose |
|--------|---------|
| `Components/` | Reusable Blazor components. |
| `Constants/` | Client-side constants. |
| `Extensions/` | DI and configuration extensions (e.g. API client registration). |
| `Hooks/` | Custom hooks or state helpers. |
| `Layout/` | Layout components (`MainLayout`, `NavMenu`). |
| `Pages/` | Routable pages (e.g. `Home`, `NotFound`). |
| `Services/` | API clients and options (`HealthApiClient`, `IApiClientOptions`). |
| `State/` | Client-side state management. |
| `Styles/` | SCSS (abstracts, base, components, layout, pages, utilities). |
| `Types/` | Client-specific TypeScript/C# types. |
| `Validators/` | Client-side validation. |
| `wwwroot/` | Static assets (HTML, CSS, icons, manifest, service worker). |

**Entry:** `Program.cs`, `App.razor`, `wwwroot/index.html`.

---

## DeliverTableSharedLibrary

Shared library referenced by server and client. No UI or hosting logic.

| Folder | Purpose |
|--------|---------|
| `Constants/` | Shared constants. |
| `Dtos/` | Data transfer objects (e.g. `HealthResponse`). |
| `Entities/` | Domain or persistence entities. |
| `Enums/` | Shared enumerations. |
| `Exceptions/` | Custom exception types. |
| `Interfaces/` | Contracts and abstractions. |

Use this project for types and contracts that must stay in sync between API and client.

---

## DeliverTableTests

Test project for the solution.

| Folder | Purpose |
|--------|---------|
| `Unit/` | Unit tests. |
| `Integration/` | Integration/API tests. |
| `Mocks/` | Test doubles and mock data. |
| `TestData/` | Fixtures and test data files. |

---

## Build and Run

- **Solution:** Open `DeliverTable.sln` and build/run from your IDE, or use `dotnet build` / `dotnet run` from the repo root or per-project directory.
- **Server:** Run `DeliverTableServer`; OpenAPI/Swagger is available in Development or when enabled via configuration.
- **Client:** Run `DeliverTableClient` (typically with the server for full-stack development).

---

*Last updated to reflect the current folder layout. Adjust this document when adding new projects or top-level folders.*
