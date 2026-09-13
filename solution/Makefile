.DEFAULT_GOAL := help
SHELL := /bin/bash

COMPOSE := docker compose
DOTNET_SDK := mcr.microsoft.com/dotnet/sdk:10.0
NODE := node:24-alpine
DOTNET_RUN := docker run --rm -v $(CURDIR)/backend:/src -w /src -v fundo-solution-nuget:/root/.nuget/packages
TESTCONTAINERS := -e TESTCONTAINERS_RYUK_DISABLED=true -e TESTCONTAINERS_HOST_OVERRIDE=host.docker.internal -v /var/run/docker.sock:/var/run/docker.sock

.PHONY: help up down logs ps reset test test-backend test-frontend test-external lint check

help: ## List targets
	@grep -E '^[a-zA-Z_-]+:.*## ' $(MAKEFILE_LIST) | awk 'BEGIN {FS = ":.*## "}; {printf "  \033[36m%-15s\033[0m %s\n", $$1, $$2}'

.env:
	cp .env.example .env

up: .env ## Build and start the whole stack (migrations run automatically)
	$(COMPOSE) up --build -d --wait
	@echo "Frontend:         http://localhost:5330"
	@echo "External service: http://localhost:5230/api/v1/customers"

down: ## Stop the stack
	$(COMPOSE) down

logs: ## Follow logs of every service
	$(COMPOSE) logs -f

ps: ## Show service status
	$(COMPOSE) ps

reset: ## Stop the stack and delete the database volume
	$(COMPOSE) down -v

test: test-backend test-frontend test-external ## Run every test suite (needs only Docker)

test-backend: ## Backend unit + integration tests (integration uses Testcontainers)
	$(DOTNET_RUN) $(TESTCONTAINERS) $(DOTNET_SDK) bash -c 'set -e; \
		for p in unit/Fundo.Loans.Domain.UnitTests unit/Fundo.Loans.Application.UnitTests integration/Fundo.Loans.IntegrationTests; do \
			n=$$(basename $$p); dotnet build tests/$$p/$$n.csproj -v q -nologo && ./tests/$$p/bin/Debug/net10.0/$$n; \
		done'

test-frontend: ## Frontend unit tests
	docker run --rm -v $(CURDIR)/frontend:/app -w /app $(NODE) sh -c "npm ci --no-audit --no-fund && npm test"

test-external: ## External service tests
	docker run --rm -v $(CURDIR)/external-service:/app -w /app $(NODE) sh -c "npm ci --no-audit --no-fund && npm test"

lint: ## Backend build with warnings as errors + frontend eslint and strict type check
	$(DOTNET_RUN) $(DOTNET_SDK) dotnet build Fundo.Loans.slnx -v q -nologo
	docker run --rm -v $(CURDIR)/frontend:/app -w /app $(NODE) sh -c "npm ci --no-audit --no-fund && npm run lint && npm run typecheck"

check: lint test ## Everything that must pass before submitting
