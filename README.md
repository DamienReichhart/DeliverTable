# DeliverTable

## Configuration

### Server (DeliverTableServer)

The server uses a `.env` file for local and environment-specific settings. Environment variables override `appsettings.json`.

- **Setup:** Copy `DeliverTableServer/.env.example` to `DeliverTableServer/.env` and set values. Do not commit `.env`.
- **Variables:** See `.env.example` for all supported keys.

### Client (DeliverTableClient)

The client uses a centralized config file loaded at startup. No secrets—the file is served to the browser.

- **Files:** `wwwroot/appconfig.json` (default) and `wwwroot/appconfig.Development.json` (used when running in Development). In Development, the client loads `appconfig.Development.json` if present, otherwise falls back to `appconfig.json`.
- **Schema:** `api.baseUrl` (API base URL; empty = same origin), `environment` (e.g. `"Development"`, `"Production"`). For local dev with the server on another port, set `api.baseUrl` in `appconfig.Development.json` (e.g. `http://localhost:5268`).
- **Usage:** Inject `IAppConfiguration` where needed; API base URL is used automatically by registered API clients.

## Contributing

Commit messages follow [Conventional Commits](https://www.conventionalcommits.org/). See [docs/conventional-commits.md](docs/conventional-commits.md) for types, scopes (frontend, backend, common, api, etc.), and examples.