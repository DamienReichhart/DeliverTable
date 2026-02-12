# Conventional Commits Standard

This document defines how to write commit messages in the DeliverTable project using [Conventional Commits](https://www.conventionalcommits.org/). Consistent commit messages improve changelog generation, release notes, and collaboration across the Blazor frontend, ASP.NET backend, and shared packages.

---

## Format

```
<type>(<scope>): <description>

[optional body]

[optional footer(s)]
```

- **type**: What kind of change (e.g. `feat`, `fix`, `docs`).
- **scope**: Which part of the solution is affected (e.g. `frontend`, `backend`, `common`).
- **description**: Short, imperative summary (e.g. "add order list" not "added order list").
- **body** (optional): Detailed explanation; wrap at 72 characters.
- **footer** (optional): References (e.g. `Refs #123`) and/or breaking changes.

---

## Types

| Type       | Use when |
|-----------|----------|
| `feat`    | New feature or user-facing capability. |
| `fix`     | Bug fix. |
| `docs`    | Documentation only (README, API docs, this file). |
| `style`   | Formatting, whitespace, missing semicolons; no code logic change. |
| `refactor`| Code change that neither fixes a bug nor adds a feature. |
| `perf`    | Performance improvement. |
| `test`    | Adding or updating tests. |
| `build`   | Build, CI, or tooling (e.g. MSBuild, Docker, GitHub Actions). |
| `ci`      | CI configuration only (e.g. workflows, scripts). |
| `chore`   | Other changes (deps, config, chores). |

---

## Scopes

Scopes map to the main areas of the solution:

| Scope        | Description |
|-------------|-------------|
| `frontend`  | Blazor UI, pages, components, client-side logic. |
| `backend`   | ASP.NET Core API, controllers, services, persistence. |
| `common`    | Shared library used by frontend and/or backend (DTOs, contracts, utilities). |
| `api`       | API surface (routes, contracts, versioning) when spanning frontend/backend. |
| `auth`      | Authentication and authorization. |
| `db`        | Migrations, schema, seed data. |
| `docker`    | Dockerfiles and container orchestration. |
| `*`         | When using  multiples scopes (eg: feat(frontend, backend)) |

---

## Examples

### Features

```text
feat(frontend): add order list with pagination
feat(backend): add GET /api/orders with filtering
feat(common): add OrderDto and validation helpers
feat(auth): implement JWT refresh in Blazor client
```

### Bug fixes

```text
fix(frontend): correct date format in order table
fix(backend): handle null customer in order service
fix(common): fix timezone in DateOnly serialization
```

### Documentation

```text
docs: add conventional commits standard
docs(api): document orders endpoint
docs(backend): add service layer overview
```

### Refactor and style

```text
refactor(backend): extract order mapping to dedicated service
style(frontend): apply editorconfig to Blazor components
```

### Build and CI

```text
build: bump Microsoft.AspNetCore.OpenApi to 9.0.1
ci: add workflow for backend unit tests
chore(docker): update base image for API
```

### Tests

```text
test(backend): add OrderService unit tests
test(common): add validation tests for OrderDto
```

### Breaking changes (footer)

```text
feat(api)!: require API key header for all endpoints

BREAKING CHANGE: Clients must send X-Api-Key. Remove in v2.
```

Use `!` after the type/scope or a `BREAKING CHANGE:` footer to signal breaking changes.

---

## Rules of thumb

1. **One logical change per commit** — easier to review and revert.
2. **Imperative mood** — "add feature" not "added feature".
3. **No period** at the end of the subject line.
4. **Reference issues** in body or footer: `Refs #42`, `Closes #42`.
5. **Scope** need to match the part of the solution you changed (frontend, backend, common, api, auth, db, docker).