# Conventional Commits Standard

**DeliverTable** · Commit message format for changelog generation, release notes, and collaboration across Blazor frontend, ASP.NET backend, and shared packages. Based on [Conventional Commits](https://www.conventionalcommits.org/).

---

## Table of contents

- [Quick reference](#quick-reference)
- [Format](#format)
- [Types](#types)
- [Scopes](#scopes)
- [Examples](#examples)
- [Rules of thumb](#rules-of-thumb)

---

## Quick reference

| Element | Syntax / convention |
|--------|----------------------|
| **Format** | `<type>(<scope>): <description>` |
| **Subject** | Imperative, no period, ~50 chars |
| **Body** | Optional; wrap at 72 characters |
| **Footer** | **Required**: `PBI: AB#n` and/or `Task: AB#n`; optional `BREAKING CHANGE:` |
| **Breaking** | `feat(scope)!:` or footer `BREAKING CHANGE:` |

---

## Format

```
<type>(<scope>): <description>

[optional body]

PBI: AB#<id>
Task: AB#<id>
[optional BREAKING CHANGE footer]
```

| Part | Meaning |
|------|--------|
| **type** | Kind of change (e.g. `feat`, `fix`, `docs`). |
| **scope** | Part of the solution affected (e.g. `client`, `server`, `shared`). |
| **description** | Short, imperative summary (e.g. "add order list" not "added order list"). |
| **body** | Optional; detailed explanation; wrap at 72 characters. |
| **footer** | **Required** Azure Boards references (`PBI: AB#n`, `Task: AB#n`); optional `BREAKING CHANGE:`. |

### Azure Boards traceability

Every commit **must** include at least one Azure Boards reference in its footer to maintain full traceability between code changes and work items.

| Token | Format | When to use |
|-------|--------|-------------|
| `PBI` | `PBI: AB#<id>` | Reference the parent Product Backlog Item. |
| `Task` | `Task: AB#<id>` | Reference the specific Task under the PBI. |

- Include **both** `PBI` and `Task` when the commit maps to a specific task.
- Include only `PBI` when the commit is not tied to a particular task (e.g. documentation, chores).
- Multiple references are allowed (one per line).

---

## Types

| Type | Use when |
|------|----------|
| `feat` | New feature or user-facing capability. |
| `fix` | Bug fix. |
| `docs` | Documentation only (README, API docs, this file). |
| `style` | Formatting, whitespace, missing semicolons; no code logic change. |
| `refactor` | Code change that neither fixes a bug nor adds a feature. |
| `perf` | Performance improvement. |
| `test` | Adding or updating tests. |
| `build` | Build, CI, or tooling (e.g. MSBuild, Docker, GitHub Actions). |
| `ci` | CI configuration only (e.g. workflows, scripts). |
| `chore` | Other changes (deps, config, chores). |

---

## Scopes

Scopes map to the projects in the solution (see project structure documentation):

| Scope | Project | Description |
|-------|---------|-------------|
| `client` | DeliverTableClient | Blazor UI, pages, components, layout, styles, client-side services and configuration. |
| `server` | DeliverTableServer | ASP.NET Core API, controllers, services, configuration, middleware, infrastructure. |
| `shared` | DeliverTableSharedLibrary | Shared DTOs, constants, entities, enums, and contracts used by client and server. |
| `tests` | DeliverTableTests | Unit and integration tests, mocks, test data. |
| `api` | — | API surface (routes, contracts, versioning) when spanning client and server. |
| `auth` | — | Authentication and authorization. |
| `db` | — | Migrations, schema, seed data. |
| `docker` | — | Dockerfiles and container orchestration. |

---

## Examples

### Features

```text
feat(client): add order list with pagination

PBI: AB#5520
Task: AB#5521
```

```text
feat(server): add GET /api/orders with filtering

PBI: AB#5520
Task: AB#5523
```

### Bug fixes

```text
fix(client): correct date format in order table

PBI: AB#5530
Task: AB#5532
```

### Documentation

```text
docs: add the data dictionary documentation

PBI: AB#5512
Task: AB#5514
```

### Refactor and style

```text
refactor(server): extract order mapping to dedicated service

PBI: AB#5540
Task: AB#5542
```

### Build and CI

```text
build: bump Microsoft.AspNetCore.OpenApi to 9.0.1

PBI: AB#5550
```

### Tests

```text
test(server): add OrderService unit tests

PBI: AB#5520
Task: AB#5525
```

### Breaking changes (footer)

```text
feat(api)!: require API key header for all endpoints

BREAKING CHANGE: Clients must send X-Api-Key. Remove in v2.

PBI: AB#5560
Task: AB#5561
```

Use `!` after the type/scope or a `BREAKING CHANGE:` footer to signal breaking changes.
Azure Boards references (`PBI`, `Task`) must appear **after** any `BREAKING CHANGE:` line.

---

## Rules of thumb

1. **One logical change per commit** — easier to review and revert.
2. **Imperative mood** — "add feature" not "added feature".
3. **No period** at the end of the subject line.
4. **Always reference Azure Boards work items** — every commit must include `PBI: AB#<id>` and, when applicable, `Task: AB#<id>` in the footer.
5. **Scope** must match the part of the solution you changed (`client`, `server`, `shared`, `tests`, `api`, `auth`, `db`, `docker`).
