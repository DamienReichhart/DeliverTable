# ═══════════════════════════════════════════════════════════════════
#  DeliverTable – Makefile
# ═══════════════════════════════════════════════════════════════════

COMPOSE_DEV   = docker compose -f docker-dev.yaml
COMPOSE_PROD  = docker compose -f docker-prod.yaml
COMPOSE_UTILS = docker compose -f docker-utils.yaml

.DEFAULT_GOAL := help

# ── Development Environment ──────────────────────────────────────

.PHONY: dev
dev: ## Start the full development stack (proxy + frontend + backend + database)
	$(COMPOSE_DEV) up --build

.PHONY: dev-detach
dev-detach: ## Start the development stack in the background
	$(COMPOSE_DEV) up --build -d

.PHONY: dev-down
dev-down: ## Stop and remove the development stack
	$(COMPOSE_DEV) down

.PHONY: dev-down-volumes
dev-down-volumes: ## Stop the development stack and remove volumes (database data, nuget cache)
	$(COMPOSE_DEV) down -v

.PHONY: dev-logs
dev-logs: ## Tail logs from all development services
	$(COMPOSE_DEV) logs -f

.PHONY: dev-ps
dev-ps: ## Show running development containers
	$(COMPOSE_DEV) ps

.PHONY: dev-restart
dev-restart: ## Restart all development services
	$(COMPOSE_DEV) restart

.PHONY: dev-rebuild
dev-rebuild: ## Rebuild images and restart the development stack
	$(COMPOSE_DEV) up --build --force-recreate

# ── Production Environment ───────────────────────────────────────

.PHONY: prod
prod: ## Start the production stack in the background (tunnel + proxy + frontend + backend + database)
	$(COMPOSE_PROD) up --build -d

.PHONY: prod-down
prod-down: ## Stop and remove the production stack
	$(COMPOSE_PROD) down

.PHONY: prod-down-volumes
prod-down-volumes: ## Stop the production stack and remove volumes (database data)
	$(COMPOSE_PROD) down -v

.PHONY: prod-logs
prod-logs: ## Tail logs from all production services
	$(COMPOSE_PROD) logs -f

.PHONY: prod-ps
prod-ps: ## Show running production containers
	$(COMPOSE_PROD) ps

.PHONY: prod-restart
prod-restart: ## Restart all production services
	$(COMPOSE_PROD) restart

.PHONY: prod-rebuild
prod-rebuild: ## Rebuild images and restart the production stack
	$(COMPOSE_PROD) up --build --force-recreate -d

.PHONY: prod-build
prod-build: ## Build production images without starting containers
	$(COMPOSE_PROD) build

# ── Testing ──────────────────────────────────────────────────────

.PHONY: test
test: ## Run the full test suite
	$(COMPOSE_UTILS) run --rm test

.PHONY: test-unit
test-unit: ## Run unit tests only
	$(COMPOSE_UTILS) run --rm test-unit

.PHONY: test-integration
test-integration: ## Run integration tests (spins up an ephemeral database)
	$(COMPOSE_UTILS) run --rm test-integration

# ── Code Coverage ────────────────────────────────────────────────

.PHONY: coverage
coverage: ## Run tests with coverage and generate HTML report (reports/coverage/)
	$(COMPOSE_UTILS) run --rm coverage

# ── Code Quality ─────────────────────────────────────────────────

.PHONY: format-check
format-check: ## Validate code formatting (fails if unformatted)
	$(COMPOSE_UTILS) run --rm format-check

.PHONY: format-fix
format-fix: ## Auto-fix code formatting
	$(COMPOSE_UTILS) run --rm format-fix

# ── Security & Dependencies ─────────────────────────────────────

.PHONY: security-audit
security-audit: ## Scan for vulnerable and deprecated NuGet packages
	$(COMPOSE_UTILS) run --rm security-audit

.PHONY: outdated
outdated: ## List outdated NuGet packages
	$(COMPOSE_UTILS) run --rm outdated

# ── Build Verification ───────────────────────────────────────────

.PHONY: build-release
build-release: ## Verify clean Release build (warnings as errors)
	$(COMPOSE_UTILS) run --rm build-release

# ── Housekeeping ─────────────────────────────────────────────────

.PHONY: clean
clean: ## Remove bin/, obj/, and reports/ directories
	$(COMPOSE_UTILS) run --rm clean

.PHONY: utils-build
utils-build: ## Pre-build the utils Docker image
	$(COMPOSE_UTILS) build

.PHONY: utils-down
utils-down: ## Stop and remove any lingering utils containers
	$(COMPOSE_UTILS) down

# ── Git Hooks ────────────────────────────────────────────────────

HOOKS_DIR = scripts/hooks

.PHONY: hooks-install
hooks-install: ## Install Git hooks (pre-commit + commit-msg)
	@git config core.hooksPath $(HOOKS_DIR)
	@echo 'Git hooks installed (core.hooksPath → $(HOOKS_DIR))'

.PHONY: hooks-uninstall
hooks-uninstall: ## Remove Git hooks
	@git config --unset core.hooksPath || true
	@echo 'Git hooks removed'

# ── Composite Targets ────────────────────────────────────────────

.PHONY: ci
ci: format-check build-release test security-audit coverage ## Run full CI quality gate (format → build → test → security → coverage)

.PHONY: check
check: format-check build-release test ## Quick pre-commit check (format → build → test)

# ── Help ─────────────────────────────────────────────────────────

.PHONY: help
help: ## Show this help
	@echo ''
	@echo '  DeliverTable – available commands'
	@echo ''
	@grep -E '^[a-zA-Z_-]+:.*?## .*$$' $(MAKEFILE_LIST) | \
		awk 'BEGIN {FS = ":.*?## "}; {printf "  \033[36m%-20s\033[0m %s\n", $$1, $$2}'
	@echo ''
