# DeliverTable

## Configuration

### Environment Variables

All environment variables live in a single `.env` file at the repository root (infrastructure, server, and tooling).

- **Setup:** Copy `.env.example` to `.env` and adjust values. Do not commit `.env`.
- **Variables:** See `.env.example` for all supported keys and documentation.

### Client (DeliverTableClient)

The client uses a centralized config file loaded at startup. No secrets—the file is served to the browser.

- **Files:** `wwwroot/appconfig.json` (default) and `wwwroot/appconfig.Development.json` (used when running in Development). In Development, the client loads `appconfig.Development.json` if present, otherwise falls back to `appconfig.json`.
- **Schema:** `api.baseUrl` (API base URL; empty = same origin), `environment` (e.g. `"Development"`, `"Production"`). For local dev with the server on another port, set `api.baseUrl` in `appconfig.Development.json` (e.g. `http://localhost:5268`).
- **Usage:** Inject `IAppConfiguration` where needed; API base URL is used automatically by registered API clients.

## Getting started

Prerequisites: **Make** and **Docker** (with Compose plugin).

```bash
# Install Git hooks (pre-commit + commit-msg validation)
make hooks-install

# Start the full development stack
make dev
```

Run `make help` for all available commands.

## Contributing

Commit messages follow [Conventional Commits](https://www.conventionalcommits.org/). See [docs/commits-convention.md](docs/commits-convention.md) for types, scopes (client, server, shared, tests, api), and examples.
